using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Plugin_Sisense.Configuration
{
    public static class Swagger
    {
        public static void ConfigureSwaggerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
            {
                var env = new EnvironmentConfig();

                options.SchemaFilter<AutoRestSchemaFilter>();

                options.OperationFilter<OperationNamingFilter>();
                options.OperationFilter<VersioningFilter>();
                options.OperationFilter<RequiredBodyFilter>();

                options.MapType<JObject>(() => new Schema()
                {
                    Type = "object",
                    AdditionalProperties = new Schema()
                    {
                    }
                });

                options.CustomSchemaIds(type =>
                {
                    if (type.IsGenericType)
                    {
                        var wrapperName = type.Name.Split("`").First();
                        var parameterNames = type.GetGenericArguments().Select(x => x.Name).ToList();
                        return $"{wrapperName}Of{string.Join("And", parameterNames)}";
                    }

                    // TODO: Uncomment when ready to support polymorphism and integrate with dataflow-contracts
                    // options.SchemaFilter<PolymorphismSchemaFilter>();
//                    var resourceName = type
//                        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
//                        .Where(fi => fi.Name == "ResourceName")
//                        .Select(fi => (string) fi.GetRawConstantValue())
//                        .FirstOrDefault();
//                    if (resourceName != null)
//                    {
//                        return resourceName;
//                    }

                    return Regex.Replace(type.Name, "Model$", "");
                });

                options.AddSecurityDefinition("api", new ApiKeyScheme()
                {
                    In = "header",
                    Name = "Authorization"
                });

                options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>()
                {
                    {"api", new string[0]}
                });

                // resolve the IApiVersionDescriptionProvider service
                // note: that we have to build a temporary service provider here because one has not been created yet
                var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                // add a swagger document for each discovered API version
                // note: you might choose to skip or document deprecated API versions differently
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description, env));
                }


                /* Disabled because it breaks dotnet watch */
//                var xmlCommentsBasePath = ApplicationEnvironment.ApplicationBasePath;
//                var xmlCommentsFilePath = typeof( Startup ).GetTypeInfo().Assembly.GetName().Name + ".xml";
//                options.IncludeXmlComments(Path.Combine( xmlCommentsBasePath, xmlCommentsFilePath));
            });
        }

        public static void ConfigureSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((document, request) =>
                {
                    var paths = document.Paths.ToDictionary(item => item.Key.ToLowerInvariant(), item => item.Value);
                    document.Paths.Clear();
                    foreach (var pathItem in paths) document.Paths.Add(pathItem.Key, pathItem.Value);

                    // Add query parameters to all GetAll paths:
                    foreach (var getAllPath in paths.Select(x => x.Value.Get)
                        .Where(x => x?.OperationId.Contains("GetAll") ?? false))
                    {
                        var modelName = getAllPath.Responses["200"].Schema.Items.Ref.Replace("#/definitions/", "");
                        var modelSchema = document.Definitions[modelName];
                        getAllPath.Parameters = modelSchema.Properties
                            .Where(x => x.Value.Type == "string" && x.Value.Format == null)
                            .Select(kv => new NonBodyParameter()
                            {
                                In = "query",
                                Name = kv.Key,
                                Type = "string",
                                Required = false,
                            })
                            .ToList<IParameter>();
                    }

                    foreach (var documentPath in document.Paths)
                    {
                        var schema = documentPath.Value?.Get?.Responses?.Values?.Select(x => x.Schema)
                            .First(x => x != null);

                        if (schema == null)
                        {
                            continue;
                        }

                        if (schema.Type == "array")
                        {
                            schema = new Schema()
                            {
                                Ref = schema.Items.Ref
                            };
                        }

                        var emptyResponses = new[] {documentPath.Value.Post, documentPath.Value.Put}
                            .Where(op => op != null)
                            .Where(op => op.Responses != null)
                            .SelectMany(op => op.Responses)
                            .Where(x => x.Key.StartsWith("2") || x.Key == "409")
                            .Select(x => x.Value)
                            .Where(x => x != null && x.Schema == null);

                        foreach (var emptyResponse in emptyResponses)
                        {
                            emptyResponse.Schema = schema;
                        }
                    }
                });

                options.PreSerializeFilters.Add((document, request) => { document.Host = request.Host.Value; });
            });
            app.UseSwaggerUI(options =>
            {
                options.DisplayOperationId();
                var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

                // build a swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
            });
        }

        private static Info CreateInfoForApiVersion(ApiVersionDescription description, EnvironmentConfig env)
        {
            var info = new Info
            {
                Title = $"Plugin Sisense {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = $"API surface for Sisense Plugin. Application version is {env.Version}.",
                Contact = new Contact {Name = "Naveego", Email = "support@naveego.com"},
            };

            if (description.IsDeprecated) info.Description += " This API version has been deprecated.";

            return info;
        }


        private class VersioningFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                if (operation.Parameters == null) return;

                foreach (var parameter in operation.Parameters.OfType<NonBodyParameter>())
                {
                    var description = context.ApiDescription
                        .ParameterDescriptions
                        .First(p => p.Name == parameter.Name);
                    var routeInfo = description.RouteInfo;

                    if (parameter.Description == null) parameter.Description = description.ModelMetadata?.Description;

                    if (routeInfo == null) continue;

                    if (parameter.Default == null) parameter.Default = routeInfo.DefaultValue;

                    parameter.Required |= !routeInfo.IsOptional;
                }
            }
        }

        private class OperationNamingFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                var operationAttribute = context.ControllerActionDescriptor
                    .GetControllerAndActionAttributes(true)
                    .OfType<SwaggerOperationAttribute>()
                    .FirstOrDefault();

                var actionName = context.ControllerActionDescriptor.ActionName.Replace("Async", "");
                var controllerName = context.ControllerActionDescriptor.ControllerName;
                var resourceName = actionName == "GetAll"
                    ? controllerName
                    : controllerName.Singularize();

                if (operationAttribute != null)
                {
                    operation.OperationId = operationAttribute.OperationId.Replace("{resource}", resourceName);
                }
                else
                {
                    var httpMethod = context.ApiDescription.HttpMethod;
                    var method =
                        httpMethod == "POST" ? "Create"
                        : httpMethod == "PUT" ? "Update"
                        : httpMethod == "DELETE" ? "Delete"
                        : "Get";


                    operation.OperationId = $"{actionName}{resourceName}";
                }

                operation.Summary = operation.Summary ?? (operation.OperationId.Humanize() + ".");
            }
        }

        private class RequiredBodyFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                foreach (var bodyParam in operation.Parameters.Where(p => p.In == "body")) bodyParam.Required = true;
            }
        }


        [UsedImplicitly]
        private class AutoRestSchemaFilter : ISchemaFilter
        {
            public void Apply(Schema schema, SchemaFilterContext context)
            {
                var typeInfo = context.SystemType;

                if (typeInfo.IsEnum)
                    schema.Extensions.Add(
                        "x-ms-enum",
                        new {name = typeInfo.Name, modelAsString = true}
                    );

                if (typeInfo == typeof(JObject))
                {
                    schema.AdditionalProperties = new Schema()
                    {
                        Type = "object"
                    };
                }
            }
        }


//        public class PolymorphismSchemaFilter : ISchemaFilter
//        {
//            private class DiscriminatorData
//            {
//                public Type Base { get; set; }
//                public string Name { get; set; }
//                public string Discriminator { get; set; }
//            }
//
//            private static Dictionary<Type, DiscriminatorData> derivedToBaseTypes;
//            private static Dictionary<Type, List<Type>> baseTypesToDerivedTypes;
//
//            static PolymorphismSchemaFilter()
//            {
//                var baseTypes = Assembly.GetExecutingAssembly().DefinedTypes
//                    .Where(x => x.GetCustomAttribute<JsonConverterAttribute>(false) != null)
//                    .ToList();
//                var derivedTypes = baseTypes.SelectMany(bt =>
//                    bt.GetCustomAttributes<JsonInheritanceAttribute>(false)
//                        .Select(kt => new KeyValuePair<Type, DiscriminatorData>(
//                            kt.Type,
//                            new DiscriminatorData()
//                            {
//                                Base = bt,
//                                Name = kt.Key,
//                                Discriminator = bt.GetCustomAttribute<JsonConverterAttribute>(false).ConverterParameters
//                                    .OfType<string>().First()
//                            })
//                        )).ToList();
//
//
//                derivedToBaseTypes = new Dictionary<Type, DiscriminatorData>(derivedTypes);
//                baseTypesToDerivedTypes = derivedToBaseTypes.GroupBy(x => x.Value.Base, x => x.Key)
//                    .ToDictionary(x => x.Key, x => x.ToList());
//            }
//
//            public void Apply(Schema model, SchemaFilterContext context)
//            {
//                if (baseTypesToDerivedTypes.TryGetValue(context.SystemType, out var derivedTypes))
//                {
//                    var discriminator = context.SystemType.GetCustomAttribute<JsonConverterAttribute>()
//                        .ConverterParameters.OfType<string>().First();
//                    if (discriminator != null)
//                    {
//                        model.Discriminator = discriminator;
//                        model.Required = model.Required ?? new List<string>();
//                        if (!model.Required.Contains(discriminator))
//                        {
//                            model.Required.Add(discriminator);
//                        }
//
//                        if (!model.Properties.ContainsKey(discriminator))
//                        {
//                            model.Properties.Add(discriminator, new Schema {Type = "string"});
//                        }
//                    }
//
//                    //Register derived classes
//                    foreach (var item in derivedTypes)
//                    {
//                        var derivedSchema = context.SchemaRegistry.GetOrRegister(item);
//
//                        var baseSchema = model;
//                        var baseSchemaRef = new Schema() {Ref = baseSchema.Ref};
//
//                        var clonedDerivedSchema = new Schema
//                        {
//                            Properties = derivedSchema.Properties.Where(p => baseSchema.Properties?.ContainsKey(p.Key) != true).ToDictionary(x => x.Key, x => x.Value),
//                            Type = derivedSchema.Type,
//                            Required = derivedSchema.Required,
//                            Title = derivedSchema.Title,
//
//                        };
//
//                        model.AllOf = new List<Schema> {baseSchemaRef, clonedDerivedSchema};
//
//                        //Reset properties for they are included in allOf, should be null but code does not handle it
//                        model.Properties = new Dictionary<string, Schema>();
//                    }
//                }
//            }
//        }
    }
}
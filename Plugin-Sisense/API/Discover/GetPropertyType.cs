using Pub;

namespace Plugin_Sisense.API.Discover
{
    public static partial class Discover
    {
        /// <summary>
        /// Gets the Naveego type from the provided Sisense information
        /// </summary>
        /// <param name="type"></param>
        /// <returns>The property type</returns>
        public static PropertyType GetPropertyType(string type)
        {
            switch (type)
            {
                case "boolean":
                    return PropertyType.Bool;
                case "double":
                    return PropertyType.Float;
                case "number":
                case "integer":
                    return PropertyType.Integer;
                case "jsonarray":
                case "jsonobject":
                    return PropertyType.Json;
                case "date":
                case "datetime":
                    return PropertyType.Datetime;
                case "time":
                    return PropertyType.Text;
                case "float":
                    return PropertyType.Float;
                case "decimal": 
                case "numeric":
                    return PropertyType.Decimal;
                default:
                    return PropertyType.String;
            }
        }
    }
}
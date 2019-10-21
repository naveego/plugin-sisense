namespace Plugin_Sisense.DataContracts
{
    public class Module
    {
        public string api_name    { get; set; }
        public string module_name { get; set; }
        public string generated_type { get; set; }
    }
    
    public class ModuleResponse
    {
        public Module[] modules { get; set; }
    }
}
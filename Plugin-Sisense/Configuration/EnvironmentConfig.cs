using System.Reflection;

namespace Plugin_Sisense.Configuration
{
    public class EnvironmentConfig
    {
        private string _version;
        public string Version
        {
            get
            {
                if (_version == null)
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    _version = $"{version.Major}.{version.Minor}.{version.Build}";
                }

                return _version;

            }
        }
    }
}
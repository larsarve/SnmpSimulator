using System.Text.Json.Serialization;

namespace SnmpSimulator
{
    public class AppConfig
    {
        public ServerConfig Server { get; set; } = new();
        public PathsConfig Paths { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();

        public class ServerConfig
        {
            public string IpAddress { get; set; } = "0.0.0.0";
            public int Port { get; set; } = 16162;
            public string Community { get; set; } = "public";
        }

        public class PathsConfig
        {
            public string DevicesDirectory { get; set; } = "devices";
            public string CustomDirectory { get; set; } = "custom";
        }

        public class LoggingConfig
        {
            public string LogLevel { get; set; } = "Information";
            public bool LogToConsole { get; set; } = true;
            public bool LogToFile { get; set; } = false;
            public string LogFilePath { get; set; } = "logs/snmpsim.log";
        }

        public static AppConfig Load()
        {
            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "appconfig.json");
            if (File.Exists(configPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(jsonContent);
                    if (config != null)
                    {
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading configuration: {ex.Message}");
                }
            }
            return new AppConfig();
        }
    }
} 
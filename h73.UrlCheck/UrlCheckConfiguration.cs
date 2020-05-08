using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace h73.UrlCheck
{
    public class UrlCheckConfiguration
    {
        private const string ConfigFile = "config.json";

        public int CheckInterval { get; set; }
        public int Retries { get; set; }
        public int RetryInterval { get; set; }
        public int FailedPause { get; set; }
        public string ReportEmail { get; set; }
        public string FromEmail { get; set; }
        public SmtpConfig SmtpConfig { get; set; }
        public List<string> Urls { get; set; }

        public static UrlCheckConfiguration GetConfiguration()
        {
            if (!File.Exists(ConfigFile)) File.Create(ConfigFile);
            return JsonConvert.DeserializeObject<UrlCheckConfiguration>(File.ReadAllText(ConfigFile));
        }

        public void Save()
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }

    public class SmtpConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
    }
}
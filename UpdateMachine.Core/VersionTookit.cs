using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;

namespace UpdateMachine.Core
{
    public class VersionTookit
    {
        public static FileVersions GetVersionFile(string root)
        {
            try
            {
                string value;
                using (WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 })
                    value = webClient.DownloadString(Path.Combine(root, ".updatemachine", "versions").Replace('\\', '/'));
                return JsonConvert.DeserializeObject<FileVersions>(value);
            }
            catch { }
            return new FileVersions() { versions = new VersionItem[0] };
        }

        public class FileVersions
        {
            public VersionItem[] versions { get; set; }
        }

        public class VersionItem
        {
            public string version { get; set; }
            public string log { get; set; }
        }
    }
}

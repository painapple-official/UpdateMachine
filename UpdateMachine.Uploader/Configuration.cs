using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UpdateMachine.Core;

namespace UpdateMachine.Uploader
{
    public class Configuration
    {
        public static FileConfiguration Load(string root)
        {
            try
            {
                string path = Path.Combine(root, ".updatemachine.workspace", "config.json");
                if (File.Exists(path))
                {
                    string cfg;
                    using (StreamReader s = new StreamReader(new FileStream(path, FileMode.Open)))
                    {
                        cfg = s.ReadToEnd();
                    }
                    return JsonConvert.DeserializeObject<FileConfiguration>(cfg);
                }
            }
            catch { }
            return new FileConfiguration();
        }

        public static void Save(string root, FileConfiguration configuration)
        {
            try
            {
                string path = Path.Combine(root, ".updatemachine.workspace");
                Directory.CreateDirectory(path);
                path = Path.Combine(path, "config.json");
                using (StreamWriter s = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.ReadWrite)))
                    s.Write(JsonConvert.SerializeObject(configuration));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
    public class FileConfiguration
    {
        public string accessKeyId { get; set; } = "";
        public string accessKeySecret { get; set; } = "";
        public string endpoint { get; set; } = "";
        public string bucketName { get; set; } = "";
        public string path { get; set; } = "";
        public string publicUrl { get; set; } = "";
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;

namespace UpdateMachine.Core
{

    public sealed class Updater
    {
        private readonly string url;

        public delegate void OnStatusChangedHandler(UpdaterStatus status);

        public event OnStatusChangedHandler OnStatusChanged;

        public delegate void OnProgressChangedHandler(long finished, long total);

        public event OnProgressChangedHandler OnProgressChanged;

        public delegate void OnExceptionHandler(Exception exception);

        public event OnExceptionHandler OnException;

        public Updater(string url)
        {
            this.url = url;
        }

        public void Update()
        {
            OnStatusChanged?.Invoke(UpdaterStatus.Start);

            string location = System.Reflection.Assembly.GetCallingAssembly().Location;
            string @base = Path.Combine(Environment.CurrentDirectory, ".updatemachine");

            if (location.EndsWith(".updatemachine"))
            {
                OnStatusChanged?.Invoke(UpdaterStatus.Copy);

                CopyNewFiles(@base, Environment.CurrentDirectory);
                if (Directory.Exists(@base)) Directory.Delete(@base, true);

                StartProcess(Regex.Replace(location, @"\.updatemachine\\\.updatemachine$", ""));
                Environment.Exit(0);
            }
            if (Directory.Exists(location + ".updatemachine")) Directory.Delete(location + ".updatemachine", true);

            OnStatusChanged?.Invoke(UpdaterStatus.Check);
            var list = CheckNewFiles();
            if (list.Count != 0)
            {
                OnStatusChanged?.Invoke(UpdaterStatus.Download);
                DownloadNewFiles(list);

                Directory.CreateDirectory(location + ".updatemachine");
                string destFileName = Path.Combine(location + ".updatemachine", ".updatemachine");
                File.Copy(location, destFileName, true);
                foreach (string newPath in Directory.GetFiles(Environment.CurrentDirectory, "*.dll", SearchOption.TopDirectoryOnly))
                    File.Copy(newPath, Path.Combine(location + ".updatemachine", newPath.Replace('/', '\\').Split('\\').Last()), true);

                StartProcess(destFileName);
                Environment.Exit(0);
            }
            OnStatusChanged?.Invoke(UpdaterStatus.Start);
        }

        private void StartProcess(string s)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = s,
                UseShellExecute = false
            };
            var v = Process.Start(processStartInfo);
        }

        private List<string> CheckNewFiles()
        {
            List<string> list = new List<string>();

            string value = null;

            var success = TryRun(new Action(() =>
              {
                  using (WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 })
                      value = webClient.DownloadString(Path.Combine(url, ".updatemachine", "index").Replace('\\', '/'));
              }), 3);
            if (!success) return list;

            FileIndex index = JsonConvert.DeserializeObject<FileIndex>(value);

            foreach (FileItem f in index.Files)
            {
                string s = Path.Combine(Environment.CurrentDirectory, f.Name);
                if ((!File.Exists(s)) || f.SHA1 != GetFileSHA1(s))
                {
                    list.Add(f.Name);
                }
            }
            return list;
        }

        private void DownloadNewFiles(List<string> list)
        {
            using (WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 })
            {
                list.ForEach(o =>
                {
                    TryRun(new Action(() =>
                    {
                        Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, ".updatemachine", o.Substring(0, o.LastIndexOf('/') <= 0 ? 0 : o.LastIndexOf('/'))));
                        webClient.DownloadFile(Path.Combine(url, o).Replace('\\', '/'), Path.Combine(Environment.CurrentDirectory, ".updatemachine", o));
                    }), 3);
                    OnProgressChanged?.Invoke(list.IndexOf(o) + 1, list.Count);
                });
            }
        }

        private void CopyNewFiles(string @base, string dest)
        {
            foreach (string dirPath in Directory.GetDirectories(@base, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(@base, dest));

            foreach (string newPath in Directory.GetFiles(@base, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(@base, dest), true);
        }

        public bool TryRun(Action action, int count)
        {
            int failCounter = 0;
        start:
            if (failCounter++ < count)
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    OnException?.Invoke(e);
                    Thread.Sleep(1000);
                    goto start;
                }
            else return false;
            return true;
        }

        public class FileIndex
        {
            [JsonProperty("files")]
            public FileItem[] Files { get; set; }
        }

        public class FileItem
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("sha1")]
            public string SHA1 { get; set; }
        }

        public enum UpdaterStatus
        {
            Check, Download, Copy, Start
        }

        public static string GetFileSHA1(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read);
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] r = sha1.ComputeHash(file);
            file.Close();

            StringBuilder sc = new StringBuilder();
            for (int i = 0; i < r.Length; i++)
            {
                sc.Append(r[i].ToString("X2"));
            }

            return sc.ToString();
        }
    }

}

using Aliyun.OSS;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using UpdateMachine.Core;

namespace UpdateMachine.Uploader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private string root;
        private FileConfiguration configuration;

        public MainWindow()
        {
            InitializeComponent();

            SetTitle(null);
            LoadWorkspaces();
        }

        private void LoadWorkspaces()
        {
            if (File.Exists("workspace.t"))
            {
                using (StreamReader s = new StreamReader(new FileStream("workspace.t", FileMode.Open, FileAccess.Read)))
                {
                    string str;
                    while ((str = s.ReadLine()) != null && str.Trim() != "")
                    {
                        TextboxWorkspace.Items.Add(str);
                    }
                }
                if (TextboxWorkspace.Items.Count > 0)
                {
                    TextboxWorkspace.SelectedIndex = 0;
                    LoadWorkspace();
                }
            }
        }

        private void SaveWorkspaces()
        {
            using (StreamWriter s = new StreamWriter(new FileStream("workspace.t", FileMode.Create, FileAccess.ReadWrite)))
            {
                foreach (var i in TextboxWorkspace.Items)
                {
                    s.WriteLine(i);
                }
            }
        }

        private void SetTitle(string s) => Title = "UpdateMachine " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ((s == null) ? "" : (" | " + s));

        private void ButtonLoad_Click(object sender, RoutedEventArgs e) => LoadWorkspace();

        private void LoadWorkspace()
        {
            if (Directory.Exists(TextboxWorkspace.Text))
            {
                if (!(TextboxWorkspace.Text.EndsWith("/") || TextboxWorkspace.Text.EndsWith("\\")))
                {
                    TextboxWorkspace.Text += "\\";
                }
                root = TextboxWorkspace.Text.Replace('\\', '/');

                configuration = Configuration.Load(root);
                LoadFromConfiguration();

                RefreshFileList();
                SetTitle(TextboxWorkspace.Text);
                GridMain.IsEnabled = true;

                string text = TextboxWorkspace.Text;
                TextboxWorkspace.Items.Remove(text);
                TextboxWorkspace.Items.Insert(0, text);
                TextboxWorkspace.SelectedIndex = 0;
            }
            else
            {
                SetTitle(null);
                GridMain.IsEnabled = false;
            }
        }

        private void LoadFromConfiguration()
        {
            if (configuration == null) return;

            TextboxEndpoint.Text = configuration.endpoint;
            TextboxAccessKeyId.Text = configuration.accessKeyId;
            TextboxAccessKeySecret.Text = configuration.accessKeySecret;
            TextboxBucketName.Text = configuration.bucketName;
            TextboxRelativePath.Text = configuration.path;
            TextboxPublicUrl.Text = configuration.publicUrl;
        }

        private void SaveToConfiguration()
        {
            if (configuration == null) return;

            configuration.endpoint = TextboxEndpoint.Text;
            configuration.accessKeyId = TextboxAccessKeyId.Text;
            configuration.accessKeySecret = TextboxAccessKeySecret.Text;
            configuration.bucketName = TextboxBucketName.Text;
            configuration.path = TextboxRelativePath.Text;
            configuration.publicUrl = TextboxPublicUrl.Text;

            Configuration.Save(root, configuration);
        }

        private void RefreshFileList()
        {
            ListboxFiles.Items.Clear();
            DirectoryInfo dir = new DirectoryInfo(root);
            foreach (FileInfo f in dir.GetFiles("*.*", SearchOption.AllDirectories))
            {
                string newItem = f.FullName.Replace('\\', '/').Replace(root, "");
                if (newItem.StartsWith(".updatemachine")) continue;
                ListboxFiles.Items.Add(newItem);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveToConfiguration();
            SaveWorkspaces();
        }

        private void ButtonRefreshList_Click(object sender, RoutedEventArgs e) => RefreshFileList();

        private List<string> CheckUploadFiles()
        {
            List<string> list = null;
            Dispatcher.Invoke(new Action(() => list = new List<string>(ListboxFiles.Items.Cast<string>())));

            try
            {
                string value;
                using (WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 })
                    value = webClient.DownloadString(Path.Combine(configuration.publicUrl, ".updatemachine", "index").Replace('\\', '/'));

                Updater.FileIndex index = JsonConvert.DeserializeObject<Updater.FileIndex>(value);

                foreach (Updater.FileItem f in index.Files)
                {
                    string s = Path.Combine(root, f.Name);
                    if (File.Exists(s) && f.SHA1 == Updater.GetFileSHA1(s))
                    {
                        list.Remove(f.Name);
                    }
                }
            }
            catch { }

            return list;
        }

        private string BuildIndex()
        {
            List<string> list = null;
            Dispatcher.Invoke(new Action(() => list = new List<string>(ListboxFiles.Items.Cast<string>())));

            List<Updater.FileItem> fileItems = new List<Updater.FileItem>();

            list.ForEach(f =>
            {
                string s = Path.Combine(root, f);
                fileItems.Add(new Updater.FileItem() { Name = f, SHA1 = Updater.GetFileSHA1(s) });
            });

            Updater.FileIndex index = new Updater.FileIndex() { Files = fileItems.ToArray() };

            return JsonConvert.SerializeObject(index);
        }

        private void ButtonUpload_Click(object sender, RoutedEventArgs e)
        {
            SaveToConfiguration();
            if (TextboxVersion.Text.Trim() == "")
            {
                MessageBox.Show("需要输入版本号", "你错了");
                return;
            }
            if (TextboxLog.Text.Trim() == "")
            {
                MessageBox.Show("需要输入更新内容", "你错了");
                return;
            }

            GridMain.IsEnabled = false;
            new Thread(() =>
            {
                OssClient client = new OssClient(configuration.endpoint, configuration.accessKeyId, configuration.accessKeySecret);

                int count = 0;

                retry:
                count++;
                if (count > 3) { goto stop; }

                try
                {
                    List<string> l = CheckUploadFiles();
                    l.ForEach(f =>
                    {
                        client.PutObject(configuration.bucketName, Path.Combine(configuration.path, f).Replace('\\', '/'), Path.Combine(root, f));
                    });
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); goto retry; }
                try
                {
                    var m = new MemoryStream(Encoding.UTF8.GetBytes(BuildIndex()));
                    client.PutObject(configuration.bucketName, Path.Combine(configuration.path, ".updatemachine", "index").Replace('\\', '/'), m);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); goto retry; }
                try
                {
                    var vs = VersionTookit.GetVersionFile(configuration.publicUrl);
                    List<VersionTookit.VersionItem> items = new List<VersionTookit.VersionItem>();
                    items.AddRange(vs.versions);

                    string tv = null, tl = null;
                    Dispatcher.Invoke(new Action(() => { tv = TextboxVersion.Text; tl = TextboxLog.Text; }));
                    items.Add(new VersionTookit.VersionItem() { version = tv, log = tl });
                    vs.versions = items.ToArray();

                    var m = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(vs)));
                    client.PutObject(configuration.bucketName, Path.Combine(configuration.path, ".updatemachine", "versions").Replace('\\', '/'), m);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); goto retry; }

                stop:
                Dispatcher.Invoke(new Action(() => GridMain.IsEnabled = true));
            }).Start();
        }
    }
}

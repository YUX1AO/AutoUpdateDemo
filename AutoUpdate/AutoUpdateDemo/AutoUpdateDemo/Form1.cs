using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using Newtonsoft.Json;

namespace AutoUpdateDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //一直检查更新
        private void alwayscheck()
        {
            // winform
            System.Timers.Timer timer = new System.Timers.Timer
            {
                Interval = 2 * 60 * 1000,
                SynchronizingObject = this
            };
            timer.Elapsed += delegate
            {
                AutoUpdater.Start("http://rbsoft.org/updates/AutoUpdaterTest.xml");
            };
            timer.Start();

            //Wmf
            //DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            //timer.Tick += delegate
            //{
            //    AutoUpdater.Start("http://rbsoft.org/updates/AutoUpdaterTestWPF.xml");
            //};
            //timer.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //手动处理更新逻辑，可以通过判断本地的版本号对比
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            //退出逻辑
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            //更新版本信息传递，其他非xml格式更新文件
            AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;

            //稍后提醒时间
            AutoUpdater.LetUserSelectRemindLater = false;
            AutoUpdater.RemindLaterTimeSpan = RemindLaterFormat.Hours;
            AutoUpdater.RemindLaterAt = 2;

            //指定下载目录
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;

            //指定提取包含更新文件的zip文件的位置
            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            if (currentDirectory.Parent != null)
            {
                AutoUpdater.InstallationPath = currentDirectory.Parent.FullName;
            }

            //窗体大小
            AutoUpdater.UpdateFormSize = new System.Drawing.Size(800, 600);

            //示例一 最简单方法
            //自动更新寻找有关软件最新版本的发行信息 xml文件，使用程序集版本来确定应用程序的当前版本
            //1
            AutoUpdater.Start("http://localhost:5000/AutoUpdateDemo.json"); //默认调用程序版本
            //2
            //Assembly assembly = Assembly.GetExecutingAssembly();
            //AutoUpdater.Start("http://rbsoft.org/updates/AutoUpdaterTest.xml", myAssembly);

            //示例二 FTP下载
            //AutoUpdater.Start("ftp://rbsoft.org/updates/AutoUpdaterTest.xml", new NetworkCredential("FtpUserName", "FtpPassword"));

        }
        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                if (args.IsUpdateAvailable)
                {
                    DialogResult dialogResult;
                    if (args.Mandatory.Value)
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.", @"Update Available",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                    }
                    else
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {
                                        args.InstalledVersion
                                    }. Do you want to update the application now?", @"Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);
                    }

                    // Uncomment the following line if you want to show standard update dialog instead.
                    AutoUpdater.ShowUpdateForm(args);

                    if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate(args))
                            {
                                Application.Exit();
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show(@"There is no update available please try again later.", @"No update available",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(
                        @"There is a problem reaching update server please check your internet connection and try again later.",
                        @"Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AutoUpdater_ApplicationExitEvent()
        {
            Text = @"Closing application...";
            Thread.Sleep(5000);
            Application.Exit();
        }

        private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = json.version,
                ChangelogURL = json.changelog,
                DownloadURL = json.url,
                Mandatory = new Mandatory
                {
                    Value = json.mandatory.value,
                    UpdateMode = json.mandatory.mode,
                    MinimumVersion = json.mandatory.minVersion
                },
                CheckSum = new CheckSum
                {
                    Value = json.checksum.value,
                    HashingAlgorithm = json.checksum.hashingAlgorithm
                }
            };
        }
    }
}

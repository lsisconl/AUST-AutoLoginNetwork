using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoLoginNetwork
{
    public partial class MainWindow : Window
    {
        public static string applicationFullPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        public static string shortFileName = applicationFullPath.Substring(applicationFullPath.LastIndexOf('\\') + 1);
        public static string applicationPath = applicationFullPath.Replace($"\\{shortFileName}", "");
        public static string passwordPath = applicationPath + "\\password.config";
        public MainWindow()
        {
            InitializeComponent();
            //判断当前登录用户是否为管理员
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                RunAtStart();
            }
            if (File.Exists(passwordPath))
            {
                StreamReader sr = new StreamReader(passwordPath, Encoding.Default);
                var text = sr.ReadToEnd().Split("|");
                sr.Close();
                if(text.Length==3 && int.TryParse(text[2],out var value))
                {
                    account.Text = text[0];
                    password.Text = text[1];
                    combobox.SelectedIndex = value;
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
                    IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv.Invoke();
                }
            }
            else
            {
                MessageBox.Show("检测到您是第一次打开此程序，添加程序自启动时需要使用管理员运行此程序\n作者：siscon\nGithub:https://github.com/lsisconl/AUST-AutoLoginNetwork", "提示");
            }
        }

        public void ButtonClick(object sender, RoutedEventArgs e)
        {
            string type = "aust";
           switch(combobox.SelectedIndex)
            {
                case 0:
                    type = "jzg";
                    break;
                case 1:
                    type = "aust";
                    break;
                case 2:
                    type = "unicom";
                    break;
                case 3:
                    type = "cmcc";
                    break;
                default:
                    type = "aust";
                    break;
            }
            try
            {
                var content = GetHttpResponse($"http://10.255.0.19/drcom/login?callback=dr1003&DDDDD={account.Text}%40{type}&upass={password.Text}&0MKKey=123456");
                if (content.Contains("\"result\":1"))
                {
                    if (!File.Exists(passwordPath))
                    {
                        File.Create(passwordPath).Close();
                    }
                    using (FileStream fs = new FileStream(passwordPath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes($"{account.Text}|{password.Text}|{combobox.SelectedIndex}");
                        fs.Write(buffer, 0, buffer.Length);
                    }
                    Task.Run(() =>
                    {
                        Thread.Sleep(3000);
                        KillMessageBox();
                    });
                    MessageBox.Show("登陆成功","提示");
                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show("登陆失败","提示");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void RunAtStart()
        {
            RunCmd($"schtasks /delete /tn AutoLoginNetwork /F");
            MessageBox.Show(RunCmd($"schtasks /Create /SC ONEVENT /MO \" *[System[(EventID = 4624)]] and *[EventData[Data[9] = \"7\"]]\" /EC Security /TN \"AutoLoginNetwork\" /TR \"{applicationFullPath}\""), "提示");
            MessageBox.Show("添加自启动成功", "提示");
        }


        private string RunCmd(string command)
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + command;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();   
            return p.StandardOutput.ReadToEnd();
        }

        public string GetHttpResponse(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent = null;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public const int WM_CLOSE = 0x10;

        private void KillMessageBox()
        {
            //查找MessageBox的弹出窗口,注意MessageBox对应的标题
            IntPtr ptr = FindWindow(null, "提示");
            if (ptr != IntPtr.Zero)
            {
                //查找到窗口则关闭
                PostMessage(ptr, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}

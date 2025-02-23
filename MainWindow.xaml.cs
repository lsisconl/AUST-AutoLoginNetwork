using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace AutoLoginNetwork
{
    public partial class MainWindow
    {
        private static readonly string ApplicationFullPath = Process.GetCurrentProcess().MainModule?.FileName;
        private static readonly string ShortFileName = ApplicationFullPath.Substring(ApplicationFullPath.LastIndexOf('\\') + 1);
        private static readonly string ApplicationPath = ApplicationFullPath.Replace($"\\{ShortFileName}", "");
        private static readonly string PasswordPath = ApplicationPath + "\\password.config";
        private static readonly HttpClient HttpClient = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();   
            EncodingProvider provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);
            if (File.Exists(PasswordPath))
            {
                StreamReader sr = new StreamReader(PasswordPath, Encoding.Default);
                var text = sr.ReadToEnd().Split("|");
                sr.Close();
                if(text.Length==3 && int.TryParse(text[2],out var value))
                {
                    account.Text = text[0];
                    password.Password = text[1];
                    combobox.SelectedIndex = value;
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
                    if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProv)
                    {
                        invokeProv.Invoke();
                    }
                }
            }
        }

        private async void ButtonClick(object sender, RoutedEventArgs e)
        { 
            string type;
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
                var content = await GetHttpResponseAsync($"http://10.255.0.19/drcom/login?callback=dr1003&DDDDD={account.Text}%40{type}&upass={password.Password}&0MKKey=123456");
                if (content.Contains("\"result\":1"))
                {
                    if (!File.Exists(PasswordPath))
                    {
                        File.Create(PasswordPath).Close();
                    }

                    await using (FileStream fs = new FileStream(PasswordPath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        byte[] buffer = Encoding.UTF8.GetBytes($"{account.Text}|{password.Password}|{combobox.SelectedIndex}");
                        fs.Write(buffer, 0, buffer.Length);
                    }
                    Task.Run(() =>
                    {
                        Thread.Sleep(2000);
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

        public void OnAddSelfStartButtonClick(object sender, RoutedEventArgs e)
        {
            if (JudgeIfAdmin())
            {
                RunAtStart();
            }
            else
            {
                MessageBox.Show("没有管理员权限，请用管理员启动程序");
            }
        }

        public void OnDeleteSelfStartButtonClick(object sender, RoutedEventArgs e)
        {
            if (JudgeIfAdmin())
            {
                DeleteRunAtStart();
            }
            else
            {
                MessageBox.Show("没有管理员权限，请用管理员启动程序");
            }
        }

        public void AboutButtonClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("此软件完全免费，已开源至Github。作者：siscon\nGithub:https://github.com/lsisconl/AUST-AutoLoginNetwork", "提示");
        }

        bool JudgeIfAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                return true;
            }
            return false;
        }

        private void RunAtStart()
        {
            RunCmd($"schtasks /delete /tn AutoLoginNetwork /F");
            MessageBox.Show(RunCmd($"schtasks /Create /SC ONEVENT /MO \" *[System[(EventID = 4624)]] and *[EventData[Data[9] = \"7\"]]\" /EC Security /TN \"AutoLoginNetwork\" /TR \"{ApplicationFullPath}\""), "提示");
        }

        private void DeleteRunAtStart()
        {
            MessageBox.Show(RunCmd($"schtasks /delete /tn AutoLoginNetwork /F"));
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

        /*private string GetHttpResponse(string url)
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
        }*/

        private async Task<string> GetHttpResponseAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WmClose = 0x10;

        private void KillMessageBox()
        {
            //查找MessageBox的弹出窗口,注意MessageBox对应的标题
            IntPtr ptr = FindWindow(null, "提示");
            if (ptr != IntPtr.Zero)
            {
                //查找到窗口则关闭
                PostMessage(ptr, WmClose, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
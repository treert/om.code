using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownloadWebsite
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
            Worker.singleton.m_add_log += AsyncAddLog;
            Worker.singleton.m_refresh_status += AsyncRefreshStatus;

            //richTextBox_log.VScroll += (object sender, EventArgs e) => {
            //    // ??
            //};
            this.button_auto_scroll_log.Text = m_auto_scroll_log ? "停止滚动" : "自动滚动";

            RefreshStatusAndLogAndUI();

            FillUI();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Worker.singleton.IsWorking())
            {
                MessageBox.Show("正在下载，先取消下载");
                return;
            }
            // > https://stackoverflow.com/questions/844423/set-folder-browser-dialog-start-location
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            var dir = XConfig.GetString(s_key_save_dir);
            if(Directory.Exists(dir) == false)
            {
                try
                {
                    dir = Path.GetDirectoryName(dir);
                }
                catch (Exception) { }
            }
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            dialog.SelectedPath = dir;
            dialog.ShowNewFolderButton = true;
            dialog.Description = "选择下载的根目录";

            if(dialog.ShowDialog() == DialogResult.OK)
            {
                this.linkLabel_save_dir.Text = dialog.SelectedPath;
                XConfig.SetString(s_key_save_dir, dialog.SelectedPath);
            }
        }

        public void AsyncAddLog(string msg)
        {
            this.Invoke(new Action(() =>
            {
                this.richTextBox_log.AppendText(msg+"\n");
                
                ScrollLogToEnd();
                //lock (Worker.singleton)
                //{
                //    this.richTextBox_log.Lines = Worker.singleton.m_log_list.ToArray();
                //}
            }));
        }

        // > https://stackoverflow.com/questions/9416608/rich-text-box-scroll-to-the-bottom-when-new-data-is-written-to-it
        void ScrollLogToEnd()
        {
            if (m_auto_scroll_log)
            {
                this.richTextBox_log.SelectionStart = this.richTextBox_log.Text.Length;
                this.richTextBox_log.ScrollToCaret();
            }
        }

        public void AsyncRefreshStatus()
        {
            this.Invoke(new Action(() =>
            {
                this.label_status.Text = Worker.singleton.m_status;
                bool is_working = Worker.singleton.m_thread_cnt > 0;
                RefreshUIByWorkingStatus(is_working);
            }));
        }

        public void RefreshStatusAndLogAndUI()
        {
            this.ClearLog();
            this.label_status.Text = Worker.singleton.m_status;
            RefreshUI();
        }

        public void RefreshUIByWorkingStatus(bool is_working)
        {
            this.textBox_web_root.ReadOnly = is_working;
            this.textBox_proxy_host.ReadOnly = is_working;
            this.textBox_proxy_port.ReadOnly = is_working;
            this.textBox_proxy_user.ReadOnly = is_working;
            this.textBox_proxy_pwd.ReadOnly = is_working;
            //this.checkBox_use_proxy.Enabled = !is_working;
            //this.checkBox_force_down.Enabled = !is_working;
        }

        void ClearLog()
        {
            this.richTextBox_log.Clear();
        }

        static readonly string s_key_save_dir = "last.save.dir";
        static readonly string s_key_website = "last.website";
        static readonly string s_key_cpu_num = "last.cpu.num";
        static readonly string s_key_proxy_use = "last.proxy.use";
        static readonly string s_key_proxy_host = "last.proxy.host";
        static readonly string s_key_proxy_port = "last.proxy.port";
        static readonly string s_key_proxy_user = "last.proxy.user";
        static readonly string s_key_proxy_pwd = "last.proxy.pwd";
        static readonly string s_key_careful_mode_open = "last.careful.mode.open";
        static readonly string s_key_force_download = "last.force.download";
        void RefreshUI()
        {
            bool is_working = Worker.singleton.IsWorking();
            RefreshUIByWorkingStatus(is_working);
            var def_save_dir = Path.Combine(Environment.CurrentDirectory, "website");
            var def_website = "";

            this.linkLabel_save_dir.Text = XConfig.GetString(s_key_save_dir,def_save_dir);
            this.textBox_web_root.Text = XConfig.GetString(s_key_website, def_website);
            this.comboBox_thread_cnt.Text = XConfig.GetString(s_key_cpu_num, "4");
        }

        void FillUI()
        {
            // Proxy
            this.checkBox_use_proxy.Checked = XConfig.GetBool(s_key_proxy_use, false);
            this.textBox_proxy_host.Text = XConfig.GetString(s_key_proxy_host);
            this.textBox_proxy_port.Text = XConfig.GetInt(s_key_proxy_port).ToString();
            this.textBox_proxy_user.Text = XConfig.GetString(s_key_proxy_user);
            this.textBox_proxy_pwd.Text = XConfig.GetString(s_key_proxy_pwd);

            // Mode
            this.checkBox_careful_mode_open.Checked = XConfig.GetBool(s_key_careful_mode_open, false);

            this.checkBox_force_down.Checked = XConfig.GetBool(s_key_force_download, false);
        }



        private void button2_Click(object sender, EventArgs e)
        {
            var web_root = textBox_web_root.Text;
            if (string.IsNullOrEmpty(web_root))
            {
                MessageBox.Show("先选择网址");
                return;
            }
            var save_dir = this.linkLabel_save_dir.Text;
            if (string.IsNullOrEmpty(save_dir))
            {
                MessageBox.Show("保存位置不能为空");
                return;
            }

            //if (XConfig.GetBool(s_key_careful_mode_open, true))
            //{
            //    //XUrl.TryParser(web_root);
            //    //var res = XWebClient.GetHttpResponseUrl(web_root);
            //    //XUrl.ConvertUrlFragToPath(web_root);
            //    return;
            //}

            int cnt = 4;
            if(int.TryParse(this.comboBox_thread_cnt.Text, out cnt) == false)
            {
                cnt = 4;
            }
            cnt = Math.Max(1, cnt);

            XConfig.SetString(s_key_cpu_num, cnt.ToString());
            XConfig.SetString(s_key_website, web_root);

            this.ClearLog();

            var proxy_use = XConfig.GetBool(s_key_proxy_use,false);
            var proxy_host = XConfig.GetString(s_key_proxy_host);
            var proxy_port = XConfig.GetInt(s_key_proxy_port);
            var proxy_user = XConfig.GetString(s_key_proxy_user);
            var proxy_pwd = XConfig.GetString(s_key_proxy_pwd);

            var force_download = XConfig.GetBool(s_key_force_download);

            if(string.IsNullOrEmpty(proxy_host) || proxy_port <= 0)
            {
                proxy_use = false;
                XConfig.SetBool(s_key_proxy_use, proxy_use);
            }

            Worker.singleton.SetProxyInfo(proxy_use, proxy_host, proxy_port, proxy_user, proxy_pwd);
            Worker.singleton.m_bao_shou_mode = XConfig.GetBool(s_key_careful_mode_open, false);
            Worker.singleton.StartDownload(web_root,save_dir, force_download, cnt);
            RefreshUI();
            FillUI();
            XLocalSave.singleton.SaveConfig();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Worker.singleton.AbortDownload();
            //RefreshUI();
        }

        private void richTextBox_log_VScroll(object sender, EventArgs e)
        {

        }

        bool m_auto_scroll_log = false;// 会卡
        private void button_auto_scroll_log_Click(object sender, EventArgs e)
        {
            m_auto_scroll_log = !m_auto_scroll_log;
            this.button_auto_scroll_log.Text = m_auto_scroll_log ? "停止滚动" : "自动滚动";
        }

        // 打开资源管理器
        // > https://blog.csdn.net/chen8643766/article/details/19755639
        public static void OpenExplorer(string dir)
        {
            System.Diagnostics.Process.Start("Explorer.exe", dir);
        }

        public static void OpenDirWithDefaultProgram(string dir)
        {
            System.Diagnostics.Process.Start(dir);
        }

        private void richTextBox_log_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            OpenDirWithDefaultProgram(e.LinkText);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExplorer(this.linkLabel_save_dir.Text);
        }

        private void comboBox_thread_cnt_SelectedIndexChanged(object sender, EventArgs e)
        {
            XConfig.SetString(s_key_cpu_num, this.comboBox_thread_cnt.Text);
        }

        private void textBox_web_root_TextChanged(object sender, EventArgs e)
        {
            XConfig.SetString(s_key_website, this.textBox_web_root.Text);
        }

        private void checkBox_use_proxy_CheckedChanged(object sender, EventArgs e)
        {
            XConfig.SetBool(s_key_proxy_use, this.checkBox_use_proxy.Checked);
        }

        private void textBox_proxy_host_TextChanged(object sender, EventArgs e)
        {
            XConfig.SetString(s_key_proxy_host, this.textBox_proxy_host.Text);
        }

        private void textBox_proxy_port_TextChanged(object sender, EventArgs e)
        {
            string port_str = this.textBox_proxy_port.Text;
            int port;
            if (int.TryParse(port_str, out port) == false) port = 80;
            XConfig.SetInt(s_key_proxy_port, port);
        }

        private void textBox_proxy_user_TextChanged(object sender, EventArgs e)
        {
            XConfig.SetString(s_key_proxy_user, this.textBox_proxy_user.Text);
        }

        private void textBox_proxy_pwd_TextChanged(object sender, EventArgs e)
        {
            XConfig.SetString(s_key_proxy_pwd, this.textBox_proxy_pwd.Text);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            XLocalSave.singleton.SaveConfig();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            XConfig.SetBool(s_key_careful_mode_open, this.checkBox_careful_mode_open.Checked);
        }

        private void checkBox_force_down_CheckedChanged(object sender, EventArgs e)
        {
            XConfig.SetBool(s_key_force_download, this.checkBox_force_down.Checked);
        }
    }
}

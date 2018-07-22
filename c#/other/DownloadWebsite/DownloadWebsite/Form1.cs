using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

            Worker.singleton.Init();
            Worker.singleton.m_refresh_log += RefreshLog;
            RefreshUI();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Worker.singleton.IsWorking())
            {
                MessageBox.Show("正在下载，先取消下载");
                return;
            }

            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.Description = "选择下载的根目录";

            if(dialog.ShowDialog() == DialogResult.OK)
            {
                this.label_save_dir.Text = dialog.SelectedPath;
            }
        }

        public void RefreshLog()
        {
            this.Invoke(new Action(() =>
            {
                this.richTextBox_log.Lines = Worker.singleton.m_log_list.ToArray();
                this.label_status.Text = Worker.singleton.m_status;
            }));
        }

        void RefreshUI()
        {
            bool is_working = Worker.singleton.IsWorking();
            this.textBox_web_root.ReadOnly = is_working;
            this.label_save_dir.Text = Worker.singleton.m_save_dir;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var web_root = textBox_web_root.Text;
            if (string.IsNullOrEmpty(web_root))
            {
                MessageBox.Show("先选择网址");
                return;
            }
            var save_dir = this.label_save_dir.Text;
            if (string.IsNullOrEmpty(save_dir))
            {
                MessageBox.Show("保存位置不能为空");
                return;
            }

            int cnt = 8;
            if(int.TryParse(this.comboBox_thread_cnt.Text, out cnt) == false)
            {
                cnt = 8;
            }
            cnt = Math.Max(1, cnt);
            Worker.singleton.StartDownload(web_root,save_dir,this.checkBox_force_down.Checked, cnt);
            RefreshUI();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Worker.singleton.AbortDownload();
            RefreshUI();
        }
    }
}

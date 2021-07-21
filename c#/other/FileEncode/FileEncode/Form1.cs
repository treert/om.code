using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileEncode
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            listView1.AllowDrop = true;
            WorkModel.singleton.m_updateFileList += UpdateFileListView;
            WorkModel.singleton.m_updateProcess += UpdateProcess;
            WorkModel.singleton.m_showMessage += ShowMessage;
            progressBar1.Visible = false;

            listView1.FullRowSelect = true;// 可以选择行
            listView1.MultiSelect = true;
        }

        private void ShowMessage(string msg)
        {
            if(InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    MessageBox.Show(msg);
                }));
            }
            else
            {
                MessageBox.Show(msg);
            }
        }

        private void UpdateFileListView()
        {
            this.Invoke(new Action(() => {
                RefreashFileListView(null);
            }));
        }

        private void RefreashFileListView(string search)
        {

            _file_info_list.Clear();
            foreach (var info in WorkModel.singleton.GetFileInfoList())
            {
                if(string.IsNullOrWhiteSpace(search)
                    || info.file_short_name.ToLower().IndexOf(search.ToLower()) >= 0
                    || info.encoding_name.ToLower().StartsWith(search.ToLower()))
                {
                    _file_info_list.Add(info);
                }
            }

            listView1.BeginUpdate();
            listView1.Items.Clear();
            listView1.VirtualMode = true;
            listView1.VirtualListSize = _file_info_list.Count;
            listView1.EndUpdate();
        }

        private void UpdateProcess(int percent)
        {
            this.Invoke(new Action(() =>
            {
                if(percent >= 0 && percent <= 100)
                {
                    progressBar1.Visible = true;
                    progressBar1.Value = percent;
                }
                else
                {
                    progressBar1.Visible = false;
                }
            }));
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            var files = ((string[])e.Data.GetData(DataFormats.FileDrop));//文件路径+文件名
            WorkModel.singleton.AddFiles(files);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WorkModel.singleton.ConvertToUtf8();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            WorkModel.singleton.Clear();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            RefreashFileListView(textBox1.Text);
        }

        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var item = _file_info_list[e.ItemIndex];
            e.Item = new ListViewItem(item.encoding_name);
            e.Item.SubItems.Add(item.file_short_name);
        }

        private List<ItemData> _file_info_list = new List<ItemData>();

        private void button3_Click(object sender, EventArgs e)
        {
            WorkModel.singleton.ConvertToUtf8(true);
        }
    }
}

namespace DownloadWebsite
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_web_root = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.richTextBox_log = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.checkBox_force_down = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_thread_cnt = new System.Windows.Forms.ComboBox();
            this.linkLabel_save_dir = new System.Windows.Forms.LinkLabel();
            this.label_status = new System.Windows.Forms.Label();
            this.button_auto_scroll_log = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBox_use_proxy = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_proxy_pwd = new System.Windows.Forms.TextBox();
            this.textBox_proxy_user = new System.Windows.Forms.TextBox();
            this.textBox_proxy_port = new System.Windows.Forms.TextBox();
            this.textBox_proxy_host = new System.Windows.Forms.TextBox();
            this.checkBox_careful_mode_open = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_web_root
            // 
            this.textBox_web_root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_web_root.Location = new System.Drawing.Point(83, 3);
            this.textBox_web_root.MaxLength = 512;
            this.textBox_web_root.Name = "textBox_web_root";
            this.textBox_web_root.Size = new System.Drawing.Size(712, 21);
            this.textBox_web_root.TabIndex = 0;
            this.textBox_web_root.Text = "http://llvm.org/docs/index.html";
            this.textBox_web_root.TextChanged += new System.EventHandler(this.textBox_web_root_TextChanged);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.label1.Size = new System.Drawing.Size(74, 30);
            this.label1.TabIndex = 1;
            this.label1.Text = "website";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.Location = new System.Drawing.Point(3, 33);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(74, 34);
            this.button1.TabIndex = 3;
            this.button1.Text = "SelectDir";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // richTextBox_log
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.richTextBox_log, 2);
            this.richTextBox_log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_log.Location = new System.Drawing.Point(3, 103);
            this.richTextBox_log.Name = "richTextBox_log";
            this.richTextBox_log.ReadOnly = true;
            this.richTextBox_log.Size = new System.Drawing.Size(792, 294);
            this.richTextBox_log.TabIndex = 4;
            this.richTextBox_log.Text = "1\n2";
            this.richTextBox_log.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBox_log_LinkClicked);
            this.richTextBox_log.VScroll += new System.EventHandler(this.richTextBox_log_VScroll);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.richTextBox_log, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.button1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBox_web_root, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label_status, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.button_auto_scroll_log, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBox_careful_mode_open, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(798, 430);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 6;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel2.Controls.Add(this.button2, 4, 0);
            this.tableLayoutPanel2.Controls.Add(this.button3, 5, 0);
            this.tableLayoutPanel2.Controls.Add(this.checkBox_force_down, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.label3, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.comboBox_thread_cnt, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.linkLabel_save_dir, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(83, 33);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(712, 34);
            this.tableLayoutPanel2.TabIndex = 5;
            // 
            // button2
            // 
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.Location = new System.Drawing.Point(555, 3);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(74, 28);
            this.button2.TabIndex = 1;
            this.button2.Text = "DownLoad";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button3.Location = new System.Drawing.Point(635, 3);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(74, 28);
            this.button3.TabIndex = 2;
            this.button3.Text = "Cancel";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // checkBox_force_down
            // 
            this.checkBox_force_down.AutoSize = true;
            this.checkBox_force_down.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox_force_down.Location = new System.Drawing.Point(475, 3);
            this.checkBox_force_down.Name = "checkBox_force_down";
            this.checkBox_force_down.Size = new System.Drawing.Size(74, 28);
            this.checkBox_force_down.TabIndex = 3;
            this.checkBox_force_down.Text = "强制下载";
            this.checkBox_force_down.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBox_force_down.UseVisualStyleBackColor = true;
            this.checkBox_force_down.CheckedChanged += new System.EventHandler(this.checkBox_force_down_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(355, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 34);
            this.label3.TabIndex = 4;
            this.label3.Text = "线程数";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBox_thread_cnt
            // 
            this.comboBox_thread_cnt.AutoCompleteCustomSource.AddRange(new string[] {
            "1",
            "4",
            "8",
            "16",
            "32",
            "64",
            "128"});
            this.comboBox_thread_cnt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBox_thread_cnt.FormatString = "N0";
            this.comboBox_thread_cnt.FormattingEnabled = true;
            this.comboBox_thread_cnt.Items.AddRange(new object[] {
            "1",
            "4",
            "8",
            "16",
            "32",
            "64",
            "128"});
            this.comboBox_thread_cnt.Location = new System.Drawing.Point(415, 3);
            this.comboBox_thread_cnt.Name = "comboBox_thread_cnt";
            this.comboBox_thread_cnt.Size = new System.Drawing.Size(54, 20);
            this.comboBox_thread_cnt.TabIndex = 5;
            this.comboBox_thread_cnt.Text = "4";
            this.comboBox_thread_cnt.SelectedIndexChanged += new System.EventHandler(this.comboBox_thread_cnt_SelectedIndexChanged);
            // 
            // linkLabel_save_dir
            // 
            this.linkLabel_save_dir.AutoSize = true;
            this.linkLabel_save_dir.Dock = System.Windows.Forms.DockStyle.Fill;
            this.linkLabel_save_dir.Location = new System.Drawing.Point(3, 0);
            this.linkLabel_save_dir.Name = "linkLabel_save_dir";
            this.linkLabel_save_dir.Size = new System.Drawing.Size(346, 34);
            this.linkLabel_save_dir.TabIndex = 6;
            this.linkLabel_save_dir.TabStop = true;
            this.linkLabel_save_dir.Text = "linkLabel_save_dir";
            this.linkLabel_save_dir.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.linkLabel_save_dir.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label_status
            // 
            this.label_status.AutoSize = true;
            this.label_status.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label_status.Location = new System.Drawing.Point(83, 400);
            this.label_status.Name = "label_status";
            this.label_status.Size = new System.Drawing.Size(712, 30);
            this.label_status.TabIndex = 7;
            this.label_status.Text = "label_status";
            this.label_status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // button_auto_scroll_log
            // 
            this.button_auto_scroll_log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button_auto_scroll_log.Location = new System.Drawing.Point(3, 403);
            this.button_auto_scroll_log.Name = "button_auto_scroll_log";
            this.button_auto_scroll_log.Size = new System.Drawing.Size(74, 24);
            this.button_auto_scroll_log.TabIndex = 8;
            this.button_auto_scroll_log.Text = "自动滚动";
            this.button_auto_scroll_log.UseVisualStyleBackColor = true;
            this.button_auto_scroll_log.Click += new System.EventHandler(this.button_auto_scroll_log_Click);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 9;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55.55556F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22.22222F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22.22222F));
            this.tableLayoutPanel3.Controls.Add(this.checkBox_use_proxy, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label4, 3, 0);
            this.tableLayoutPanel3.Controls.Add(this.label5, 5, 0);
            this.tableLayoutPanel3.Controls.Add(this.label6, 7, 0);
            this.tableLayoutPanel3.Controls.Add(this.textBox_proxy_pwd, 8, 0);
            this.tableLayoutPanel3.Controls.Add(this.textBox_proxy_user, 6, 0);
            this.tableLayoutPanel3.Controls.Add(this.textBox_proxy_port, 4, 0);
            this.tableLayoutPanel3.Controls.Add(this.textBox_proxy_host, 2, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(83, 73);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(712, 24);
            this.tableLayoutPanel3.TabIndex = 10;
            // 
            // checkBox_use_proxy
            // 
            this.checkBox_use_proxy.AutoSize = true;
            this.checkBox_use_proxy.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox_use_proxy.Location = new System.Drawing.Point(3, 3);
            this.checkBox_use_proxy.Name = "checkBox_use_proxy";
            this.checkBox_use_proxy.Size = new System.Drawing.Size(74, 18);
            this.checkBox_use_proxy.TabIndex = 10;
            this.checkBox_use_proxy.Text = "使用代理";
            this.checkBox_use_proxy.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.checkBox_use_proxy.UseVisualStyleBackColor = true;
            this.checkBox_use_proxy.CheckedChanged += new System.EventHandler(this.checkBox_use_proxy_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(83, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 24);
            this.label2.TabIndex = 0;
            this.label2.Text = "地址:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(339, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(44, 24);
            this.label4.TabIndex = 1;
            this.label4.Text = "端口:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(439, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 24);
            this.label5.TabIndex = 2;
            this.label5.Text = "用户名:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(581, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(44, 24);
            this.label6.TabIndex = 3;
            this.label6.Text = "密码:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBox_proxy_pwd
            // 
            this.textBox_proxy_pwd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_proxy_pwd.Location = new System.Drawing.Point(631, 3);
            this.textBox_proxy_pwd.MaxLength = 64;
            this.textBox_proxy_pwd.Name = "textBox_proxy_pwd";
            this.textBox_proxy_pwd.Size = new System.Drawing.Size(78, 21);
            this.textBox_proxy_pwd.TabIndex = 4;
            this.textBox_proxy_pwd.TextChanged += new System.EventHandler(this.textBox_proxy_pwd_TextChanged);
            // 
            // textBox_proxy_user
            // 
            this.textBox_proxy_user.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_proxy_user.Location = new System.Drawing.Point(499, 3);
            this.textBox_proxy_user.MaxLength = 64;
            this.textBox_proxy_user.Name = "textBox_proxy_user";
            this.textBox_proxy_user.Size = new System.Drawing.Size(76, 21);
            this.textBox_proxy_user.TabIndex = 5;
            this.textBox_proxy_user.TextChanged += new System.EventHandler(this.textBox_proxy_user_TextChanged);
            // 
            // textBox_proxy_port
            // 
            this.textBox_proxy_port.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_proxy_port.Location = new System.Drawing.Point(389, 3);
            this.textBox_proxy_port.MaxLength = 5;
            this.textBox_proxy_port.Name = "textBox_proxy_port";
            this.textBox_proxy_port.Size = new System.Drawing.Size(44, 21);
            this.textBox_proxy_port.TabIndex = 6;
            this.textBox_proxy_port.TextChanged += new System.EventHandler(this.textBox_proxy_port_TextChanged);
            // 
            // textBox_proxy_host
            // 
            this.textBox_proxy_host.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_proxy_host.Location = new System.Drawing.Point(133, 3);
            this.textBox_proxy_host.MaxLength = 256;
            this.textBox_proxy_host.Name = "textBox_proxy_host";
            this.textBox_proxy_host.Size = new System.Drawing.Size(200, 21);
            this.textBox_proxy_host.TabIndex = 7;
            this.textBox_proxy_host.TextChanged += new System.EventHandler(this.textBox_proxy_host_TextChanged);
            // 
            // checkBox_careful_mode_open
            // 
            this.checkBox_careful_mode_open.AutoSize = true;
            this.checkBox_careful_mode_open.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkBox_careful_mode_open.Location = new System.Drawing.Point(3, 73);
            this.checkBox_careful_mode_open.Name = "checkBox_careful_mode_open";
            this.checkBox_careful_mode_open.Size = new System.Drawing.Size(74, 24);
            this.checkBox_careful_mode_open.TabIndex = 11;
            this.checkBox_careful_mode_open.Text = "保守模式";
            this.checkBox_careful_mode_open.UseVisualStyleBackColor = true;
            this.checkBox_careful_mode_open.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(798, 430);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "Form1";
            this.Text = "下载静态资料网站";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_web_root;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.RichTextBox richTextBox_log;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label_status;
        private System.Windows.Forms.Button button_auto_scroll_log;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.CheckBox checkBox_force_down;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox_thread_cnt;
        private System.Windows.Forms.LinkLabel linkLabel_save_dir;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_proxy_pwd;
        private System.Windows.Forms.TextBox textBox_proxy_user;
        private System.Windows.Forms.TextBox textBox_proxy_port;
        private System.Windows.Forms.TextBox textBox_proxy_host;
        private System.Windows.Forms.CheckBox checkBox_use_proxy;
        private System.Windows.Forms.CheckBox checkBox_careful_mode_open;
    }
}


namespace Hydrology.Forms
{
    partial class CBatchManagement
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CBatchManagement));
            this.label4 = new System.Windows.Forms.Label();
            this.btn_StartAdjust = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton3 = new System.Windows.Forms.RadioButton();
            this.radioButton4 = new System.Windows.Forms.RadioButton();
            this.button1 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.chk_All = new System.Windows.Forms.CheckBox();
            this.cmb_SubCenter = new System.Windows.Forms.ComboBox();
            this.btn_Exit = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.panel7 = new System.Windows.Forms.Panel();
            this.statusStrip2 = new System.Windows.Forms.StatusStrip();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.panel4 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.listView2 = new System.Windows.Forms.ListView();
            this.panel6 = new System.Windows.Forms.Panel();
            this.statusStrip3 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.系统对时 = new System.Windows.Forms.TabPage();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.panel2 = new System.Windows.Forms.Panel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tst_Label = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.groupBox2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel7.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel6.SuspendLayout();
            this.statusStrip3.SuspendLayout();
            this.系统对时.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.Blue;
            this.label4.Location = new System.Drawing.Point(13, 79);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(0, 12);
            this.label4.TabIndex = 11;
            // 
            // btn_StartAdjust
            // 
            this.btn_StartAdjust.Location = new System.Drawing.Point(313, 25);
            this.btn_StartAdjust.Name = "btn_StartAdjust";
            this.btn_StartAdjust.Size = new System.Drawing.Size(78, 28);
            this.btn_StartAdjust.TabIndex = 9;
            this.btn_StartAdjust.Text = "系统对时";
            this.btn_StartAdjust.UseVisualStyleBackColor = true;
            this.btn_StartAdjust.Click += new System.EventHandler(this.btn_StartAdjust_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButton2);
            this.groupBox2.Controls.Add(this.radioButton1);
            this.groupBox2.Controls.Add(this.radioButton3);
            this.groupBox2.Controls.Add(this.radioButton4);
            this.groupBox2.Location = new System.Drawing.Point(5, 48);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(302, 36);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Visible = false;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(83, 12);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(71, 16);
            this.radioButton2.TabIndex = 1;
            this.radioButton2.Text = "实测水位";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Checked = true;
            this.radioButton1.Location = new System.Drawing.Point(6, 12);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(71, 16);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "存储水位";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // radioButton3
            // 
            this.radioButton3.AutoSize = true;
            this.radioButton3.Location = new System.Drawing.Point(160, 12);
            this.radioButton3.Name = "radioButton3";
            this.radioButton3.Size = new System.Drawing.Size(47, 16);
            this.radioButton3.TabIndex = 2;
            this.radioButton3.Text = "雨量";
            this.radioButton3.UseVisualStyleBackColor = true;
            this.radioButton3.CheckedChanged += new System.EventHandler(this.radioButton3_CheckedChanged);
            // 
            // radioButton4
            // 
            this.radioButton4.AutoSize = true;
            this.radioButton4.Location = new System.Drawing.Point(228, 12);
            this.radioButton4.Name = "radioButton4";
            this.radioButton4.Size = new System.Drawing.Size(47, 16);
            this.radioButton4.TabIndex = 3;
            this.radioButton4.Text = "墒情";
            this.radioButton4.UseVisualStyleBackColor = true;
            this.radioButton4.CheckedChanged += new System.EventHandler(this.radioButton4_CheckedChanged);
            // 
            // button1
            // 
            this.button1.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.button1.Location = new System.Drawing.Point(412, 25);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 27);
            this.button1.TabIndex = 13;
            this.button1.Text = "读取";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.chk_All);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.cmb_SubCenter);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.btn_StartAdjust);
            this.panel1.Controls.Add(this.btn_Exit);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(12, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(646, 95);
            this.panel1.TabIndex = 2;
            // 
            // chk_All
            // 
            this.chk_All.AutoSize = true;
            this.chk_All.Location = new System.Drawing.Point(226, 23);
            this.chk_All.Name = "chk_All";
            this.chk_All.Size = new System.Drawing.Size(48, 16);
            this.chk_All.TabIndex = 12;
            this.chk_All.Text = "全选";
            this.chk_All.UseVisualStyleBackColor = true;
            // 
            // cmb_SubCenter
            // 
            this.cmb_SubCenter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_SubCenter.FormattingEnabled = true;
            this.cmb_SubCenter.Location = new System.Drawing.Point(86, 22);
            this.cmb_SubCenter.Name = "cmb_SubCenter";
            this.cmb_SubCenter.Size = new System.Drawing.Size(121, 20);
            this.cmb_SubCenter.TabIndex = 11;
            // 
            // btn_Exit
            // 
            this.btn_Exit.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btn_Exit.Location = new System.Drawing.Point(513, 26);
            this.btn_Exit.Name = "btn_Exit";
            this.btn_Exit.Size = new System.Drawing.Size(73, 27);
            this.btn_Exit.TabIndex = 10;
            this.btn_Exit.Text = "退出";
            this.btn_Exit.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 8;
            this.label2.Text = "分中心选择：";
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.statusStrip2);
            this.panel7.Location = new System.Drawing.Point(3, 102);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(630, 24);
            this.panel7.TabIndex = 11;
            // 
            // statusStrip2
            // 
            this.statusStrip2.BackgroundImage = global::Hydrology.Properties.Resources.状态栏;
            this.statusStrip2.Location = new System.Drawing.Point(0, 2);
            this.statusStrip2.Name = "statusStrip2";
            this.statusStrip2.Size = new System.Drawing.Size(630, 22);
            this.statusStrip2.TabIndex = 3;
            this.statusStrip2.Text = "statusStrip2";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.panel6);
            this.tabPage2.Controls.Add(this.panel5);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.panel4);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(636, 430);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "批量读取";
            this.tabPage2.UseVisualStyleBackColor = true;
            this.tabPage2.Hide();
            
            // 
            // panel4
            // 
            this.panel4.Location = new System.Drawing.Point(3, 3);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(627, 278);
            this.panel4.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(3, 284);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 14);
            this.label1.TabIndex = 5;
            this.label1.Text = "日志";
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.listView2);
            this.panel5.Location = new System.Drawing.Point(5, 301);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(627, 103);
            this.panel5.TabIndex = 6;
            // 
            // listView2
            // 
            this.listView2.Location = new System.Drawing.Point(1, 3);
            this.listView2.Name = "listView2";
            this.listView2.Size = new System.Drawing.Size(618, 94);
            this.listView2.TabIndex = 0;
            this.listView2.UseCompatibleStateImageBehavior = false;
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.statusStrip3);
            this.panel6.Location = new System.Drawing.Point(5, 404);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(630, 24);
            this.panel6.TabIndex = 11;
            // 
            // statusStrip3
            // 
            this.statusStrip3.BackgroundImage = global::Hydrology.Properties.Resources.状态栏;
            this.statusStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip3.Location = new System.Drawing.Point(0, 2);
            this.statusStrip3.Name = "statusStrip3";
            this.statusStrip3.Size = new System.Drawing.Size(630, 22);
            this.statusStrip3.TabIndex = 3;
            this.statusStrip3.Text = "statusStrip3";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.ForeColor = System.Drawing.Color.White;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(56, 17);
            this.toolStripStatusLabel1.Text = "准备就绪";
            // 
            // 系统对时
            // 
            this.系统对时.Controls.Add(this.panel2);
            this.系统对时.Controls.Add(this.panel3);
            this.系统对时.Controls.Add(this.label3);
            this.系统对时.Controls.Add(this.panelBottom);
            this.系统对时.Location = new System.Drawing.Point(4, 22);
            this.系统对时.Name = "系统对时";
            this.系统对时.Padding = new System.Windows.Forms.Padding(3);
            this.系统对时.Size = new System.Drawing.Size(636, 430);
            this.系统对时.TabIndex = 0;
            this.系统对时.Text = "系统对时";
            this.系统对时.UseVisualStyleBackColor = true;
            // 
            // panelBottom
            // 
            this.panelBottom.Location = new System.Drawing.Point(1, 0);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(630, 282);
            this.panelBottom.TabIndex = 0;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(6, 288);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(91, 14);
            this.label3.TabIndex = 1;
            this.label3.Text = "配置对时信息";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.listView1);
            this.panel3.Location = new System.Drawing.Point(1, 305);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(635, 100);
            this.panel3.TabIndex = 2;
            // 
            // listView1
            // 
            this.listView1.Location = new System.Drawing.Point(4, 3);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(625, 92);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.statusStrip1);
            this.panel2.Location = new System.Drawing.Point(3, 406);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(630, 24);
            this.panel2.TabIndex = 10;
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackgroundImage = global::Hydrology.Properties.Resources.状态栏;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tst_Label});
            this.statusStrip1.Location = new System.Drawing.Point(0, 2);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(630, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tst_Label
            // 
            this.tst_Label.ForeColor = System.Drawing.Color.White;
            this.tst_Label.Name = "tst_Label";
            this.tst_Label.Size = new System.Drawing.Size(56, 17);
            this.tst_Label.Text = "准备就绪";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.系统对时);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(14, 113);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(644, 456);
            this.tabControl1.TabIndex = 0;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // CBatchManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(662, 576);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "CBatchManagement";
            this.Text = "批量远程";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel7.ResumeLayout(false);
            this.panel7.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            this.statusStrip3.ResumeLayout(false);
            this.statusStrip3.PerformLayout();
            this.系统对时.ResumeLayout(false);
            this.系统对时.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btn_StartAdjust;
        private System.Windows.Forms.RadioButton radioButton4;
        private System.Windows.Forms.RadioButton radioButton3;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox chk_All;
        private System.Windows.Forms.ComboBox cmb_SubCenter;
        private System.Windows.Forms.Button btn_Exit;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.StatusStrip statusStrip2;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.StatusStrip statusStrip3;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.ListView listView2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.TabPage 系统对时;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tst_Label;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.TabControl tabControl1;
    }
}
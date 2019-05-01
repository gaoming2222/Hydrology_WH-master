namespace Hydrology.Forms
{
    partial class CBatchFlashMgrForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CBatchFlashMgrForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.listView1 = new System.Windows.Forms.ListView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dtp_EndTime = new System.Windows.Forms.DateTimePicker();
            this.dtp_StartTime = new System.Windows.Forms.DateTimePicker();
            this.cmbStation = new System.Windows.Forms.ComboBox();
            this.lbl_EndTime = new System.Windows.Forms.Label();
            this.lbl_StartTime = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButtonSave = new System.Windows.Forms.RadioButton();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnStartTrans = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.tableLayoutPanel1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(434, 562);
            this.panel1.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(434, 562);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.listView1);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(3, 238);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(428, 321);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "返回信息";
            // 
            // listView1
            // 
            this.listView1.BackColor = System.Drawing.SystemColors.Info;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.Location = new System.Drawing.Point(3, 17);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(422, 301);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.dtp_EndTime);
            this.groupBox2.Controls.Add(this.dtp_StartTime);
            this.groupBox2.Controls.Add(this.cmbStation);
            this.groupBox2.Controls.Add(this.lbl_EndTime);
            this.groupBox2.Controls.Add(this.lbl_StartTime);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(3, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(428, 194);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "配置";
            // 
            // dtp_EndTime
            // 
            this.dtp_EndTime.Location = new System.Drawing.Point(172, 131);
            this.dtp_EndTime.Name = "dtp_EndTime";
            this.dtp_EndTime.Size = new System.Drawing.Size(167, 21);
            this.dtp_EndTime.TabIndex = 8;
            // 
            // dtp_StartTime
            // 
            this.dtp_StartTime.CustomFormat = "\"\"";
            this.dtp_StartTime.Location = new System.Drawing.Point(172, 78);
            this.dtp_StartTime.Name = "dtp_StartTime";
            this.dtp_StartTime.Size = new System.Drawing.Size(167, 21);
            this.dtp_StartTime.TabIndex = 7;
            // 
            // cmbStation
            // 
            this.cmbStation.FormattingEnabled = true;
            this.cmbStation.Items.AddRange(new object[] {
            "0011223344",
            "0011223345"});
            this.cmbStation.Location = new System.Drawing.Point(172, 24);
            this.cmbStation.Name = "cmbStation";
            this.cmbStation.Size = new System.Drawing.Size(167, 20);
            this.cmbStation.TabIndex = 4;
            // 
            // lbl_EndTime
            // 
            this.lbl_EndTime.AutoSize = true;
            this.lbl_EndTime.Location = new System.Drawing.Point(73, 140);
            this.lbl_EndTime.Name = "lbl_EndTime";
            this.lbl_EndTime.Size = new System.Drawing.Size(65, 12);
            this.lbl_EndTime.TabIndex = 3;
            this.lbl_EndTime.Text = "结束时间：";
            // 
            // lbl_StartTime
            // 
            this.lbl_StartTime.AutoSize = true;
            this.lbl_StartTime.Location = new System.Drawing.Point(73, 84);
            this.lbl_StartTime.Name = "lbl_StartTime";
            this.lbl_StartTime.Size = new System.Drawing.Size(65, 12);
            this.lbl_StartTime.TabIndex = 2;
            this.lbl_StartTime.Text = "起始时间：";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(73, 24);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(41, 12);
            this.label9.TabIndex = 0;
            this.label9.Text = "测站：";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.radioButton2);
            this.panel2.Controls.Add(this.radioButtonSave);
            this.panel2.Controls.Add(this.btnClear);
            this.panel2.Controls.Add(this.btnStartTrans);
            this.panel2.Controls.Add(this.btnExit);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(3, 203);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(428, 29);
            this.panel2.TabIndex = 7;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(334, 10);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(47, 16);
            this.radioButton2.TabIndex = 12;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "存库";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.Visible = false;
            // 
            // radioButtonSave
            // 
            this.radioButtonSave.AutoSize = true;
            this.radioButtonSave.Location = new System.Drawing.Point(269, 10);
            this.radioButtonSave.Name = "radioButtonSave";
            this.radioButtonSave.Size = new System.Drawing.Size(59, 16);
            this.radioButtonSave.TabIndex = 11;
            this.radioButtonSave.TabStop = true;
            this.radioButtonSave.Text = "存文本";
            this.radioButtonSave.UseVisualStyleBackColor = true;
            this.radioButtonSave.Visible = false;
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(26, 3);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 23);
            this.btnClear.TabIndex = 10;
            this.btnClear.Text = "清空返回信息";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnStartTrans
            // 
            this.btnStartTrans.Location = new System.Drawing.Point(107, 3);
            this.btnStartTrans.Name = "btnStartTrans";
            this.btnStartTrans.Size = new System.Drawing.Size(75, 23);
            this.btnStartTrans.TabIndex = 8;
            this.btnStartTrans.Text = "开始传输";
            this.btnStartTrans.UseVisualStyleBackColor = true;
            this.btnStartTrans.Click += new System.EventHandler(this.btnStartTrans_Click);
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(188, 3);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 9;
            this.btnExit.Text = "退出";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // CBatchFlashMgrForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(434, 562);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CBatchFlashMgrForm";
            this.Text = "批量传输";
            this.panel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DateTimePicker dtp_EndTime;
        private System.Windows.Forms.DateTimePicker dtp_StartTime;
        private System.Windows.Forms.Label lbl_EndTime;
        private System.Windows.Forms.Label lbl_StartTime;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnStartTrans;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.ComboBox cmbStation;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButtonSave;

    }
}
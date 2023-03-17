using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace T3.Player
{
    partial class StartDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StartDialog));
            this.btnLaunch = new System.Windows.Forms.Button();
            this._windowedCheckbox = new System.Windows.Forms.CheckBox();
            this._vsyncCheckbox = new System.Windows.Forms.CheckBox();
            this._resolutionListbox = new System.Windows.Forms.ListBox();
            this._loopCheckBox = new System.Windows.Forms.CheckBox();
            this._resultTestLabel = new System.Windows.Forms.Label();
            this._txtboxwidth = new System.Windows.Forms.TextBox();
            this._txtboxheight = new System.Windows.Forms.TextBox();
            this._labelWidth = new System.Windows.Forms.Label();
            this._labelHeight = new System.Windows.Forms.Label();
            this._labelResolution = new System.Windows.Forms.Label();
            this._pictureTest = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this._pictureTest)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLaunch
            // 
            this.btnLaunch.Location = new System.Drawing.Point(154, 287);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(98, 23);
            this.btnLaunch.TabIndex = 0;
            this.btnLaunch.Text = "Launch T3";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.Button1_Click);
            // 
            // _windowedCheckbox
            // 
            this._windowedCheckbox.AutoSize = true;
            this._windowedCheckbox.Location = new System.Drawing.Point(154, 214);
            this._windowedCheckbox.Name = "chkWindowed";
            this._windowedCheckbox.Size = new System.Drawing.Size(77, 17);
            this._windowedCheckbox.TabIndex = 1;
            this._windowedCheckbox.Text = "Windowed";
            this._windowedCheckbox.UseVisualStyleBackColor = true;
            this._windowedCheckbox.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // _vsyncCheckbox
            // 
            this._vsyncCheckbox.AutoSize = true;
            this._vsyncCheckbox.Location = new System.Drawing.Point(154, 237);
            this._vsyncCheckbox.Name = "chkVsync";
            this._vsyncCheckbox.Size = new System.Drawing.Size(98, 17);
            this._vsyncCheckbox.TabIndex = 2;
            this._vsyncCheckbox.Text = "Disable V-Sync";
            this._vsyncCheckbox.UseVisualStyleBackColor = true;
            this._vsyncCheckbox.CheckedChanged += new System.EventHandler(this.CheckBox2_CheckedChanged);
            // 
            // _resolutionListbox
            // 
            this._resolutionListbox.FormattingEnabled = true;
            this._resolutionListbox.Items.AddRange(new object[] {
            "640 x 360 (16:9)",
            "1280 x 720 (16:9)",
            "1360 x 768",
            "1366 x 768",
            "1440 x 900",
            "1600 x 900",
            "1680 x 1050",
            "1920 x 1080 (16:9)",
            "1920 x 1200",
            "2160 x 1440",
            "2560 x 1080",
            "2560 x 1600",
            "2560 x 1440 (16:9)",
            "3440 x 1440",
            "3840 x 2160 (16:9)",
            "Custom Resolution"});
            this._resolutionListbox.Location = new System.Drawing.Point(12, 390);
            this._resolutionListbox.Name = "listBoxResolution";
            this._resolutionListbox.Size = new System.Drawing.Size(132, 225);
            this._resolutionListbox.TabIndex = 3;
            this._resolutionListbox.SelectedIndexChanged += new System.EventHandler(this.ListBox1_SelectedIndexChanged);
            // 
            // _loopCheckBox
            // 
            this._loopCheckBox.AutoSize = true;
            this._loopCheckBox.Location = new System.Drawing.Point(154, 260);
            this._loopCheckBox.Name = "chkLoop";
            this._loopCheckBox.Size = new System.Drawing.Size(50, 17);
            this._loopCheckBox.TabIndex = 4;
            this._loopCheckBox.Text = "Loop";
            this._loopCheckBox.UseVisualStyleBackColor = true;
            this._loopCheckBox.CheckedChanged += new System.EventHandler(this.CheckBox3_CheckedChanged);
            // 
            // _resultTestLabel
            // 
            this._resultTestLabel.AutoSize = true;
            this._resultTestLabel.Location = new System.Drawing.Point(12, 347);
            this._resultTestLabel.Name = "lblResult";
            this._resultTestLabel.Size = new System.Drawing.Size(35, 13);
            this._resultTestLabel.TabIndex = 6;
            this._resultTestLabel.Text = "label1";
            // 
            // _txtboxwidth
            // 
            this._txtboxwidth.Location = new System.Drawing.Point(16, 248);
            this._txtboxwidth.Name = "txtboxwidth";
            this._txtboxwidth.Size = new System.Drawing.Size(100, 20);
            this._txtboxwidth.TabIndex = 7;
            this._txtboxwidth.Text = "1920";
            this._txtboxwidth.TextChanged += new System.EventHandler(this.TextBox1_TextChanged);
            // 
            // _txtboxheight
            // 
            this._txtboxheight.Location = new System.Drawing.Point(16, 287);
            this._txtboxheight.Name = "txtboxheight";
            this._txtboxheight.Size = new System.Drawing.Size(100, 20);
            this._txtboxheight.TabIndex = 8;
            this._txtboxheight.Text = "1080";
            // 
            // _labelWidth
            // 
            this._labelWidth.AutoSize = true;
            this._labelWidth.Location = new System.Drawing.Point(13, 232);
            this._labelWidth.Name = "labelWidth";
            this._labelWidth.Size = new System.Drawing.Size(35, 13);
            this._labelWidth.TabIndex = 9;
            this._labelWidth.Text = "Width";
            // 
            // _labelHeight
            // 
            this._labelHeight.AutoSize = true;
            this._labelHeight.Location = new System.Drawing.Point(13, 271);
            this._labelHeight.Name = "labelHeight";
            this._labelHeight.Size = new System.Drawing.Size(38, 13);
            this._labelHeight.TabIndex = 10;
            this._labelHeight.Text = "Height";
            this._labelHeight.Click += new System.EventHandler(this.Label2_Click);
            // 
            // _labelResolution
            // 
            this._labelResolution.AutoSize = true;
            this._labelResolution.Location = new System.Drawing.Point(13, 213);
            this._labelResolution.Name = "labelResolution";
            this._labelResolution.Size = new System.Drawing.Size(60, 13);
            this._labelResolution.TabIndex = 11;
            this._labelResolution.Text = "Resolution:";
            // 
            // _pictureTest
            // 
            // this._pictureTest.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            // this._pictureTest.Image = ((System.Drawing.Image)(resources.GetObject("pictureTest.Image")));
            // this._pictureTest.InitialImage = global::T3_Launcher.Properties.Resources._344;
            // this._pictureTest.Location = new System.Drawing.Point(12, 12);
            // this._pictureTest.Name = "pictureTest";
            // this._pictureTest.Size = new System.Drawing.Size(341, 190);
            // this._pictureTest.TabIndex = 12;
            // this._pictureTest.TabStop = false;
            // this._pictureTest.Click += new System.EventHandler(this.pictureBox1_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(364, 816);
            this.Controls.Add(this._pictureTest);
            this.Controls.Add(this._labelResolution);
            this.Controls.Add(this._labelHeight);
            this.Controls.Add(this._labelWidth);
            this.Controls.Add(this._txtboxheight);
            this.Controls.Add(this._txtboxwidth);
            this.Controls.Add(this._resultTestLabel);
            this.Controls.Add(this._loopCheckBox);
            this.Controls.Add(this._resolutionListbox);
            this.Controls.Add(this._vsyncCheckbox);
            this.Controls.Add(this._windowedCheckbox);
            this.Controls.Add(this.btnLaunch);
            //this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "StartDialog";
            this.Text = "Tooll3 Launcher";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this._pictureTest)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button btnLaunch;
        private CheckBox _windowedCheckbox;
        private CheckBox _vsyncCheckbox;
        private ListBox _resolutionListbox;
        private CheckBox _loopCheckBox;
        private Label _resultTestLabel;
        private TextBox _txtboxwidth;
        private TextBox _txtboxheight;
        private Label _labelWidth;
        private Label _labelHeight;
        private Label _labelResolution;
        private PictureBox _pictureTest;
    }
}


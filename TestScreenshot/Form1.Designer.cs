namespace TestScreenshot
{
    partial class Form1
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
            this.btnInject = new System.Windows.Forms.Button();
            this.btnCapture = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.btnLoadTest = new System.Windows.Forms.Button();
            this.txtNumber = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtCaptureX = new System.Windows.Forms.TextBox();
            this.txtCaptureY = new System.Windows.Forms.TextBox();
            this.txtCaptureWidth = new System.Windows.Forms.TextBox();
            this.txtCaptureHeight = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtDebugLog = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.cbAutoGAC = new System.Windows.Forms.CheckBox();
            this.rbDirect3D9 = new System.Windows.Forms.RadioButton();
            this.rbDirect3D10 = new System.Windows.Forms.RadioButton();
            this.rbDirect3D11 = new System.Windows.Forms.RadioButton();
            this.cbDrawOverlay = new System.Windows.Forms.CheckBox();
            this.rbAutodetect = new System.Windows.Forms.RadioButton();
            this.rbDirect3D10_1 = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnInject
            // 
            this.btnInject.Location = new System.Drawing.Point(112, 30);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(74, 23);
            this.btnInject.TabIndex = 0;
            this.btnInject.Text = "Inject";
            this.btnInject.UseVisualStyleBackColor = true;
            this.btnInject.Click += new System.EventHandler(this.btnInject_Click);
            // 
            // btnCapture
            // 
            this.btnCapture.Enabled = false;
            this.btnCapture.Location = new System.Drawing.Point(112, 59);
            this.btnCapture.Name = "btnCapture";
            this.btnCapture.Size = new System.Drawing.Size(75, 41);
            this.btnCapture.TabIndex = 1;
            this.btnCapture.Text = "Request Capture";
            this.btnCapture.UseVisualStyleBackColor = true;
            this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point(193, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(797, 405);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // btnLoadTest
            // 
            this.btnLoadTest.Enabled = false;
            this.btnLoadTest.Location = new System.Drawing.Point(112, 106);
            this.btnLoadTest.Name = "btnLoadTest";
            this.btnLoadTest.Size = new System.Drawing.Size(75, 23);
            this.btnLoadTest.TabIndex = 3;
            this.btnLoadTest.Text = "Load Test";
            this.btnLoadTest.UseVisualStyleBackColor = true;
            this.btnLoadTest.Click += new System.EventHandler(this.btnLoadTest_Click);
            // 
            // txtNumber
            // 
            this.txtNumber.Location = new System.Drawing.Point(6, 108);
            this.txtNumber.Name = "txtNumber";
            this.txtNumber.Size = new System.Drawing.Size(100, 20);
            this.txtNumber.TabIndex = 4;
            this.txtNumber.Text = "100";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(6, 135);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(180, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(8, 32);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(98, 20);
            this.textBox1.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 167);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(14, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "X";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 193);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(14, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Y";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 221);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Width";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 247);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Height";
            // 
            // txtCaptureX
            // 
            this.txtCaptureX.Location = new System.Drawing.Point(66, 164);
            this.txtCaptureX.Name = "txtCaptureX";
            this.txtCaptureX.Size = new System.Drawing.Size(100, 20);
            this.txtCaptureX.TabIndex = 11;
            this.txtCaptureX.Text = "0";
            // 
            // txtCaptureY
            // 
            this.txtCaptureY.Location = new System.Drawing.Point(66, 190);
            this.txtCaptureY.Name = "txtCaptureY";
            this.txtCaptureY.Size = new System.Drawing.Size(100, 20);
            this.txtCaptureY.TabIndex = 12;
            this.txtCaptureY.Text = "0";
            // 
            // txtCaptureWidth
            // 
            this.txtCaptureWidth.Location = new System.Drawing.Point(66, 218);
            this.txtCaptureWidth.Name = "txtCaptureWidth";
            this.txtCaptureWidth.Size = new System.Drawing.Size(100, 20);
            this.txtCaptureWidth.TabIndex = 13;
            this.txtCaptureWidth.Text = "0";
            // 
            // txtCaptureHeight
            // 
            this.txtCaptureHeight.Location = new System.Drawing.Point(66, 244);
            this.txtCaptureHeight.Name = "txtCaptureHeight";
            this.txtCaptureHeight.Size = new System.Drawing.Size(100, 20);
            this.txtCaptureHeight.TabIndex = 14;
            this.txtCaptureHeight.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(63, 267);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(93, 39);
            this.label5.TabIndex = 15;
            this.label5.Text = "Width of 0 means \r\ncapture whole \r\nwindow";
            // 
            // txtDebugLog
            // 
            this.txtDebugLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugLog.Location = new System.Drawing.Point(5, 424);
            this.txtDebugLog.Multiline = true;
            this.txtDebugLog.Name = "txtDebugLog";
            this.txtDebugLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtDebugLog.Size = new System.Drawing.Size(985, 90);
            this.txtDebugLog.TabIndex = 16;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(5, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(174, 13);
            this.label6.TabIndex = 17;
            this.label6.Text = "EXE Name of Direct3D Application:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(7, 92);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(86, 13);
            this.label7.TabIndex = 18;
            this.label7.Text = "Capture Multiple:";
            // 
            // cbAutoGAC
            // 
            this.cbAutoGAC.AutoSize = true;
            this.cbAutoGAC.Location = new System.Drawing.Point(5, 378);
            this.cbAutoGAC.Name = "cbAutoGAC";
            this.cbAutoGAC.Size = new System.Drawing.Size(179, 17);
            this.cbAutoGAC.TabIndex = 25;
            this.cbAutoGAC.Text = "Auto register GAC (run as admin)";
            this.cbAutoGAC.UseVisualStyleBackColor = true;
            // 
            // rbDirect3D9
            // 
            this.rbDirect3D9.AutoSize = true;
            this.rbDirect3D9.Location = new System.Drawing.Point(93, 309);
            this.rbDirect3D9.Name = "rbDirect3D9";
            this.rbDirect3D9.Size = new System.Drawing.Size(76, 17);
            this.rbDirect3D9.TabIndex = 21;
            this.rbDirect3D9.Text = "Direct3D 9";
            this.rbDirect3D9.UseVisualStyleBackColor = true;
            // 
            // rbDirect3D10
            // 
            this.rbDirect3D10.AutoSize = true;
            this.rbDirect3D10.Location = new System.Drawing.Point(10, 332);
            this.rbDirect3D10.Name = "rbDirect3D10";
            this.rbDirect3D10.Size = new System.Drawing.Size(82, 17);
            this.rbDirect3D10.TabIndex = 22;
            this.rbDirect3D10.Text = "Direct3D 10";
            this.rbDirect3D10.UseVisualStyleBackColor = true;
            // 
            // rbDirect3D11
            // 
            this.rbDirect3D11.AutoSize = true;
            this.rbDirect3D11.Location = new System.Drawing.Point(10, 355);
            this.rbDirect3D11.Name = "rbDirect3D11";
            this.rbDirect3D11.Size = new System.Drawing.Size(82, 17);
            this.rbDirect3D11.TabIndex = 24;
            this.rbDirect3D11.Text = "Direct3D 11";
            this.rbDirect3D11.UseVisualStyleBackColor = true;
            // 
            // cbDrawOverlay
            // 
            this.cbDrawOverlay.AutoSize = true;
            this.cbDrawOverlay.Checked = true;
            this.cbDrawOverlay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbDrawOverlay.Location = new System.Drawing.Point(5, 401);
            this.cbDrawOverlay.Name = "cbDrawOverlay";
            this.cbDrawOverlay.Size = new System.Drawing.Size(90, 17);
            this.cbDrawOverlay.TabIndex = 26;
            this.cbDrawOverlay.Text = "Draw Overlay";
            this.cbDrawOverlay.UseVisualStyleBackColor = true;
            // 
            // rbAutodetect
            // 
            this.rbAutodetect.AutoSize = true;
            this.rbAutodetect.Checked = true;
            this.rbAutodetect.Location = new System.Drawing.Point(10, 309);
            this.rbAutodetect.Name = "rbAutodetect";
            this.rbAutodetect.Size = new System.Drawing.Size(77, 17);
            this.rbAutodetect.TabIndex = 20;
            this.rbAutodetect.TabStop = true;
            this.rbAutodetect.Text = "Autodetect";
            this.rbAutodetect.UseVisualStyleBackColor = true;
            // 
            // rbDirect3D10_1
            // 
            this.rbDirect3D10_1.AutoSize = true;
            this.rbDirect3D10_1.Location = new System.Drawing.Point(93, 332);
            this.rbDirect3D10_1.Name = "rbDirect3D10_1";
            this.rbDirect3D10_1.Size = new System.Drawing.Size(91, 17);
            this.rbDirect3D10_1.TabIndex = 23;
            this.rbDirect3D10_1.Text = "Direct3D 10.1";
            this.rbDirect3D10_1.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1002, 526);
            this.Controls.Add(this.rbDirect3D10_1);
            this.Controls.Add(this.rbAutodetect);
            this.Controls.Add(this.cbDrawOverlay);
            this.Controls.Add(this.rbDirect3D11);
            this.Controls.Add(this.rbDirect3D10);
            this.Controls.Add(this.rbDirect3D9);
            this.Controls.Add(this.cbAutoGAC);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtDebugLog);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtCaptureHeight);
            this.Controls.Add(this.txtCaptureWidth);
            this.Controls.Add(this.txtCaptureY);
            this.Controls.Add(this.txtCaptureX);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.txtNumber);
            this.Controls.Add(this.btnLoadTest);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnCapture);
            this.Controls.Add(this.btnInject);
            this.Name = "Form1";
            this.Text = "Test Screenshot Direct3D API Hook";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnInject;
        private System.Windows.Forms.Button btnCapture;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnLoadTest;
        private System.Windows.Forms.TextBox txtNumber;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtCaptureX;
        private System.Windows.Forms.TextBox txtCaptureY;
        private System.Windows.Forms.TextBox txtCaptureWidth;
        private System.Windows.Forms.TextBox txtCaptureHeight;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtDebugLog;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox cbAutoGAC;
        private System.Windows.Forms.RadioButton rbDirect3D9;
        private System.Windows.Forms.RadioButton rbDirect3D10;
        private System.Windows.Forms.RadioButton rbDirect3D11;
        private System.Windows.Forms.CheckBox cbDrawOverlay;
        private System.Windows.Forms.RadioButton rbAutodetect;
        private System.Windows.Forms.RadioButton rbDirect3D10_1;
    }
}


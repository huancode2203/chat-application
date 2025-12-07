namespace ChatClient.Forms
{
    partial class VerifyOtpForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.picIcon = new System.Windows.Forms.PictureBox();
            this.lblUserInfo = new System.Windows.Forms.Label();
            this.lblInstruction = new System.Windows.Forms.Label();
            this.panelOtpInputs = new System.Windows.Forms.Panel();
            this.txtOtp1 = new System.Windows.Forms.TextBox();
            this.txtOtp2 = new System.Windows.Forms.TextBox();
            this.txtOtp3 = new System.Windows.Forms.TextBox();
            this.lblDash1 = new System.Windows.Forms.Label();
            this.txtOtp4 = new System.Windows.Forms.TextBox();
            this.txtOtp5 = new System.Windows.Forms.TextBox();
            this.txtOtp6 = new System.Windows.Forms.TextBox();
            this.lblTimer = new System.Windows.Forms.Label();
            this.progressTimer = new System.Windows.Forms.ProgressBar();
            this.btnVerify = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelDivider = new System.Windows.Forms.Panel();
            this.lblResendText = new System.Windows.Forms.Label();
            this.btnResend = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblHelpText = new System.Windows.Forms.Label();
            this.panelHeader.SuspendLayout();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.panelOtpInputs.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(155, 89, 182);
            this.panelHeader.Controls.Add(this.lblSubtitle);
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(550, 100);
            this.panelHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(0, 20, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(550, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "🔐 Xác Thực OTP";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(230, 220, 255);
            this.lblSubtitle.Location = new System.Drawing.Point(0, 60);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(550, 40);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Nhập mã xác thực 6 chữ số đã được gửi đến email của bạn";
            this.lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelMain
            // 
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.lblHelpText);
            this.panelMain.Controls.Add(this.btnCancel);
            this.panelMain.Controls.Add(this.btnResend);
            this.panelMain.Controls.Add(this.lblResendText);
            this.panelMain.Controls.Add(this.panelDivider);
            this.panelMain.Controls.Add(this.lblStatus);
            this.panelMain.Controls.Add(this.progressBar);
            this.panelMain.Controls.Add(this.btnVerify);
            this.panelMain.Controls.Add(this.progressTimer);
            this.panelMain.Controls.Add(this.lblTimer);
            this.panelMain.Controls.Add(this.panelOtpInputs);
            this.panelMain.Controls.Add(this.lblInstruction);
            this.panelMain.Controls.Add(this.lblUserInfo);
            this.panelMain.Controls.Add(this.picIcon);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 100);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(40, 30, 40, 30);
            this.panelMain.Size = new System.Drawing.Size(550, 500);
            this.panelMain.TabIndex = 1;
            // 
            // picIcon
            // 
            this.picIcon.Location = new System.Drawing.Point(225, 30);
            this.picIcon.Name = "picIcon";
            this.picIcon.Size = new System.Drawing.Size(100, 100);
            this.picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIcon.TabIndex = 0;
            this.picIcon.TabStop = false;
            // 
            // lblUserInfo
            // 
            this.lblUserInfo.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblUserInfo.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblUserInfo.Location = new System.Drawing.Point(40, 140);
            this.lblUserInfo.Name = "lblUserInfo";
            this.lblUserInfo.Size = new System.Drawing.Size(470, 25);
            this.lblUserInfo.TabIndex = 1;
            this.lblUserInfo.Text = "Tài khoản: username";
            this.lblUserInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblInstruction
            // 
            this.lblInstruction.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInstruction.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblInstruction.Location = new System.Drawing.Point(40, 170);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(470, 20);
            this.lblInstruction.TabIndex = 2;
            this.lblInstruction.Text = "Nhập mã OTP 6 chữ số";
            this.lblInstruction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelOtpInputs
            // 
            this.panelOtpInputs.Controls.Add(this.lblDash1);
            this.panelOtpInputs.Controls.Add(this.txtOtp6);
            this.panelOtpInputs.Controls.Add(this.txtOtp5);
            this.panelOtpInputs.Controls.Add(this.txtOtp4);
            this.panelOtpInputs.Controls.Add(this.txtOtp3);
            this.panelOtpInputs.Controls.Add(this.txtOtp2);
            this.panelOtpInputs.Controls.Add(this.txtOtp1);
            this.panelOtpInputs.Location = new System.Drawing.Point(70, 200);
            this.panelOtpInputs.Name = "panelOtpInputs";
            this.panelOtpInputs.Size = new System.Drawing.Size(410, 60);
            this.panelOtpInputs.TabIndex = 3;
            // 
            // txtOtp1
            // 
            this.txtOtp1.Font = new System.Drawing.Font("Consolas", 20F, System.Drawing.FontStyle.Bold);
            this.txtOtp1.Location = new System.Drawing.Point(0, 10);
            this.txtOtp1.MaxLength = 1;
            this.txtOtp1.Name = "txtOtp1";
            this.txtOtp1.Size = new System.Drawing.Size(50, 39);
            this.txtOtp1.TabIndex = 0;
            this.txtOtp1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtOtp2
            // 
            this.txtOtp2.Font = new System.Drawing.Font("Consolas", 20F, System.Drawing.FontStyle.Bold);
            this.txtOtp2.Location = new System.Drawing.Point(60, 10);
            this.txtOtp2.MaxLength = 1;
            this.txtOtp2.Name = "txtOtp2";
            this.txtOtp2.Size = new System.Drawing.Size(50, 39);
            this.txtOtp2.TabIndex = 1;
            this.txtOtp2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtOtp3
            // 
            this.txtOtp3.Font = new System.Drawing.Font("Consolas", 20F, System.Drawing.FontStyle.Bold);
            this.txtOtp3.Location = new System.Drawing.Point(120, 10);
            this.txtOtp3.MaxLength = 1;
            this.txtOtp3.Name = "txtOtp3";
            this.txtOtp3.Size = new System.Drawing.Size(50, 39);
            this.txtOtp3.TabIndex = 2;
            this.txtOtp3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblDash1
            // 
            this.lblDash1.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblDash1.ForeColor = System.Drawing.Color.FromArgb(189, 195, 199);
            this.lblDash1.Location = new System.Drawing.Point(180, 10);
            this.lblDash1.Name = "lblDash1";
            this.lblDash1.Size = new System.Drawing.Size(50, 39);
            this.lblDash1.TabIndex = 3;
            this.lblDash1.Text = "-";
            this.lblDash1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtOtp4
            // 
            this.txtOtp4.Font = new System.Drawing.Font("Consolas", 20F, System.Drawing.FontStyle.Bold);
            this.txtOtp4.Location = new System.Drawing.Point(240, 10);
            this.txtOtp4.MaxLength = 1;
            this.txtOtp4.Name = "txtOtp4";
            this.txtOtp4.Size = new System.Drawing.Size(50, 39);
            this.txtOtp4.TabIndex = 4;
            this.txtOtp4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtOtp5
            // 
            this.txtOtp5.Font = new System.Drawing.Font("Consolas", 20F, System.Drawing.FontStyle.Bold);
            this.txtOtp5.Location = new System.Drawing.Point(300, 10);
            this.txtOtp5.MaxLength = 1;
            this.txtOtp5.Name = "txtOtp5";
            this.txtOtp5.Size = new System.Drawing.Size(50, 39);
            this.txtOtp5.TabIndex = 5;
            this.txtOtp5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtOtp6
            // 
            this.txtOtp6.Font = new System.Drawing.Font("Consolas", 20F, System.Drawing.FontStyle.Bold);
            this.txtOtp6.Location = new System.Drawing.Point(360, 10);
            this.txtOtp6.MaxLength = 1;
            this.txtOtp6.Name = "txtOtp6";
            this.txtOtp6.Size = new System.Drawing.Size(50, 39);
            this.txtOtp6.TabIndex = 6;
            this.txtOtp6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblTimer
            // 
            this.lblTimer.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblTimer.ForeColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.lblTimer.Location = new System.Drawing.Point(40, 270);
            this.lblTimer.Name = "lblTimer";
            this.lblTimer.Size = new System.Drawing.Size(470, 25);
            this.lblTimer.TabIndex = 4;
            this.lblTimer.Text = "⏱️ Còn lại: 10:00";
            this.lblTimer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressTimer
            // 
            this.progressTimer.Location = new System.Drawing.Point(40, 300);
            this.progressTimer.Name = "progressTimer";
            this.progressTimer.Size = new System.Drawing.Size(470, 8);
            this.progressTimer.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressTimer.TabIndex = 5;
            this.progressTimer.Value = 100;
            // 
            // btnVerify
            // 
            this.btnVerify.BackColor = System.Drawing.Color.FromArgb(155, 89, 182);
            this.btnVerify.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVerify.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnVerify.ForeColor = System.Drawing.Color.White;
            this.btnVerify.Location = new System.Drawing.Point(40, 325);
            this.btnVerify.Name = "btnVerify";
            this.btnVerify.Size = new System.Drawing.Size(470, 45);
            this.btnVerify.TabIndex = 6;
            this.btnVerify.Text = "✅ Xác thực";
            this.btnVerify.UseVisualStyleBackColor = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(40, 375);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(470, 3);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 7;
            this.progressBar.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.Location = new System.Drawing.Point(40, 383);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(470, 20);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelDivider
            // 
            this.panelDivider.BackColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this.panelDivider.Location = new System.Drawing.Point(40, 413);
            this.panelDivider.Name = "panelDivider";
            this.panelDivider.Size = new System.Drawing.Size(470, 1);
            this.panelDivider.TabIndex = 9;
            // 
            // lblResendText
            // 
            this.lblResendText.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblResendText.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblResendText.Location = new System.Drawing.Point(40, 420);
            this.lblResendText.Name = "lblResendText";
            this.lblResendText.Size = new System.Drawing.Size(200, 20);
            this.lblResendText.TabIndex = 10;
            this.lblResendText.Text = "Không nhận được mã?";
            this.lblResendText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnResend
            // 
            this.btnResend.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnResend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResend.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnResend.ForeColor = System.Drawing.Color.White;
            this.btnResend.Location = new System.Drawing.Point(250, 417);
            this.btnResend.Name = "btnResend";
            this.btnResend.Size = new System.Drawing.Size(130, 30);
            this.btnResend.TabIndex = 11;
            this.btnResend.Text = "📧 Gửi lại mã";
            this.btnResend.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(149, 165, 166);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(390, 417);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 30);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "❌ Hủy";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // lblHelpText
            // 
            this.lblHelpText.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblHelpText.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblHelpText.Location = new System.Drawing.Point(40, 455);
            this.lblHelpText.Name = "lblHelpText";
            this.lblHelpText.Size = new System.Drawing.Size(470, 30);
            this.lblHelpText.TabIndex = 13;
            this.lblHelpText.Text = "💡 Mẹo: Bạn có thể dán (Ctrl+V) mã OTP 6 chữ số từ email";
            this.lblHelpText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // VerifyOtpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(550, 600);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VerifyOtpForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Xác Thực OTP - Chat Nội Bộ";
            this.panelHeader.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.panelOtpInputs.ResumeLayout(false);
            this.panelOtpInputs.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.Label lblUserInfo;
        private System.Windows.Forms.Label lblInstruction;
        private System.Windows.Forms.Panel panelOtpInputs;
        private System.Windows.Forms.TextBox txtOtp1;
        private System.Windows.Forms.TextBox txtOtp2;
        private System.Windows.Forms.TextBox txtOtp3;
        private System.Windows.Forms.Label lblDash1;
        private System.Windows.Forms.TextBox txtOtp4;
        private System.Windows.Forms.TextBox txtOtp5;
        private System.Windows.Forms.TextBox txtOtp6;
        private System.Windows.Forms.Label lblTimer;
        private System.Windows.Forms.ProgressBar progressTimer;
        private System.Windows.Forms.Button btnVerify;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panelDivider;
        private System.Windows.Forms.Label lblResendText;
        private System.Windows.Forms.Button btnResend;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblHelpText;
    }
}
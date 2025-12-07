namespace ChatClient.Forms
{
    partial class ResetPasswordForm
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
            this.lblUser = new System.Windows.Forms.Label();
            this.lblDescription = new System.Windows.Forms.Label();
            this.lblOtp = new System.Windows.Forms.Label();
            this.txtOtp = new System.Windows.Forms.TextBox();
            this.lblOtpHint = new System.Windows.Forms.Label();
            this.lblNewPassword = new System.Windows.Forms.Label();
            this.panelNewPassword = new System.Windows.Forms.Panel();
            this.txtNewPassword = new System.Windows.Forms.TextBox();
            this.btnTogglePassword = new System.Windows.Forms.Button();
            this.lblConfirmPassword = new System.Windows.Forms.Label();
            this.panelConfirmPassword = new System.Windows.Forms.Panel();
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.btnToggleConfirm = new System.Windows.Forms.Button();
            this.lblPasswordMatch = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panelDivider = new System.Windows.Forms.Panel();
            this.lblPasswordRules = new System.Windows.Forms.Label();
            this.panelHeader.SuspendLayout();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.panelNewPassword.SuspendLayout();
            this.panelConfirmPassword.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
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
            this.lblTitle.Text = "🔐 Đặt Lại Mật Khẩu";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(230, 240, 255);
            this.lblSubtitle.Location = new System.Drawing.Point(0, 60);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(550, 40);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Nhập mã OTP và mật khẩu mới";
            this.lblSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelMain
            // 
            this.panelMain.AutoScroll = true;
            this.panelMain.BackColor = System.Drawing.Color.White;
            this.panelMain.Controls.Add(this.lblPasswordRules);
            this.panelMain.Controls.Add(this.panelDivider);
            this.panelMain.Controls.Add(this.lblStatus);
            this.panelMain.Controls.Add(this.progressBar);
            this.panelMain.Controls.Add(this.btnCancel);
            this.panelMain.Controls.Add(this.btnReset);
            this.panelMain.Controls.Add(this.lblPasswordMatch);
            this.panelMain.Controls.Add(this.panelConfirmPassword);
            this.panelMain.Controls.Add(this.lblConfirmPassword);
            this.panelMain.Controls.Add(this.panelNewPassword);
            this.panelMain.Controls.Add(this.lblNewPassword);
            this.panelMain.Controls.Add(this.lblOtpHint);
            this.panelMain.Controls.Add(this.txtOtp);
            this.panelMain.Controls.Add(this.lblOtp);
            this.panelMain.Controls.Add(this.lblDescription);
            this.panelMain.Controls.Add(this.lblUser);
            this.panelMain.Controls.Add(this.picIcon);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 100);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(40, 20, 40, 20);
            this.panelMain.Size = new System.Drawing.Size(550, 600);
            this.panelMain.TabIndex = 1;
            // 
            // picIcon
            // 
            this.picIcon.Location = new System.Drawing.Point(225, 20);
            this.picIcon.Name = "picIcon";
            this.picIcon.Size = new System.Drawing.Size(100, 100);
            this.picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIcon.TabIndex = 0;
            this.picIcon.TabStop = false;
            // 
            // lblUser
            // 
            this.lblUser.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblUser.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblUser.Location = new System.Drawing.Point(40, 130);
            this.lblUser.Name = "lblUser";
            this.lblUser.Size = new System.Drawing.Size(470, 25);
            this.lblUser.TabIndex = 1;
            this.lblUser.Text = "👤 Tài khoản: username";
            this.lblUser.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblDescription
            // 
            this.lblDescription.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDescription.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblDescription.Location = new System.Drawing.Point(40, 160);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(470, 20);
            this.lblDescription.TabIndex = 2;
            this.lblDescription.Text = "Vui lòng nhập mã OTP từ email và đặt mật khẩu mới";
            this.lblDescription.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblOtp
            // 
            this.lblOtp.AutoSize = true;
            this.lblOtp.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblOtp.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblOtp.Location = new System.Drawing.Point(40, 195);
            this.lblOtp.Name = "lblOtp";
            this.lblOtp.Size = new System.Drawing.Size(161, 19);
            this.lblOtp.TabIndex = 3;
            this.lblOtp.Text = "📧 Mã OTP (6 chữ số) *";
            // 
            // txtOtp
            // 
            this.txtOtp.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOtp.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Bold);
            this.txtOtp.Location = new System.Drawing.Point(40, 220);
            this.txtOtp.MaxLength = 6;
            this.txtOtp.Name = "txtOtp";
            this.txtOtp.Size = new System.Drawing.Size(470, 29);
            this.txtOtp.TabIndex = 4;
            this.txtOtp.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblOtpHint
            // 
            this.lblOtpHint.AutoSize = true;
            this.lblOtpHint.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblOtpHint.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblOtpHint.Location = new System.Drawing.Point(40, 252);
            this.lblOtpHint.Name = "lblOtpHint";
            this.lblOtpHint.Size = new System.Drawing.Size(183, 13);
            this.lblOtpHint.TabIndex = 5;
            this.lblOtpHint.Text = "💡 Kiểm tra email để lấy mã OTP";
            // 
            // lblNewPassword
            // 
            this.lblNewPassword.AutoSize = true;
            this.lblNewPassword.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblNewPassword.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblNewPassword.Location = new System.Drawing.Point(40, 280);
            this.lblNewPassword.Name = "lblNewPassword";
            this.lblNewPassword.Size = new System.Drawing.Size(124, 19);
            this.lblNewPassword.TabIndex = 6;
            this.lblNewPassword.Text = "🔒 Mật khẩu mới *";
            // 
            // panelNewPassword
            // 
            this.panelNewPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelNewPassword.Controls.Add(this.btnTogglePassword);
            this.panelNewPassword.Controls.Add(this.txtNewPassword);
            this.panelNewPassword.Location = new System.Drawing.Point(40, 305);
            this.panelNewPassword.Name = "panelNewPassword";
            this.panelNewPassword.Size = new System.Drawing.Size(470, 32);
            this.panelNewPassword.TabIndex = 7;
            // 
            // txtNewPassword
            // 
            this.txtNewPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtNewPassword.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtNewPassword.Location = new System.Drawing.Point(5, 6);
            this.txtNewPassword.Name = "txtNewPassword";
            this.txtNewPassword.PasswordChar = '●';
            this.txtNewPassword.Size = new System.Drawing.Size(420, 20);
            this.txtNewPassword.TabIndex = 0;
            // 
            // btnTogglePassword
            // 
            this.btnTogglePassword.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTogglePassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnTogglePassword.Location = new System.Drawing.Point(430, 0);
            this.btnTogglePassword.Name = "btnTogglePassword";
            this.btnTogglePassword.Size = new System.Drawing.Size(38, 30);
            this.btnTogglePassword.TabIndex = 1;
            this.btnTogglePassword.Text = "👁️";
            this.btnTogglePassword.UseVisualStyleBackColor = true;
            // 
            // lblConfirmPassword
            // 
            this.lblConfirmPassword.AutoSize = true;
            this.lblConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblConfirmPassword.ForeColor = System.Drawing.Color.FromArgb(52, 73, 94);
            this.lblConfirmPassword.Location = new System.Drawing.Point(40, 350);
            this.lblConfirmPassword.Name = "lblConfirmPassword";
            this.lblConfirmPassword.Size = new System.Drawing.Size(182, 19);
            this.lblConfirmPassword.TabIndex = 8;
            this.lblConfirmPassword.Text = "🔐 Xác nhận mật khẩu mới *";
            // 
            // panelConfirmPassword
            // 
            this.panelConfirmPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelConfirmPassword.Controls.Add(this.btnToggleConfirm);
            this.panelConfirmPassword.Controls.Add(this.txtConfirmPassword);
            this.panelConfirmPassword.Location = new System.Drawing.Point(40, 375);
            this.panelConfirmPassword.Name = "panelConfirmPassword";
            this.panelConfirmPassword.Size = new System.Drawing.Size(470, 32);
            this.panelConfirmPassword.TabIndex = 9;
            // 
            // txtConfirmPassword
            // 
            this.txtConfirmPassword.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtConfirmPassword.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.txtConfirmPassword.Location = new System.Drawing.Point(5, 6);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PasswordChar = '●';
            this.txtConfirmPassword.Size = new System.Drawing.Size(420, 20);
            this.txtConfirmPassword.TabIndex = 0;
            // 
            // btnToggleConfirm
            // 
            this.btnToggleConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleConfirm.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnToggleConfirm.Location = new System.Drawing.Point(430, 0);
            this.btnToggleConfirm.Name = "btnToggleConfirm";
            this.btnToggleConfirm.Size = new System.Drawing.Size(38, 30);
            this.btnToggleConfirm.TabIndex = 1;
            this.btnToggleConfirm.Text = "👁️";
            this.btnToggleConfirm.UseVisualStyleBackColor = true;
            // 
            // lblPasswordMatch
            // 
            this.lblPasswordMatch.AutoSize = true;
            this.lblPasswordMatch.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lblPasswordMatch.Location = new System.Drawing.Point(40, 410);
            this.lblPasswordMatch.Name = "lblPasswordMatch";
            this.lblPasswordMatch.Size = new System.Drawing.Size(0, 15);
            this.lblPasswordMatch.TabIndex = 10;
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(52, 152, 219);
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(40, 440);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(220, 45);
            this.btnReset.TabIndex = 11;
            this.btnReset.Text = "🔐 Đặt lại mật khẩu";
            this.btnReset.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(149, 165, 166);
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnCancel.ForeColor = System.Drawing.Color.White;
            this.btnCancel.Location = new System.Drawing.Point(290, 440);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(220, 45);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "❌ Hủy";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(40, 495);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(470, 3);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 13;
            this.progressBar.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblStatus.Location = new System.Drawing.Point(40, 503);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(470, 20);
            this.lblStatus.TabIndex = 14;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelDivider
            // 
            this.panelDivider.BackColor = System.Drawing.Color.FromArgb(220, 220, 220);
            this.panelDivider.Location = new System.Drawing.Point(40, 533);
            this.panelDivider.Name = "panelDivider";
            this.panelDivider.Size = new System.Drawing.Size(470, 1);
            this.panelDivider.TabIndex = 15;
            // 
            // lblPasswordRules
            // 
            this.lblPasswordRules.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblPasswordRules.ForeColor = System.Drawing.Color.FromArgb(127, 140, 141);
            this.lblPasswordRules.Location = new System.Drawing.Point(40, 540);
            this.lblPasswordRules.Name = "lblPasswordRules";
            this.lblPasswordRules.Size = new System.Drawing.Size(470, 40);
            this.lblPasswordRules.TabIndex = 16;
            this.lblPasswordRules.Text = "ℹ️ Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số";
            this.lblPasswordRules.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ResetPasswordForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(550, 700);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ResetPasswordForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Đặt Lại Mật Khẩu - Chat Nội Bộ";
            this.panelHeader.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.panelNewPassword.ResumeLayout(false);
            this.panelNewPassword.PerformLayout();
            this.panelConfirmPassword.ResumeLayout(false);
            this.panelConfirmPassword.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.Label lblUser;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblOtp;
        private System.Windows.Forms.TextBox txtOtp;
        private System.Windows.Forms.Label lblOtpHint;
        private System.Windows.Forms.Label lblNewPassword;
        private System.Windows.Forms.Panel panelNewPassword;
        private System.Windows.Forms.TextBox txtNewPassword;
        private System.Windows.Forms.Button btnTogglePassword;
        private System.Windows.Forms.Label lblConfirmPassword;
        private System.Windows.Forms.Panel panelConfirmPassword;
        private System.Windows.Forms.TextBox txtConfirmPassword;
        private System.Windows.Forms.Button btnToggleConfirm;
        private System.Windows.Forms.Label lblPasswordMatch;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panelDivider;
        private System.Windows.Forms.Label lblPasswordRules;
    }
}
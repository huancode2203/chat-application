namespace ChatClient.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblCaptcha;
        private System.Windows.Forms.TextBox txtCaptcha;
        private System.Windows.Forms.Button btnRefreshCaptcha;
        private System.Windows.Forms.PictureBox picCaptcha;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Button btnForgotPassword;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && picCaptcha.Image != null)
            {
                picCaptcha.Image.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlMain = new Panel();
            btnForgotPassword = new Button();
            btnRegister = new Button();
            lblStatus = new Label();
            btnLogin = new Button();
            picCaptcha = new PictureBox();
            btnRefreshCaptcha = new Button();
            txtCaptcha = new TextBox();
            lblCaptcha = new Label();
            lblPassword = new Label();
            txtPassword = new TextBox();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblTitle = new Label();
            pnlMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).BeginInit();
            SuspendLayout();
            // 
            // pnlMain
            // 
            pnlMain.BackColor = Color.FromArgb(245, 247, 250);
            pnlMain.Controls.Add(btnForgotPassword);
            pnlMain.Controls.Add(btnRegister);
            pnlMain.Controls.Add(lblStatus);
            pnlMain.Controls.Add(btnLogin);
            pnlMain.Controls.Add(picCaptcha);
            pnlMain.Controls.Add(btnRefreshCaptcha);
            pnlMain.Controls.Add(txtCaptcha);
            pnlMain.Controls.Add(lblCaptcha);
            pnlMain.Controls.Add(lblPassword);
            pnlMain.Controls.Add(txtPassword);
            pnlMain.Controls.Add(lblUsername);
            pnlMain.Controls.Add(txtUsername);
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 0);
            pnlMain.Margin = new Padding(4, 5, 4, 5);
            pnlMain.Name = "pnlMain";
            pnlMain.Size = new Size(714, 750);
            pnlMain.TabIndex = 0;
            // 
            // btnForgotPassword
            // 
            btnForgotPassword.BackColor = Color.FromArgb(100, 100, 100);
            btnForgotPassword.FlatAppearance.BorderSize = 0;
            btnForgotPassword.FlatStyle = FlatStyle.Flat;
            btnForgotPassword.Font = new Font("Arial", 10F);
            btnForgotPassword.ForeColor = Color.White;
            btnForgotPassword.Location = new Point(371, 576);
            btnForgotPassword.Margin = new Padding(4, 5, 4, 5);
            btnForgotPassword.Name = "btnForgotPassword";
            btnForgotPassword.Size = new Size(200, 58);
            btnForgotPassword.TabIndex = 11;
            btnForgotPassword.Text = "Quên mật khẩu";
            btnForgotPassword.UseVisualStyleBackColor = false;
            // 
            // btnRegister
            // 
            btnRegister.BackColor = Color.FromArgb(0, 102, 204);
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Arial", 10F);
            btnRegister.ForeColor = Color.White;
            btnRegister.Location = new Point(143, 576);
            btnRegister.Margin = new Padding(4, 5, 4, 5);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(200, 58);
            btnRegister.TabIndex = 10;
            btnRegister.Text = "Đăng ký";
            btnRegister.UseVisualStyleBackColor = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.Red;
            lblStatus.Location = new Point(143, 567);
            lblStatus.Margin = new Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 25);
            lblStatus.TabIndex = 9;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(255, 152, 0);
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Arial", 12F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(143, 445);
            btnLogin.Margin = new Padding(4, 5, 4, 5);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(429, 75);
            btnLogin.TabIndex = 8;
            btnLogin.Text = "ĐĂNG NHẬP";
            btnLogin.UseVisualStyleBackColor = false;
            // 
            // picCaptcha
            // 
            picCaptcha.BorderStyle = BorderStyle.FixedSingle;
            picCaptcha.Location = new Point(394, 340);
            picCaptcha.Margin = new Padding(4, 5, 4, 5);
            picCaptcha.Name = "picCaptcha";
            picCaptcha.Size = new Size(177, 58);
            picCaptcha.SizeMode = PictureBoxSizeMode.StretchImage;
            picCaptcha.TabIndex = 7;
            picCaptcha.TabStop = false;
            // 
            // btnRefreshCaptcha
            // 
            btnRefreshCaptcha.BackColor = Color.FromArgb(255, 152, 0);
            btnRefreshCaptcha.FlatAppearance.BorderSize = 0;
            btnRefreshCaptcha.FlatStyle = FlatStyle.Flat;
            btnRefreshCaptcha.Font = new Font("Arial", 12F, FontStyle.Bold);
            btnRefreshCaptcha.ForeColor = Color.White;
            btnRefreshCaptcha.Location = new Point(579, 340);
            btnRefreshCaptcha.Margin = new Padding(4, 5, 4, 5);
            btnRefreshCaptcha.Name = "btnRefreshCaptcha";
            btnRefreshCaptcha.Size = new Size(61, 58);
            btnRefreshCaptcha.TabIndex = 6;
            btnRefreshCaptcha.Text = "↻";
            btnRefreshCaptcha.UseVisualStyleBackColor = false;
            // 
            // txtCaptcha
            // 
            txtCaptcha.BorderStyle = BorderStyle.FixedSingle;
            txtCaptcha.Font = new Font("Arial", 11F);
            txtCaptcha.Location = new Point(143, 350);
            txtCaptcha.Margin = new Padding(4, 5, 4, 5);
            txtCaptcha.Name = "txtCaptcha";
            txtCaptcha.Size = new Size(183, 33);
            txtCaptcha.TabIndex = 5;
            // 
            // lblCaptcha
            // 
            lblCaptcha.AutoSize = true;
            lblCaptcha.Font = new Font("Arial", 10F);
            lblCaptcha.ForeColor = Color.Gray;
            lblCaptcha.Location = new Point(143, 317);
            lblCaptcha.Margin = new Padding(4, 0, 4, 0);
            lblCaptcha.Name = "lblCaptcha";
            lblCaptcha.Size = new Size(94, 23);
            lblCaptcha.TabIndex = 4;
            lblCaptcha.Text = "🔐 Nhập mã captcha:";
            //
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI", 10F);
            lblPassword.ForeColor = Color.FromArgb(60, 60, 60);
            lblPassword.Location = new Point(143, 210);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(100, 25);
            lblPassword.Text = "🔒 Mật khẩu";
            // 
            // txtPassword
            // 
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.Font = new Font("Segoe UI", 11F);
            txtPassword.Location = new Point(143, 240);
            txtPassword.Margin = new Padding(4, 5, 4, 5);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '●';
            txtPassword.Size = new Size(428, 34);
            txtPassword.TabIndex = 2;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI", 10F);
            lblUsername.ForeColor = Color.FromArgb(60, 60, 60);
            lblUsername.Location = new Point(143, 135);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(130, 25);
            lblUsername.Text = "👤 Tên đăng nhập";
            // 
            // txtUsername
            // 
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.Font = new Font("Segoe UI", 11F);
            txtUsername.Location = new Point(143, 165);
            txtUsername.Margin = new Padding(4, 5, 4, 5);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(428, 34);
            txtUsername.TabIndex = 1;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Arial", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 102, 204);
            lblTitle.Location = new Point(152, 66);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(379, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "ĐĂNG NHẬP HỆ THỐNG";
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(714, 750);
            Controls.Add(pnlMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Đăng nhập hệ thống";
            pnlMain.ResumeLayout(false);
            pnlMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picCaptcha).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}

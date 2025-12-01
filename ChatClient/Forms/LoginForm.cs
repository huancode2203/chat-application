using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form đăng nhập hoàn chỉnh.
    /// - Kết nối server qua TCP.
    /// - Gửi request Login với username/password.
    /// - Nếu thành công, mở ChatForm.
    /// - Có nút mở RegisterForm và ForgotPasswordForm.
    /// </summary>
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            btnLogin.Click += async (_, _) => await BtnLogin_Click();
            btnRegister.Click += BtnRegister_Click;
            btnForgotPassword.Click += BtnForgotPassword_Click;

            txtPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    btnLogin.PerformClick();
                }
            };
        }

        private async Task BtnLogin_Click()
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                lblStatus.Text = "Vui lòng nhập đủ tên đăng nhập và mật khẩu.";
                return;
            }

            btnLogin.Enabled = false;
            lblStatus.Text = "Đang kết nối...";

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.LoginAsync(username, password);
                if (response == null || !response.Success)
                {
                    lblStatus.Text = response?.Message ?? "Lỗi kết nối server.";
                    btnLogin.Enabled = true;
                    return;
                }

                // Đăng nhập thành công
                var user = new User
                {
                    Username = username,
                    Password = password,
                    ClearanceLevel = response.ClearanceLevel
                };

                var chatForm = new ChatForm(user);
                chatForm.FormClosed += (_, _) => Close();
                chatForm.Show();
                Hide();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnLogin.Enabled = true;
            }
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }

        private void BtnForgotPassword_Click(object? sender, EventArgs e)
        {
            var forgotForm = new ForgotPasswordForm();
            forgotForm.ShowDialog();
        }
    }
}

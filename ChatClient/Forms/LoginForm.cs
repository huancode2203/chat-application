using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;
using ChatClient.Utils;

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
        private string _currentCaptcha = string.Empty;

        public LoginForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            LoadCaptcha();
        }

        private void SetupEventHandlers()
        {
            btnLogin.Click += async (_, _) => await BtnLogin_Click();
            btnRegister.Click += BtnRegister_Click;
            btnForgotPassword.Click += BtnForgotPassword_Click;
            btnRefreshCaptcha.Click += (_, _) => LoadCaptcha();

            txtPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    btnLogin.PerformClick();
                }
            };

            txtCaptcha.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    btnLogin.PerformClick();
                }
            };
        }

        private void LoadCaptcha()
        {
            _currentCaptcha = CaptchaHelper.GenerateCaptcha();
            var captchaImage = CaptchaHelper.GenerateCaptchaImage(_currentCaptcha);
            picCaptcha.Image?.Dispose();
            picCaptcha.Image = captchaImage;
            txtCaptcha.Clear();
        }

        private async Task BtnLogin_Click()
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text.Trim();
            var captcha = txtCaptcha.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                lblStatus.Text = "Vui lòng nhập đủ tên đăng nhập và mật khẩu.";
                return;
            }

            if (string.IsNullOrWhiteSpace(captcha))
            {
                lblStatus.Text = "Vui lòng nhập mã captcha.";
                return;
            }

            if (!CaptchaHelper.ValidateCaptcha(captcha))
            {
                lblStatus.Text = "Mã captcha không đúng. Vui lòng thử lại.";
                LoadCaptcha();
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
                    var errorMessage = response?.Message ?? "Lỗi kết nối server.";
                    lblStatus.Text = errorMessage;
                    
                    // Nếu lỗi là chưa verify OTP, hỏi user có muốn verify không
                    if (errorMessage.Contains("verify") || errorMessage.Contains("OTP") || errorMessage.Contains("xác minh"))
                    {
                        var result = MessageBox.Show(
                            $"{errorMessage}\n\nBạn có muốn xác minh OTP ngay bây giờ không?",
                            "Chưa xác minh OTP",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        
                        if (result == DialogResult.Yes)
                        {
                            var verifyForm = new VerifyOtpForm(username);
                            verifyForm.ShowDialog();
                            // Sau khi verify, thử đăng nhập lại
                            btnLogin.Enabled = true;
                            return;
                        }
                    }
                    
                    btnLogin.Enabled = true;
                    return;
                }

                // Đăng nhập thành công
                var user = new User
                {
                    Matk = username, // MATK = TENTK trong trường hợp này
                    Username = username, // TENTK
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

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
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
        private CheckBox? chkRememberMe;
        private CheckBox? chkShowPassword;
        private string _loginStorePath = string.Empty;

        public LoginForm()
        {
            InitializeComponent();
            SetupModernUI();
            SetupEventHandlers();
            InitializeExtraControls();
            LoadRememberedCredentials();
            LoadCaptcha();
        }

        private void SetupModernUI()
        {
            // Các style đã được set trong Designer.cs
            // Method này chỉ để set các thuộc tính runtime nếu cần
            if (this.Text == "LoginForm")
                this.Text = "Đăng nhập - Chat Application";
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

        private void InitializeExtraControls()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "ChatApplication");
            try { Directory.CreateDirectory(dir); } catch { }
            _loginStorePath = Path.Combine(dir, "login.json");

            chkRememberMe = new CheckBox
            {
                Text = "Ghi nhớ tôi",
                AutoSize = true
            };

            chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                AutoSize = true
            };

            try
            {
                txtPassword.UseSystemPasswordChar = true;
            }
            catch { }

            chkShowPassword.CheckedChanged += (_, _) =>
            {
                try { txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked; } catch { }
            };

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    btnLogin.PerformClick();
                }
            };

            try
            {
                if (txtPassword != null)
                {
                    chkShowPassword.Location = new System.Drawing.Point(txtPassword.Right - 140, txtPassword.Top + 3);
                    chkShowPassword.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    this.Controls.Add(chkShowPassword);
                }

                if (btnLogin != null)
                {
                    chkRememberMe.Location = new System.Drawing.Point(btnLogin.Left, btnLogin.Bottom + 8);
                    chkRememberMe.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    this.Controls.Add(chkRememberMe);
                }
            }
            catch { }
        }

        private void LoadRememberedCredentials()
        {
            try
            {
                if (File.Exists(_loginStorePath))
                {
                    var json = File.ReadAllText(_loginStorePath);
                    var data = JsonSerializer.Deserialize<LoginData>(json);
                    if (data != null && !string.IsNullOrWhiteSpace(data.Username))
                    {
                        txtUsername.Text = data.Username;
                        if (chkRememberMe != null) chkRememberMe.Checked = true;
                    }
                }
            }
            catch { }
        }

        private void SaveOrClearRemembered(string username)
        {
            try
            {
                if (chkRememberMe != null && chkRememberMe.Checked)
                {
                    var json = JsonSerializer.Serialize(new LoginData { Username = username });
                    File.WriteAllText(_loginStorePath, json);
                }
                else
                {
                    if (File.Exists(_loginStorePath)) File.Delete(_loginStorePath);
                }
            }
            catch { }
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
                    
                    // Check if user is banned
                    if (errorMessage.Contains("bị cấm") || errorMessage.Contains("banned") || errorMessage.Contains("bị khóa"))
                    {
                        lblStatus.Text = "❌ Tài khoản của bạn đã bị khóa!";
                        MessageBox.Show("Tài khoản của bạn đã bị quản trị viên khóa.\nVui lòng liên hệ admin để biết thêm chi tiết.",
                                      "Tài khoản bị khóa",
                                      MessageBoxButtons.OK,
                                      MessageBoxIcon.Stop);
                        btnLogin.Enabled = true;
                        return;
                    }
                    
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
                lblStatus.Text = "✓ Đăng nhập thành công!";
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(40, 167, 69);
                
                var user = new User
                {
                    Matk = username, // MATK = TENTK trong trường hợp này
                    Username = username, // TENTK
                    Password = password,
                    ClearanceLevel = response.ClearanceLevel
                };

                SaveOrClearRemembered(username);

                // Small delay for UX
                await Task.Delay(300);
                
                var chatForm = new ChatFormNew(user);
                chatForm.FormClosed += (s, args) =>
                {
                    // If user logged out (DialogResult.Cancel), show login form again
                    if (chatForm.DialogResult == DialogResult.Cancel)
                    {
                        this.Show();
                        // Clear password for security
                        txtPassword.Text = "";
                        txtUsername.Focus();
                    }
                    else
                    {
                        // Normal close - exit application
                        Close();
                    }
                };
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

        private class LoginData
        {
            public string Username { get; set; } = string.Empty;
        }
    }
}

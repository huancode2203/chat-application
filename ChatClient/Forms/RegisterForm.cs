using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form đăng ký tài khoản mới.
    /// - Nhập username, password, confirm password, email, clearance level.
    /// - Gửi request Register lên server.
    /// - Nếu thành công, mở VerifyOtpForm.
    /// </summary>
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            btnRegister.Click += async (_, _) => await BtnRegister_Click();
            btnCancel.Click += (_, _) => Close();
            cbClearance.SelectedIndex = 0;
        }

        private async Task BtnRegister_Click()
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text.Trim();
            var confirmPassword = txtConfirmPassword.Text.Trim();
            var email = txtEmail.Text.Trim();
            var clearanceLevel = cbClearance.SelectedIndex + 1;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(email))
            {
                lblStatus.Text = "Vui lòng điền đầy đủ thông tin.";
                return;
            }

            if (password != confirmPassword)
            {
                lblStatus.Text = "Mật khẩu xác nhận không khớp.";
                return;
            }

            if (!email.Contains("@"))
            {
                lblStatus.Text = "Email không hợp lệ.";
                return;
            }

            btnRegister.Enabled = false;
            lblStatus.Text = "Đang đăng ký...";

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.RegisterAsync(username, password, email, clearanceLevel);
                if (response == null || !response.Success)
                {
                    lblStatus.Text = response?.Message ?? "Lỗi đăng ký.";
                    btnRegister.Enabled = true;
                    return;
                }

                MessageBox.Show("Đăng ký thành công! Vui lòng xác minh OTP được gửi đến email của bạn.", 
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var verifyForm = new VerifyOtpForm(username);
                verifyForm.ShowDialog();
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnRegister.Enabled = true;
            }
        }
    }
}

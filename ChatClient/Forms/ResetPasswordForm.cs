using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form đặt lại mật khẩu với OTP.
    /// - Nhập username, OTP, mật khẩu mới, xác nhận mật khẩu.
    /// - Gửi request ResetPassword lên server.
    /// </summary>
    public partial class ResetPasswordForm : Form
    {
        private readonly string _username;

        public ResetPasswordForm(string username)
        {
            _username = username;
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            lblUser.Text = $"Tên đăng nhập: {_username}";
            btnReset.Click += async (_, _) => await BtnReset_Click();
            btnCancel.Click += (_, _) => Close();

            txtOtp.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                {
                    e.Handled = true;
                }
            };
        }

        private async Task BtnReset_Click()
        {
            var otp = txtOtp.Text.Trim();
            var newPassword = txtNewPassword.Text.Trim();
            var confirmPassword = txtConfirmPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6)
            {
                lblStatus.Text = "Vui lòng nhập mã OTP 6 chữ số.";
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                lblStatus.Text = "Vui lòng nhập mật khẩu mới.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                lblStatus.Text = "Mật khẩu xác nhận không khớp.";
                return;
            }

            btnReset.Enabled = false;
            lblStatus.Text = "Đang đặt lại mật khẩu...";

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.ResetPasswordAsync(_username, otp, newPassword);
                if (response == null || !response.Success)
                {
                    lblStatus.Text = response?.Message ?? "Lỗi đặt lại mật khẩu.";
                    btnReset.Enabled = true;
                    return;
                }

                MessageBox.Show("Đặt lại mật khẩu thành công! Bạn có thể đăng nhập với mật khẩu mới.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnReset.Enabled = true;
            }
        }
    }
}

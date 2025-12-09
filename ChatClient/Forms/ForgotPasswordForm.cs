using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form yêu cầu quên mật khẩu.
    /// - Nhập username và email.
    /// - Gửi request ForgotPasswordRequest lên server.
    /// - Nếu thành công, mở ResetPasswordForm.
    /// </summary>
    public partial class ForgotPasswordForm : Form
    {
        public ForgotPasswordForm()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            btnSubmit.Click += async (_, _) => await BtnSubmit_Click();
            btnCancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            
            // Quay lại đăng nhập
            lnkBackToLogin.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
        }

        private async Task BtnSubmit_Click()
        {
            var username = txtUsername.Text.Trim();
            var email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                lblStatus.Text = "Vui lòng điền đầy đủ thông tin.";
                return;
            }

            if (!email.Contains("@"))
            {
                lblStatus.Text = "Email không hợp lệ.";
                return;
            }

            btnSubmit.Enabled = false;
            lblStatus.Text = "Đang gửi yêu cầu...";

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.ForgotPasswordRequestAsync(username, email);
                if (response == null || !response.Success)
                {
                    lblStatus.Text = response?.Message ?? "Lỗi gửi yêu cầu.";
                    btnSubmit.Enabled = true;
                    return;
                }

                MessageBox.Show("OTP đã được gửi đến email của bạn. Vui lòng kiểm tra và nhập mã OTP.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var resetForm = new ResetPasswordForm(username);
                resetForm.ShowDialog();
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnSubmit.Enabled = true;
            }
        }
    }
}

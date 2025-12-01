using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form xác minh OTP sau khi đăng ký.
    /// - Nhập username và OTP.
    /// - Gửi request VerifyOtp lên server.
    /// </summary>
    public partial class VerifyOtpForm : Form
    {
        private readonly string _username;

        public VerifyOtpForm(string username)
        {
            _username = username;
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            lblUser.Text = $"Tên đăng nhập: {_username}";
            btnVerify.Click += async (_, _) => await BtnVerify_Click();
            btnCancel.Click += (_, _) => Close();

            txtOtp.KeyPress += (s, e) =>
            {
                if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                {
                    e.Handled = true;
                }
            };
        }

        private async Task BtnVerify_Click()
        {
            var otp = txtOtp.Text.Trim();

            if (string.IsNullOrWhiteSpace(otp) || otp.Length != 6)
            {
                lblStatus.Text = "Vui lòng nhập mã OTP 6 chữ số.";
                return;
            }

            btnVerify.Enabled = false;
            lblStatus.Text = "Đang xác minh...";

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.VerifyOtpAsync(_username, otp);
                if (response == null || !response.Success)
                {
                    lblStatus.Text = response?.Message ?? "Lỗi xác minh OTP.";
                    btnVerify.Enabled = true;
                    return;
                }

                MessageBox.Show("Xác minh OTP thành công! Bạn có thể đăng nhập ngay bây giờ.",
                    "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnVerify.Enabled = true;
            }
        }
    }
}

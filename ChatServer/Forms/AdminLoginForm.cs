using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatServer.Database;
using ChatServer.Utils;

namespace ChatServer.Forms
{
    public partial class AdminLoginForm : Form
    {
        private readonly DbContext _dbContext;

        public AdminLoginForm(DbContext dbContext)
        {
            _dbContext = dbContext;
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập đủ tên đăng nhập và mật khẩu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            lblStatus.Text = "Đang xác thực...";

            try
            {
                var account = await _dbContext.GetUserAccountAsync(username);
                if (account == null || !PasswordHelper.VerifyPassword(password, account.PasswordHash))
                {
                    lblStatus.Text = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    btnLogin.Enabled = true;
                    return;
                }

                if (account.ClearanceLevel < 3)
                {
                    lblStatus.Text = "Bạn không có quyền admin (cần Clearance Level >= 3).";
                    btnLogin.Enabled = true;
                    return;
                }

                if (!await _dbContext.IsOtpVerifiedAsync(username))
                {
                    lblStatus.Text = "Vui lòng xác minh OTP trước khi đăng nhập admin.";
                    btnLogin.Enabled = true;
                    return;
                }

                // Login successful
                DialogResult = DialogResult.OK;
                var adminForm = new AdminPanelForm(_dbContext, username, account.ClearanceLevel);
                adminForm.Show();
                Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnLogin.Enabled = true;
            }
        }
    }
}


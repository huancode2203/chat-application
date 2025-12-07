using System;
using System.Drawing;
using System.Windows.Forms;
using ChatServer.Database;
using ChatServer.Utils;

namespace ChatServer.Forms
{
    /// <summary>
    /// Form để tạo mới hoặc chỉnh sửa thông tin người dùng
    /// </summary>
    public class UserEditForm : Form
    {
        private readonly DbContext _dbContext;
        private readonly string? _existingMatkOrUsername;
        private readonly bool _isEditMode;
        private string? _resolvedMatk; // MATK thực sự từ database

        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private TextBox txtEmail = null!;
        private TextBox txtHovaten = null!;
        private TextBox txtPhone = null!;
        private ComboBox cbClearance = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Label lblStatus = null!;

        public UserEditForm(DbContext dbContext, string? existingMatkOrUsername = null)
        {
            _dbContext = dbContext;
            _existingMatkOrUsername = existingMatkOrUsername;
            _isEditMode = !string.IsNullOrEmpty(existingMatkOrUsername);

            InitializeUI();
            
            if (_isEditMode)
            {
                _ = LoadUserDataAsync();
            }
        }

        private void InitializeUI()
        {
            this.Text = _isEditMode ? "Chỉnh sửa người dùng" : "Tạo người dùng mới";
            this.Size = new Size(920, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 12F);

            var lblTitle = new Label
            {
                Text = _isEditMode ? "📝 CHỈNH SỬA NGƯỜI DÙNG" : "➕ TẠO NGƯỜI DÙNG MỚI",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(40, 25),
                AutoSize = true
            };

            var y = 85;
            var lblSpacing = 80;
            var labelX = 40;
            var inputX = 290;  // Increased spacing: 250px from labelX
            var inputWidth = 560;  // Wider input fields

            // Username
            var lblUsername = new Label { Text = "👤 Tên đăng nhập:", Location = new Point(labelX, y), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            txtUsername = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 38), ReadOnly = _isEditMode, Font = new Font("Segoe UI", 12F) };
            y += lblSpacing;

            // Password
            var lblPassword = new Label { Text = "🔒 Mật khẩu:", Location = new Point(labelX, y), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            txtPassword = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 38), PasswordChar = '●', Font = new Font("Segoe UI", 12F) };
            if (_isEditMode)
            {
                txtPassword.PlaceholderText = "(Để trống nếu không đổi)";
            }
            y += lblSpacing;

            // Email
            var lblEmail = new Label { Text = "📧 Email:", Location = new Point(labelX, y), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            txtEmail = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 38), Font = new Font("Segoe UI", 12F) };
            y += lblSpacing;

            // Họ và tên
            var lblHovaten = new Label { Text = "📝 Họ và tên:", Location = new Point(labelX, y), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            txtHovaten = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 38), Font = new Font("Segoe UI", 12F) };
            y += lblSpacing;

            // Phone
            var lblPhone = new Label { Text = "📱 Số điện thoại:", Location = new Point(labelX, y), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            txtPhone = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 38), Font = new Font("Segoe UI", 12F) };
            y += lblSpacing;

            // Clearance Level
            var lblClearance = new Label { Text = "🔐 Mức bảo mật:", Location = new Point(labelX, y), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            cbClearance = new ComboBox
            {
                Location = new Point(inputX, y - 3),
                Size = new Size(inputWidth, 38),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12F)
            };
            cbClearance.Items.AddRange(new object[] { "1 - LOW", "2 - MEDIUM", "3 - HIGH", "4 - TOP SECRET", "5 - CLASSIFIED" });
            cbClearance.SelectedIndex = 0;
            y += lblSpacing + 20;

            // Status label
            lblStatus = new Label
            {
                Location = new Point(labelX, y),
                Size = new Size(820, 35),
                ForeColor = Color.Red,
                Text = "",
                Font = new Font("Segoe UI", 11F)
            };
            y += 50;

            // Buttons
            btnSave = new Button
            {
                Text = _isEditMode ? "💾 LƯU THAY ĐỔI" : "➕ TẠO TÀI KHOẢN",
                Size = new Size(240, 55),
                Location = new Point(280, y),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) => await SaveUserAsync();

            btnCancel = new Button
            {
                Text = "❌ HỦY",
                Size = new Size(160, 55),
                Location = new Point(540, y),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 13F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblUsername, txtUsername, lblPassword, txtPassword,
                lblEmail, txtEmail, lblHovaten, txtHovaten, lblPhone, txtPhone,
                lblClearance, cbClearance, lblStatus, btnSave, btnCancel
            });
        }

        private async System.Threading.Tasks.Task LoadUserDataAsync()
        {
            try
            {
                var user = await _dbContext.GetUserDetailsAsync(_existingMatkOrUsername!);
                if (user == null)
                {
                    MessageBox.Show("Không tìm thấy người dùng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                // Lưu MATK thực sự để dùng khi update
                _resolvedMatk = user.Matk;

                txtUsername.Text = user.Username;
                txtEmail.Text = user.Email;
                txtHovaten.Text = user.Hovaten;
                txtPhone.Text = user.Phone;
                cbClearance.SelectedIndex = Math.Max(0, Math.Min(4, user.ClearanceLevel - 1));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task SaveUserAsync()
        {
            try
            {
                var username = txtUsername.Text.Trim();
                var password = txtPassword.Text;
                var email = txtEmail.Text.Trim();
                var hovaten = txtHovaten.Text.Trim();
                var phone = txtPhone.Text.Trim();
                var clearanceLevel = cbClearance.SelectedIndex + 1;

                if (string.IsNullOrEmpty(username))
                {
                    lblStatus.Text = "Vui lòng nhập tên đăng nhập!";
                    return;
                }

                if (!_isEditMode && string.IsNullOrEmpty(password))
                {
                    lblStatus.Text = "Vui lòng nhập mật khẩu!";
                    return;
                }

                btnSave.Enabled = false;
                lblStatus.ForeColor = Color.Blue;
                lblStatus.Text = "Đang lưu...";

                if (_isEditMode)
                {
                    // Sử dụng MATK thực sự đã resolve từ database
                    var matkToUpdate = _resolvedMatk ?? _existingMatkOrUsername!;
                    
                    // Update existing user
                    await _dbContext.UpdateUserInfoAsync(matkToUpdate, email, hovaten, phone, clearanceLevel, null);

                    if (!string.IsNullOrEmpty(password))
                    {
                        var passwordHash = PasswordHelper.HashPassword(password);
                        await _dbContext.UpdatePasswordAsync(matkToUpdate, passwordHash);
                    }

                    lblStatus.ForeColor = Color.Green;
                    lblStatus.Text = "✅ Đã lưu thành công!";
                }
                else
                {
                    // Create new user
                    var matk = "TK" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    var passwordHash = PasswordHelper.HashPassword(password);

                    await _dbContext.CreateAccountAsync(matk, username, passwordHash, "VT003", clearanceLevel); // VT003 = Người dùng

                    // Update additional info
                    if (!string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(hovaten) || !string.IsNullOrEmpty(phone))
                    {
                        await _dbContext.UpdateUserInfoAsync(matk, email, hovaten, phone, null, null);
                    }

                    lblStatus.ForeColor = Color.Green;
                    lblStatus.Text = "✅ Đã tạo tài khoản thành công!";
                }

                await System.Threading.Tasks.Task.Delay(500);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.Red;
                lblStatus.Text = $"Lỗi: {ex.Message}";
                btnSave.Enabled = true;
            }
        }
    }
}

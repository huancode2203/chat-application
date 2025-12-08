using System;
using System.Drawing;
using System.Windows.Forms;
using ChatServer.Database;
using ChatServer.Utils;

namespace ChatServer.Forms
{
    /// <summary>
    /// Form để tạo mới hoặc chỉnh sửa thông tin người dùng
    /// Đồng bộ với bảng TAIKHOAN và NGUOIDUNG trong schema
    /// </summary>
    public class UserEditForm : Form
    {
        private readonly DbContext _dbContext;
        private readonly string? _existingMatkOrUsername;
        private readonly bool _isEditMode;
        private string? _resolvedMatk; // MATK thực sự từ database

        // TAIKHOAN fields
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private ComboBox cbClearance = null!;
        private ComboBox cbVaitro = null!;
        
        // NGUOIDUNG fields
        private TextBox txtHovaten = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private TextBox txtDiachi = null!;
        private TextBox txtBio = null!;
        private DateTimePicker dtpNgaysinh = null!;
        private ComboBox cbPhongban = null!;
        private ComboBox cbChucvu = null!;
        
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
            this.Size = new Size(950, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 11F);
            this.AutoScroll = true;

            var lblTitle = new Label
            {
                Text = _isEditMode ? "📝 CHỈNH SỬA NGƯỜI DÙNG" : "➕ TẠO NGƯỜI DÙNG MỚI",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(40, 20),
                AutoSize = true
            };

            var y = 65;
            var lblSpacing = 55;
            var labelX = 40;
            var inputX = 220;
            var inputWidth = 680;
            var font = new Font("Segoe UI", 11F);

            // ===== TAIKHOAN Section =====
            var lblSection1 = new Label { Text = "🔑 THÔNG TIN TÀI KHOẢN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 100, 180), Location = new Point(labelX, y), AutoSize = true };
            y += 35;

            // Username
            var lblUsername = new Label { Text = "Tên đăng nhập:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtUsername = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 30), ReadOnly = _isEditMode, Font = font };
            y += lblSpacing;

            // Password
            var lblPassword = new Label { Text = "Mật khẩu:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtPassword = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 30), PasswordChar = '●', Font = font };
            if (_isEditMode) txtPassword.PlaceholderText = "(Để trống nếu không đổi)";
            y += lblSpacing;

            // Clearance Level
            var lblClearance = new Label { Text = "Mức bảo mật:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            cbClearance = new ComboBox { Location = new Point(inputX, y - 3), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = font };
            cbClearance.Items.AddRange(new object[] { "1 - Thấp", "2 - Trung bình", "3 - Cao", "4 - Tối mật", "5 - Tuyệt mật" });
            cbClearance.SelectedIndex = 0;
            
            // Vai trò
            var lblVaitro = new Label { Text = "Vai trò:", Location = new Point(inputX + 320, y), AutoSize = true, Font = font };
            cbVaitro = new ComboBox { Location = new Point(inputX + 400, y - 3), Size = new Size(280, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = font };
            cbVaitro.Items.AddRange(new object[] { "VT001 - Admin", "VT002 - Moderator", "VT003 - Người dùng" });
            cbVaitro.SelectedIndex = 2;
            y += lblSpacing + 15;

            // ===== NGUOIDUNG Section =====
            var lblSection2 = new Label { Text = "👤 THÔNG TIN CÁ NHÂN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 100, 180), Location = new Point(labelX, y), AutoSize = true };
            y += 35;

            // Họ và tên
            var lblHovaten = new Label { Text = "Họ và tên:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtHovaten = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 30), Font = font };
            y += lblSpacing;

            // Email
            var lblEmail = new Label { Text = "Email:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtEmail = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 30), Font = font };
            y += lblSpacing;

            // Phone
            var lblPhone = new Label { Text = "Số điện thoại:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtPhone = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(300, 30), Font = font };
            
            // Ngày sinh
            var lblNgaysinh = new Label { Text = "Ngày sinh:", Location = new Point(inputX + 320, y), AutoSize = true, Font = font };
            dtpNgaysinh = new DateTimePicker { Location = new Point(inputX + 420, y - 3), Size = new Size(260, 30), Font = font, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = false };
            y += lblSpacing;

            // Địa chỉ
            var lblDiachi = new Label { Text = "Địa chỉ:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtDiachi = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 30), Font = font };
            y += lblSpacing;

            // Phòng ban & Chức vụ
            var lblPhongban = new Label { Text = "Phòng ban:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            cbPhongban = new ComboBox { Location = new Point(inputX, y - 3), Size = new Size(300, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = font };
            cbPhongban.Items.AddRange(new object[] { "", "PB001 - Ban Giám Đốc", "PB002 - Phòng Kế Toán", "PB003 - Phòng Kinh Doanh", "PB004 - Phòng Nhân Sự", "PB005 - Phòng IT" });
            cbPhongban.SelectedIndex = 0;
            
            var lblChucvu = new Label { Text = "Chức vụ:", Location = new Point(inputX + 320, y), AutoSize = true, Font = font };
            cbChucvu = new ComboBox { Location = new Point(inputX + 400, y - 3), Size = new Size(280, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = font };
            cbChucvu.Items.AddRange(new object[] { "", "CV001 - Giám Đốc", "CV002 - Phó Giám Đốc", "CV003 - Trưởng Phòng", "CV004 - Phó Phòng", "CV005 - Nhân Viên", "CV006 - Thực Tập Sinh" });
            cbChucvu.SelectedIndex = 0;
            y += lblSpacing;

            // Bio
            var lblBio = new Label { Text = "Giới thiệu:", Location = new Point(labelX, y), AutoSize = true, Font = font };
            txtBio = new TextBox { Location = new Point(inputX, y - 3), Size = new Size(inputWidth, 70), Font = font, Multiline = true, ScrollBars = ScrollBars.Vertical };
            y += 90;

            // Status label
            lblStatus = new Label { Location = new Point(labelX, y), Size = new Size(860, 30), ForeColor = Color.Red, Text = "", Font = font };
            y += 40;

            // Buttons
            btnSave = new Button
            {
                Text = _isEditMode ? "💾 LƯU THAY ĐỔI" : "➕ TẠO TÀI KHOẢN",
                Size = new Size(200, 50),
                Location = new Point(300, y),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) => await SaveUserAsync();

            btnCancel = new Button
            {
                Text = "❌ HỦY",
                Size = new Size(140, 50),
                Location = new Point(520, y),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 12F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblSection1,
                lblUsername, txtUsername, lblPassword, txtPassword,
                lblClearance, cbClearance, lblVaitro, cbVaitro,
                lblSection2,
                lblHovaten, txtHovaten, lblEmail, txtEmail,
                lblPhone, txtPhone, lblNgaysinh, dtpNgaysinh,
                lblDiachi, txtDiachi,
                lblPhongban, cbPhongban, lblChucvu, cbChucvu,
                lblBio, txtBio,
                lblStatus, btnSave, btnCancel
            });
        }

        private async System.Threading.Tasks.Task LoadUserDataAsync()
        {
            try
            {
                var user = await _dbContext.GetUserDetailsFullAsync(_existingMatkOrUsername!);
                if (user == null)
                {
                    MessageBox.Show("Không tìm thấy người dùng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                // Lưu MATK thực sự để dùng khi update
                _resolvedMatk = user.Matk;

                // TAIKHOAN fields
                txtUsername.Text = user.Username;
                cbClearance.SelectedIndex = Math.Max(0, Math.Min(4, user.ClearanceLevel - 1));
                
                // Vai trò
                var vaitroIndex = user.Mavaitro switch
                {
                    "VT001" => 0,
                    "VT002" => 1,
                    _ => 2
                };
                cbVaitro.SelectedIndex = vaitroIndex;

                // NGUOIDUNG fields
                txtHovaten.Text = user.Hovaten ?? string.Empty;
                txtEmail.Text = user.Email ?? string.Empty;
                txtPhone.Text = user.Sdt ?? string.Empty;
                txtDiachi.Text = user.Diachi ?? string.Empty;
                txtBio.Text = user.Bio ?? string.Empty;
                
                if (user.Ngaysinh.HasValue)
                {
                    dtpNgaysinh.Checked = true;
                    dtpNgaysinh.Value = user.Ngaysinh.Value;
                }
                
                // Phòng ban
                for (int i = 0; i < cbPhongban.Items.Count; i++)
                {
                    if (cbPhongban.Items[i].ToString()!.StartsWith(user.Mapb ?? ""))
                    {
                        cbPhongban.SelectedIndex = i;
                        break;
                    }
                }
                
                // Chức vụ
                for (int i = 0; i < cbChucvu.Items.Count; i++)
                {
                    if (cbChucvu.Items[i].ToString()!.StartsWith(user.Macv ?? ""))
                    {
                        cbChucvu.SelectedIndex = i;
                        break;
                    }
                }
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
                // TAIKHOAN fields
                var username = txtUsername.Text.Trim();
                var password = txtPassword.Text;
                var clearanceLevel = cbClearance.SelectedIndex + 1;
                var mavaitro = cbVaitro.SelectedIndex switch
                {
                    0 => "VT001",
                    1 => "VT002",
                    _ => "VT003"
                };

                // NGUOIDUNG fields
                var hovaten = txtHovaten.Text.Trim();
                var email = txtEmail.Text.Trim();
                var phone = txtPhone.Text.Trim();
                var diachi = txtDiachi.Text.Trim();
                var bio = txtBio.Text.Trim();
                DateTime? ngaysinh = dtpNgaysinh.Checked ? dtpNgaysinh.Value : null;
                
                // Phòng ban & Chức vụ
                var mapb = cbPhongban.SelectedIndex > 0 
                    ? cbPhongban.Items[cbPhongban.SelectedIndex].ToString()!.Split(' ')[0] 
                    : null;
                var macv = cbChucvu.SelectedIndex > 0 
                    ? cbChucvu.Items[cbChucvu.SelectedIndex].ToString()!.Split(' ')[0] 
                    : null;

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
                    var matkToUpdate = _resolvedMatk ?? _existingMatkOrUsername!;
                    
                    // Update TAIKHOAN (clearance, mavaitro)
                    await _dbContext.UpdateUserInfoAsync(matkToUpdate, null, null, null, clearanceLevel, mavaitro);

                    // Update NGUOIDUNG (đầy đủ các cột)
                    await _dbContext.UpdateUserProfileFullAsync(matkToUpdate, hovaten, email, phone, 
                        diachi, bio, null, ngaysinh, mapb, macv);

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

                    await _dbContext.CreateAccountAsync(matk, username, passwordHash, mavaitro, clearanceLevel);

                    // Create NGUOIDUNG record
                    await _dbContext.UpdateUserProfileFullAsync(matk, hovaten, email, phone, 
                        diachi, bio, null, ngaysinh, mapb, macv);

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

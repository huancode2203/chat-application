using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form xem và chỉnh sửa thông tin người dùng
    /// Tương tự profile trong Telegram/Messenger
    /// </summary>
    public partial class UserProfileForm : Form
    {
        private readonly SocketClientService _socketClient;
        private readonly User _currentUser;
        private readonly string? _viewingMatk; // null = xem profile của mình
        private bool _isEditMode = false;
        private bool _isOwnProfile = true;

        private PictureBox picAvatar;
        private Label lblUsername;
        private Label lblStatus;
        private Panel pnlInfo;
        private Label lblFullNameTitle;
        private TextBox txtFullName;
        private Label lblEmailTitle;
        private TextBox txtEmail;
        private Label lblPhoneTitle;
        private TextBox txtPhone;
        private Label lblDiachiTitle;
        private TextBox txtDiachi;
        private Label lblBioTitle;
        private TextBox txtBio;
        private Label lblDepartmentTitle;
        private ComboBox cboDepartment;
        private Label lblPositionTitle;
        private ComboBox cboPosition;
        private Label lblNgaysinhTitle;
        private DateTimePicker dtpNgaysinh;
        private Button btnEdit;
        private Button btnSave;
        private Button btnCancel;
        private Button btnClose;
        private Button btnSendMessage; // For viewing other users
        private Panel pnlButtons;
        private Label lblMemberSince;
        private Label lblClearanceLevel;

        public UserProfileForm(SocketClientService socketClient, User currentUser, string? viewingMatk = null)
        {
            _socketClient = socketClient;
            _currentUser = currentUser;
            _viewingMatk = viewingMatk;
            _isOwnProfile = string.IsNullOrEmpty(viewingMatk) || viewingMatk == currentUser.Matk;

            InitializeComponent();
            InitializeUserProfileLayout();
            ApplyModernStyle();
            SetupControls();

            Shown += async (_, _) => await LoadProfileDataAsync();
        }

        private void InitializeUserProfileLayout()
        {
            this.SuspendLayout();

            // Form settings - Made wider with better spacing
            this.ClientSize = new Size(700, 720);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = _isOwnProfile ? "Thông tin của bạn" : "Thông tin người dùng";
            this.BackColor = Color.FromArgb(245, 246, 250);

            // Avatar
            picAvatar = new PictureBox
            {
                Location = new Point(300, 20),
                Size = new Size(100, 100),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(0, 132, 255)
            };

            // Username
            lblUsername = new Label
            {
                Location = new Point(50, 135),
                Size = new Size(600, 30),
                Text = "Loading...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 30, 33)
            };

            // Status
            lblStatus = new Label
            {
                Location = new Point(50, 165),
                Size = new Size(600, 20),
                Text = "Online",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            // Info Panel
            pnlInfo = new Panel
            {
                Location = new Point(50, 200),
                Size = new Size(600, 380),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Full Name
            lblFullNameTitle = new Label
            {
                Location = new Point(20, 15),
                Size = new Size(150, 25),
                Text = "Họ và tên:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtFullName = new TextBox
            {
                Location = new Point(200, 15),
                Size = new Size(370, 25),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Email
            lblEmailTitle = new Label
            {
                Location = new Point(20, 55),
                Size = new Size(150, 25),
                Text = "Email:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtEmail = new TextBox
            {
                Location = new Point(200, 55),
                Size = new Size(370, 25),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Phone
            lblPhoneTitle = new Label
            {
                Location = new Point(20, 95),
                Size = new Size(150, 25),
                Text = "Số điện thoại:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtPhone = new TextBox
            {
                Location = new Point(200, 95),
                Size = new Size(370, 25),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Địa chỉ
            lblDiachiTitle = new Label
            {
                Location = new Point(20, 135),
                Size = new Size(150, 25),
                Text = "Địa chỉ:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtDiachi = new TextBox
            {
                Location = new Point(200, 132),
                Size = new Size(370, 25),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Ngày sinh
            lblNgaysinhTitle = new Label
            {
                Location = new Point(20, 170),
                Size = new Size(150, 25),
                Text = "Ngày sinh:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            dtpNgaysinh = new DateTimePicker
            {
                Location = new Point(200, 167),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 10F),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false,
                Enabled = false
            };

            // Department
            lblDepartmentTitle = new Label
            {
                Location = new Point(20, 205),
                Size = new Size(150, 25),
                Text = "Phòng ban:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            cboDepartment = new ComboBox
            {
                Location = new Point(200, 202),
                Size = new Size(370, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cboDepartment.Items.AddRange(new object[] {
                "", "Ban Giám Đốc", "Phòng Kế Toán", "Phòng Kinh Doanh", "Phòng Nhân Sự", "Phòng IT"
            });

            // Position
            lblPositionTitle = new Label
            {
                Location = new Point(20, 240),
                Size = new Size(150, 25),
                Text = "Chức vụ:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            cboPosition = new ComboBox
            {
                Location = new Point(200, 237),
                Size = new Size(370, 25),
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cboPosition.Items.AddRange(new object[] {
                "", "Giám Đốc", "Phó Giám Đốc", "Trưởng Phòng", "Phó Phòng", "Nhân Viên", "Thực Tập Sinh"
            });

            // Bio
            lblBioTitle = new Label
            {
                Location = new Point(20, 275),
                Size = new Size(150, 25),
                Text = "Giới thiệu:",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtBio = new TextBox
            {
                Location = new Point(200, 275),
                Size = new Size(370, 50),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Member Since & Clearance Level
            lblMemberSince = new Label
            {
                Location = new Point(20, 335),
                Size = new Size(280, 25),
                Text = "📅 Thành viên từ: ...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(144, 149, 160),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblClearanceLevel = new Label
            {
                Location = new Point(310, 335),
                Size = new Size(260, 25),
                Text = "🔐 Mức bảo mật: ...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(144, 149, 160),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlInfo.Controls.AddRange(new Control[] {
                lblFullNameTitle, txtFullName,
                lblEmailTitle, txtEmail,
                lblPhoneTitle, txtPhone,
                lblDiachiTitle, txtDiachi,
                lblNgaysinhTitle, dtpNgaysinh,
                lblDepartmentTitle, cboDepartment,
                lblPositionTitle, cboPosition,
                lblBioTitle, txtBio,
                lblMemberSince, lblClearanceLevel
            });

            // Buttons Panel
            pnlButtons = new Panel
            {
                Location = new Point(50, 595),
                Size = new Size(600, 55),
                BackColor = Color.Transparent
            };

            btnEdit = new Button
            {
                Location = new Point(150, 10),
                Size = new Size(130, 40),
                Text = "✏️ Chỉnh sửa",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 132, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Visible = _isOwnProfile
            };
            btnEdit.FlatAppearance.BorderSize = 0;

            btnSave = new Button
            {
                Location = new Point(150, 10),
                Size = new Size(130, 40),
                Text = "💾 Lưu",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnSave.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Location = new Point(300, 10),
                Size = new Size(130, 40),
                Text = "❌ Hủy",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnClose = new Button
            {
                Location = new Point(320, 10),
                Size = new Size(130, 40),
                Text = "Đóng",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand,
                Visible = _isOwnProfile
            };
            btnClose.FlatAppearance.BorderSize = 0;

            btnSendMessage = new Button
            {
                Location = new Point(80, 10),
                Size = new Size(230, 35),
                Text = "💬 Gửi tin nhắn",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 132, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Visible = !_isOwnProfile
            };
            btnSendMessage.FlatAppearance.BorderSize = 0;

            pnlButtons.Controls.AddRange(new Control[] {
                btnEdit, btnSave, btnCancel, btnClose, btnSendMessage
            });

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                picAvatar, lblUsername, lblStatus, pnlInfo, pnlButtons
            });

            this.ResumeLayout(false);
        }

        private void ApplyModernStyle()
        {
            // Round avatar
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, picAvatar.Width, picAvatar.Height);
            picAvatar.Region = new Region(path);

            // Add shadow effect to info panel
            pnlInfo.Paint += (s, e) =>
            {
                var rect = pnlInfo.ClientRectangle;
                using var shadowBrush = new SolidBrush(Color.FromArgb(10, 0, 0, 0));
                e.Graphics.FillRectangle(shadowBrush, new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height));
            };
        }

        private void SetupControls()
        {
            btnEdit.Click += (s, e) => EnterEditMode();
            btnSave.Click += async (s, e) => await SaveChangesAsync();
            btnCancel.Click += (s, e) => ExitEditMode();
            btnClose.Click += (s, e) => Close();
            btnSendMessage.Click += (s, e) => SendMessageToUser();

            // Generate avatar placeholder
            picAvatar.Paint += (s, e) =>
            {
                if (picAvatar.Image == null)
                {
                    var initial = string.IsNullOrEmpty(lblUsername.Text) || lblUsername.Text == "Loading..."
                        ? "?"
                        : lblUsername.Text.Substring(0, 1).ToUpper();

                    using var font = new Font("Segoe UI", 36F, FontStyle.Bold);
                    using var brush = new SolidBrush(Color.White);
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString(initial, font, brush,
                        new RectangleF(0, 0, picAvatar.Width, picAvatar.Height), sf);
                }
            };
        }

        private async Task LoadProfileDataAsync()
        {
            try
            {
                if (_isOwnProfile)
                {
                    // Sử dụng thông tin từ User model đã có sẵn
                    lblUsername.Text = _currentUser.Username;
                    txtFullName.Text = !string.IsNullOrEmpty(_currentUser.Hovaten) 
                        ? _currentUser.Hovaten 
                        : _currentUser.Username;
                    txtEmail.Text = _currentUser.Email ?? string.Empty;
                    txtPhone.Text = _currentUser.Sdt ?? string.Empty;
                    txtDiachi.Text = _currentUser.Diachi ?? string.Empty;
                    txtBio.Text = _currentUser.Bio ?? string.Empty;
                    
                    // Ngày sinh
                    if (_currentUser.Ngaysinh.HasValue)
                    {
                        dtpNgaysinh.Checked = true;
                        dtpNgaysinh.Value = _currentUser.Ngaysinh.Value;
                    }
                    
                    // Thời gian tham gia
                    var memberSince = _currentUser.NgayTao != default 
                        ? _currentUser.NgayTao.ToString("dd/MM/yyyy") 
                        : "N/A";
                    lblMemberSince.Text = $"📅 Thành viên từ: {memberSince}";
                    
                    // Mức bảo mật
                    lblClearanceLevel.Text = $"🔐 Mức bảo mật: {_currentUser.ClearanceLevel}";
                    
                    // Trạng thái
                    lblStatus.Text = _currentUser.IsBannedGlobal 
                        ? "🔴 Đã bị khóa" 
                        : "🟢 Trực tuyến";
                }
                else
                {
                    // Lấy thông tin từ server cho user khác
                    var response = await _socketClient.GetUserDetailsAsync(_currentUser, _viewingMatk!);
                    if (response?.Success == true && response.AdminUser != null)
                    {
                        var user = response.AdminUser;
                        lblUsername.Text = user.Username;
                        txtFullName.Text = !string.IsNullOrEmpty(user.Hovaten) ? user.Hovaten : user.Username;
                        txtEmail.Text = user.Email ?? string.Empty;
                        txtPhone.Text = user.Phone ?? string.Empty;
                        txtDiachi.Text = string.Empty;
                        txtBio.Text = string.Empty;
                        lblMemberSince.Text = $"📅 Thành viên từ: {user.NgayTao:dd/MM/yyyy}";
                        lblClearanceLevel.Text = $"🔐 Mức bảo mật: {user.ClearanceLevel}";
                        lblStatus.Text = user.IsBannedGlobal ? "🔴 Đã bị khóa" : "⚪ Ngoại tuyến";
                    }
                    else
                    {
                        lblUsername.Text = _viewingMatk ?? "Unknown";
                        txtFullName.Text = string.Empty;
                        lblStatus.Text = "❓ Không thể tải thông tin";
                    }
                }
                
                picAvatar.Invalidate();
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông tin: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnterEditMode()
        {
            _isEditMode = true;
            txtFullName.ReadOnly = false;
            txtEmail.ReadOnly = false;
            txtPhone.ReadOnly = false;
            txtDiachi.ReadOnly = false;
            txtBio.ReadOnly = false;
            dtpNgaysinh.Enabled = true;
            cboDepartment.Enabled = true;
            cboPosition.Enabled = true;

            txtFullName.BackColor = Color.White;
            txtEmail.BackColor = Color.White;
            txtPhone.BackColor = Color.White;
            txtDiachi.BackColor = Color.White;
            txtBio.BackColor = Color.White;

            btnEdit.Visible = false;
            btnClose.Visible = false;
            btnSave.Visible = true;
            btnCancel.Visible = true;
        }

        private void ExitEditMode()
        {
            _isEditMode = false;
            txtFullName.ReadOnly = true;
            txtEmail.ReadOnly = true;
            txtPhone.ReadOnly = true;
            txtDiachi.ReadOnly = true;
            txtBio.ReadOnly = true;
            dtpNgaysinh.Enabled = false;
            cboDepartment.Enabled = false;
            cboPosition.Enabled = false;

            txtFullName.BackColor = SystemColors.Control;
            txtEmail.BackColor = SystemColors.Control;
            txtPhone.BackColor = SystemColors.Control;
            txtDiachi.BackColor = SystemColors.Control;
            txtBio.BackColor = SystemColors.Control;

            btnEdit.Visible = true;
            btnClose.Visible = true;
            btnSave.Visible = false;
            btnCancel.Visible = false;

            // Reload data
            Task.Run(LoadProfileDataAsync);
        }

        private async Task SaveChangesAsync()
        {
            try
            {
                // Validation
                if (!string.IsNullOrEmpty(txtEmail.Text) && !txtEmail.Text.Contains("@"))
                {
                    MessageBox.Show("Email không hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnSave.Enabled = false;

                // Gọi API cập nhật profile
                var response = await _socketClient.UpdateUserProfileAsync(
                    _currentUser,
                    txtFullName.Text.Trim(),
                    txtEmail.Text.Trim(),
                    txtPhone.Text.Trim(),
                    txtBio.Text.Trim()
                );

                if (response?.Success == true)
                {
                    // Cập nhật lại thông tin trong User model local
                    _currentUser.Hovaten = txtFullName.Text.Trim();
                    _currentUser.Email = txtEmail.Text.Trim();
                    _currentUser.Sdt = txtPhone.Text.Trim();
                    _currentUser.Bio = txtBio.Text.Trim();

                    MessageBox.Show("Cập nhật thông tin thành công!", "Thành công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ExitEditMode();
                }
                else
                {
                    MessageBox.Show(response?.Message ?? "Lỗi không xác định", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi cập nhật: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSave.Enabled = true;
            }
        }

        private void SendMessageToUser()
        {
            // TODO: Open chat with this user
            MessageBox.Show($"Mở chat với {lblUsername.Text}", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
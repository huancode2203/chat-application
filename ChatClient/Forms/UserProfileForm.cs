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
        private Label lblBioTitle;
        private TextBox txtBio;
        private Label lblDepartmentTitle;
        private ComboBox cboDepartment;
        private Label lblPositionTitle;
        private ComboBox cboPosition;
        private Button btnEdit;
        private Button btnSave;
        private Button btnCancel;
        private Button btnClose;
        private Button btnSendMessage; // For viewing other users
        private Panel pnlButtons;
        private Label lblMemberSince;

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

            // Form settings
            this.ClientSize = new Size(450, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = _isOwnProfile ? "Thông tin của bạn" : "Thông tin người dùng";
            this.BackColor = Color.FromArgb(245, 246, 250);

            // Avatar
            picAvatar = new PictureBox
            {
                Location = new Point(175, 20),
                Size = new Size(100, 100),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(0, 132, 255)
            };

            // Username
            lblUsername = new Label
            {
                Location = new Point(50, 135),
                Size = new Size(350, 30),
                Text = "Loading...",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 30, 33)
            };

            // Status
            lblStatus = new Label
            {
                Location = new Point(50, 165),
                Size = new Size(350, 20),
                Text = "Online",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            // Info Panel
            pnlInfo = new Panel
            {
                Location = new Point(30, 200),
                Size = new Size(390, 310),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Full Name
            lblFullNameTitle = new Label
            {
                Location = new Point(15, 15),
                Size = new Size(100, 25),
                Text = "Họ và tên:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtFullName = new TextBox
            {
                Location = new Point(120, 15),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Email
            lblEmailTitle = new Label
            {
                Location = new Point(15, 55),
                Size = new Size(100, 25),
                Text = "Email:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtEmail = new TextBox
            {
                Location = new Point(120, 55),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Phone
            lblPhoneTitle = new Label
            {
                Location = new Point(15, 95),
                Size = new Size(100, 25),
                Text = "Số điện thoại:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtPhone = new TextBox
            {
                Location = new Point(120, 95),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            // Department
            lblDepartmentTitle = new Label
            {
                Location = new Point(15, 135),
                Size = new Size(100, 25),
                Text = "Phòng ban:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            cboDepartment = new ComboBox
            {
                Location = new Point(120, 132),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cboDepartment.Items.AddRange(new object[] {
                "Ban Giám Đốc", "Phòng Kế Toán", "Phòng Kinh Doanh", "Phòng Nhân Sự", "Phòng IT"
            });

            // Position
            lblPositionTitle = new Label
            {
                Location = new Point(15, 170),
                Size = new Size(100, 25),
                Text = "Chức vụ:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            cboPosition = new ComboBox
            {
                Location = new Point(120, 167),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cboPosition.Items.AddRange(new object[] {
                "Giám Đốc", "Phó Giám Đốc", "Trưởng Phòng", "Phó Phòng", "Nhân Viên", "Thực Tập Sinh"
            });

            // Bio
            lblBioTitle = new Label
            {
                Location = new Point(15, 205),
                Size = new Size(100, 25),
                Text = "Giới thiệu:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125)
            };

            txtBio = new TextBox
            {
                Location = new Point(120, 205),
                Size = new Size(250, 60),
                Font = new Font("Segoe UI", 9.5F),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            // Member Since
            lblMemberSince = new Label
            {
                Location = new Point(15, 275),
                Size = new Size(360, 20),
                Text = "Thành viên từ: ...",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(144, 149, 160),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlInfo.Controls.AddRange(new Control[] {
                lblFullNameTitle, txtFullName,
                lblEmailTitle, txtEmail,
                lblPhoneTitle, txtPhone,
                lblDepartmentTitle, cboDepartment,
                lblPositionTitle, cboPosition,
                lblBioTitle, txtBio,
                lblMemberSince
            });

            // Buttons Panel
            pnlButtons = new Panel
            {
                Location = new Point(30, 520),
                Size = new Size(390, 50),
                BackColor = Color.Transparent
            };

            btnEdit = new Button
            {
                Location = new Point(80, 10),
                Size = new Size(110, 35),
                Text = "✏️ Chỉnh sửa",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 132, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Visible = _isOwnProfile
            };
            btnEdit.FlatAppearance.BorderSize = 0;

            btnSave = new Button
            {
                Location = new Point(60, 10),
                Size = new Size(110, 35),
                Text = "💾 Lưu",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnSave.FlatAppearance.BorderSize = 0;

            btnCancel = new Button
            {
                Location = new Point(180, 10),
                Size = new Size(110, 35),
                Text = "❌ Hủy",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnClose = new Button
            {
                Location = new Point(200, 10),
                Size = new Size(110, 35),
                Text = "Đóng",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
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
                var targetMatk = _isOwnProfile ? _currentUser.Matk : _viewingMatk!;

                // TODO: Call API to get user details
                // For now, use mock data
                lblUsername.Text = _isOwnProfile ? _currentUser.Username : "User " + targetMatk;
                txtFullName.Text = "Nguyễn Văn A";
                txtEmail.Text = "user@example.com";
                txtPhone.Text = "0123456789";
                cboDepartment.SelectedIndex = 4; // Phòng IT
                cboPosition.SelectedIndex = 4; // Nhân Viên
                txtBio.Text = "Xin chào! Tôi đang sử dụng Chat App.";
                lblMemberSince.Text = "Ngày tạo tài khoản: 01/01/2024";
                lblStatus.Text = "Trực tuyến";

                picAvatar.Invalidate(); // Trigger repaint for avatar
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
            txtBio.ReadOnly = false;
            cboDepartment.Enabled = true;
            cboPosition.Enabled = true;

            txtFullName.BackColor = Color.White;
            txtEmail.BackColor = Color.White;
            txtPhone.BackColor = Color.White;
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
            txtBio.ReadOnly = true;
            cboDepartment.Enabled = false;
            cboPosition.Enabled = false;

            txtFullName.BackColor = SystemColors.Control;
            txtEmail.BackColor = SystemColors.Control;
            txtPhone.BackColor = SystemColors.Control;
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
                btnSave.Enabled = false;

                // TODO: Call API to update user profile
                // var response = await _socketClient.UpdateUserProfileAsync(...);

                MessageBox.Show("Cập nhật thông tin thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                ExitEditMode();
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
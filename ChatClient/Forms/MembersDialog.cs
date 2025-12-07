using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Forms
{
    public partial class MembersDialog : Form
    {
        private readonly SocketClientService _socketClient;
        private readonly User _currentUser;
        private readonly string _conversationId;
        private readonly bool _isPrivateChat;
        private string _currentOwner = string.Empty;
        private ContextMenuStrip _contextMenu = null!;

        // Controls
        private ListView lstMembers = null!;
        private ColumnHeader colUsername = null!;
        private ColumnHeader colRole = null!;
        private ColumnHeader colBanStatus = null!;
        private ColumnHeader colJoinedDate = null!;
        private Button btnAddMember = null!;
        private Button btnRemoveMember = null!;
        private Button btnBanMember = null!;
        private Button btnUnbanMember = null!;
        private Button btnClose = null!;
        private Label lblStatus = null!;
        private Label lblTitle = null!;

        public MembersDialog(SocketClientService socketClient, User currentUser, string conversationId, bool isPrivateChat = false)
        {
            _socketClient = socketClient;
            _currentUser = currentUser;
            _conversationId = conversationId;
            _isPrivateChat = isPrivateChat;

            InitializeComponent();
            SetupContextMenu();
            SetupControls();
            ApplyModernStyling();
            
            // Hide member management for private chat
            if (_isPrivateChat)
            {
                btnAddMember.Visible = false;
                btnRemoveMember.Visible = false;
                btnBanMember.Visible = false;
                btnUnbanMember.Visible = false;
                lblTitle.Text = "👥 Thành Viên Cuộc Trò Chuyện";
            }
        }

        private void InitializeComponent()
        {
            lstMembers = new ListView();
            colUsername = new ColumnHeader();
            colRole = new ColumnHeader();
            colBanStatus = new ColumnHeader();
            colJoinedDate = new ColumnHeader();
            btnAddMember = new Button();
            btnRemoveMember = new Button();
            btnBanMember = new Button();
            btnUnbanMember = new Button();
            btnClose = new Button();
            lblStatus = new Label();
            lblTitle = new Label();
            SuspendLayout();

            // lstMembers
            lstMembers.Columns.AddRange(new ColumnHeader[] { colUsername, colRole, colBanStatus, colJoinedDate });
            lstMembers.FullRowSelect = true;
            lstMembers.GridLines = true;
            lstMembers.Location = new Point(20, 70);
            lstMembers.MultiSelect = false;
            lstMembers.Name = "lstMembers";
            lstMembers.Size = new Size(760, 450);
            lstMembers.TabIndex = 0;
            lstMembers.UseCompatibleStateImageBehavior = false;
            lstMembers.View = View.Details;

            // Columns
            colUsername.Text = "Người dùng";
            colUsername.Width = 180;
            colRole.Text = "Vai trò";
            colRole.Width = 150;
            colBanStatus.Text = "Trạng thái";
            colBanStatus.Width = 200;
            colJoinedDate.Text = "Ngày tham gia";
            colJoinedDate.Width = 180;

            // btnAddMember
            btnAddMember.Location = new Point(20, 540);
            btnAddMember.Size = new Size(130, 40);
            btnAddMember.Text = "➕ Thêm";

            // btnRemoveMember
            btnRemoveMember.Enabled = false;
            btnRemoveMember.Location = new Point(160, 540);
            btnRemoveMember.Size = new Size(130, 40);
            btnRemoveMember.Text = "❌ Xóa";

            // btnBanMember (now Mute)
            btnBanMember.Enabled = false;
            btnBanMember.Location = new Point(300, 540);
            btnBanMember.Size = new Size(130, 40);
            btnBanMember.Text = "🔇 Tắt tiếng";

            // btnUnbanMember (now Unmute)
            btnUnbanMember.Enabled = false;
            btnUnbanMember.Location = new Point(440, 540);
            btnUnbanMember.Size = new Size(130, 40);
            btnUnbanMember.Text = "🔊 Bỏ tắt tiếng";

            // btnClose
            btnClose.Location = new Point(650, 540);
            btnClose.Size = new Size(130, 40);
            btnClose.Text = "Đóng";

            // lblStatus
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(20, 595);
            lblStatus.Size = new Size(200, 25);

            // lblTitle
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(28, 30, 33);
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(760, 40);
            lblTitle.Text = "👥 Quản Lý Thành Viên";

            // MembersDialog
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 630);
            Controls.Add(lblTitle);
            Controls.Add(lblStatus);
            Controls.Add(btnClose);
            Controls.Add(btnUnbanMember);
            Controls.Add(btnBanMember);
            Controls.Add(btnRemoveMember);
            Controls.Add(btnAddMember);
            Controls.Add(lstMembers);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MembersDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Quản lý thành viên";
            ResumeLayout(false);
            PerformLayout();
        }

        private void ApplyModernStyling()
        {
            BackColor = Color.FromArgb(245, 246, 250);
            lstMembers.BackColor = Color.White;
            lstMembers.ForeColor = Color.FromArgb(28, 30, 33);
            lstMembers.BorderStyle = BorderStyle.None;

            StyleButton(btnAddMember, Color.FromArgb(40, 167, 69));
            StyleButton(btnRemoveMember, Color.FromArgb(220, 53, 69));
            StyleButton(btnBanMember, Color.FromArgb(255, 193, 7));
            StyleButton(btnUnbanMember, Color.FromArgb(23, 162, 184));
            StyleButton(btnClose, Color.FromArgb(108, 117, 125));

            lblStatus.Font = new Font("Segoe UI", 9F);
        }

        private void SetupContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            
            var viewProfileItem = new ToolStripMenuItem("👤 Xem thông tin", null, (s, e) =>
            {
                if (lstMembers.SelectedItems.Count > 0)
                {
                    var username = lstMembers.SelectedItems[0].Tag?.ToString();
                    if (!string.IsNullOrEmpty(username))
                        OpenUserProfile(username);
                }
            });
            _contextMenu.Items.Add(viewProfileItem);

            if (!_isPrivateChat)
            {
                _contextMenu.Items.Add(new ToolStripSeparator());
                
                var muteItem = new ToolStripMenuItem("🔇 Tắt tiếng", null, async (s, e) => await BanMemberAsync());
                var unmuteItem = new ToolStripMenuItem("🔊 Bỏ tắt tiếng", null, async (s, e) => await UnbanMemberAsync());
                var removeItem = new ToolStripMenuItem("❌ Xóa khỏi nhóm", null, async (s, e) => await RemoveMemberAsync());
                
                _contextMenu.Items.Add(muteItem);
                _contextMenu.Items.Add(unmuteItem);
                _contextMenu.Items.Add(removeItem);
            }

            lstMembers.ContextMenuStrip = _contextMenu;
            
            _contextMenu.Opening += (s, e) =>
            {
                if (lstMembers.SelectedItems.Count == 0)
                {
                    e.Cancel = true;
                    return;
                }

                var selectedUser = lstMembers.SelectedItems[0].Tag?.ToString();
                var isOwnProfile = selectedUser == _currentUser.Matk || selectedUser == _currentUser.Username;
                var isOwner = lstMembers.SelectedItems[0].SubItems[1].Text.ToLower().Contains("owner");
                var isMuted = lstMembers.SelectedItems[0].SubItems[2].Text.Contains("Tắt tiếng");

                // Always allow viewing profile
                _contextMenu.Items[0].Enabled = true;

                if (!_isPrivateChat && _contextMenu.Items.Count > 2)
                {
                    // Mute item
                    _contextMenu.Items[2].Enabled = !isOwnProfile && !isOwner && !isMuted;
                    // Unmute item
                    _contextMenu.Items[3].Enabled = !isOwnProfile && isMuted;
                    // Remove item
                    _contextMenu.Items[4].Enabled = !isOwnProfile && !isOwner;
                }
            };
        }

        private void StyleButton(Button btn, Color color)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 9F);
            btn.Cursor = Cursors.Hand;
        }

        private void SetupControls()
        {
            btnAddMember.Click += async (s, e) => await AddMemberAsync();
            btnRemoveMember.Click += async (s, e) => await RemoveMemberAsync();
            btnBanMember.Click += async (s, e) => await BanMemberAsync();
            btnUnbanMember.Click += async (s, e) => await UnbanMemberAsync();
            btnClose.Click += (s, e) => Close();

            lstMembers.SelectedIndexChanged += (s, e) =>
            {
                var hasSelection = lstMembers.SelectedItems.Count > 0;
                var selectedUser = hasSelection ? lstMembers.SelectedItems[0].Tag?.ToString() : null;
                var isOwnProfile = selectedUser == _currentUser.Matk;
                var isOwner = hasSelection && lstMembers.SelectedItems[0].SubItems[1].Text.ToLower().Contains("owner");

                btnRemoveMember.Enabled = hasSelection && !isOwnProfile && !isOwner;
                btnBanMember.Enabled = hasSelection && !isOwnProfile && !isOwner;
                btnUnbanMember.Enabled = hasSelection && !isOwnProfile;

                if (hasSelection)
                {
                    var isMuted = lstMembers.SelectedItems[0].SubItems[2].Text.Contains("Tắt tiếng");
                    btnBanMember.Text = isMuted ? "🔇 Đã tắt tiếng" : "🔇 Tắt tiếng";
                    btnBanMember.Enabled = !isMuted && !isOwnProfile && !isOwner;
                    btnUnbanMember.Enabled = isMuted && !isOwnProfile;
                }
            };

            lstMembers.DoubleClick += (s, e) =>
            {
                if (lstMembers.SelectedItems.Count > 0)
                {
                    var username = lstMembers.SelectedItems[0].Tag?.ToString();
                    if (!string.IsNullOrEmpty(username))
                        OpenUserProfile(username);
                }
            };
        }

        public async Task LoadMembersAsync()
        {
            try
            {
                UpdateStatus("Đang tải danh sách thành viên...", Color.Blue);

                var response = await _socketClient.GetConversationMembersAsync(_currentUser, _conversationId);

                if (response == null || !response.Success)
                {
                    UpdateStatus(response?.Message ?? "Lỗi tải danh sách thành viên.", Color.Red);
                    return;
                }

                lstMembers.BeginUpdate();
                lstMembers.Items.Clear();

                foreach (var member in response.Members.OrderBy(m => m.Role == "owner" ? 0 : m.Role == "admin" ? 1 : 2))
                {
                    var item = new ListViewItem(member.Username);

                    var roleText = member.Role.ToLower() switch
                    {
                        "owner" => "👑 Chủ nhóm",
                        "admin" => "⭐ Quản trị",
                        "moderator" => "🛡️ Điều hành",
                        _ => "👤 Thành viên"
                    };
                    item.SubItems.Add(roleText);

                    var muteStatus = member.IsBanned ? "🔇 Tắt tiếng" : "✅ Bình thường";
                    var muteSubItem = item.SubItems.Add(muteStatus);
                    muteSubItem.ForeColor = member.IsBanned ? Color.Orange : Color.Green;

                    item.SubItems.Add(member.JoinedDate.ToString("dd/MM/yyyy HH:mm"));
                    item.Tag = string.IsNullOrWhiteSpace(member.Matk) ? member.Username : member.Matk;

                    if (member.Role.ToLower() == "owner")
                    {
                        _currentOwner = string.IsNullOrWhiteSpace(member.Matk) ? member.Username : member.Matk;
                        item.Font = new Font(lstMembers.Font, FontStyle.Bold);
                    }

                    if ((!string.IsNullOrWhiteSpace(member.Matk) && member.Matk == _currentUser.Matk) ||
                        (string.IsNullOrWhiteSpace(member.Matk) && member.Username == _currentUser.Matk))
                    {
                        item.BackColor = Color.FromArgb(220, 248, 198);
                    }

                    lstMembers.Items.Add(item);
                }

                lstMembers.EndUpdate();
                UpdateStatus($"Đã tải {response.Members.Length} thành viên.", Color.Green);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}", Color.Red);
            }
        }

        private async Task AddMemberAsync()
        {
            using var addMemberForm = new AddMemberDialog();
            if (addMemberForm.ShowDialog() != DialogResult.OK)
                return;

            var username = addMemberForm.SelectedUsername;
            if (string.IsNullOrWhiteSpace(username))
                return;

            try
            {
                btnAddMember.Enabled = false;
                UpdateStatus("Đang thêm thành viên...", Color.Blue);

                var response = await _socketClient.AddMemberToConversationAsync(
                    _currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi thêm thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"Đã thêm {username} vào nhóm!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAddMember.Enabled = true;
            }
        }

        private async Task RemoveMemberAsync()
        {
            if (lstMembers.SelectedItems.Count == 0) return;

            var username = lstMembers.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(username)) return;

            if (username == _currentUser.Matk)
            {
                MessageBox.Show("Bạn không thể xóa chính mình khỏi nhóm!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (username == _currentOwner)
            {
                MessageBox.Show("Không thể xóa chủ nhóm!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa {username} khỏi cuộc trò chuyện?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                UpdateStatus("Đang xóa thành viên...", Color.Blue);

                var response = await _socketClient.RemoveMemberFromConversationAsync(
                    _currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"Đã xóa {username} khỏi nhóm.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task BanMemberAsync()
        {
            if (lstMembers.SelectedItems.Count == 0) return;

            var username = lstMembers.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(username)) return;

            if (username == _currentUser.Matk || username == _currentOwner)
            {
                MessageBox.Show("Không thể thực hiện hành động này!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var isMuted = lstMembers.SelectedItems[0].SubItems[2].Text.Contains("Tắt tiếng");
            if (isMuted)
            {
                MessageBox.Show("Thành viên này đã bị tắt tiếng!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn tắt tiếng {username}?\nNgười này sẽ không thể gửi tin nhắn trong nhóm.",
                "Xác nhận tắt tiếng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                UpdateStatus("Đang tắt tiếng thành viên...", Color.Blue);
                var response = await _socketClient.MuteMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tắt tiếng thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"Đã tắt tiếng {username}.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UnbanMemberAsync()
        {
            if (lstMembers.SelectedItems.Count == 0) return;

            var username = lstMembers.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(username)) return;

            var isMuted = lstMembers.SelectedItems[0].SubItems[2].Text.Contains("Tắt tiếng");
            if (!isMuted)
            {
                MessageBox.Show("Thành viên này chưa bị tắt tiếng!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                UpdateStatus("Đang bỏ tắt tiếng thành viên...", Color.Blue);
                var response = await _socketClient.UnmuteMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi bỏ tắt tiếng thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"Đã bỏ tắt tiếng {username}.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenUserProfile(string username)
        {
            try
            {
                using var profileForm = new UserProfileForm(_socketClient, _currentUser, username);
                profileForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở hồ sơ: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }
    }

    // Helper dialog for adding members
    public class AddMemberDialog : Form
    {
        private TextBox txtUsername = null!;
        private ListBox lstSuggestions = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Label lblUsername = null!;
        private Label lblSuggestions = null!;
        private Label lblTitle = null!;

        public string SelectedUsername => txtUsername.Text.Trim();

        public AddMemberDialog()
        {
            InitializeComponent();
            SetupControls();
            ApplyModernStyling();
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblSuggestions = new Label();
            lstSuggestions = new ListBox();
            btnOK = new Button();
            btnCancel = new Button();
            SuspendLayout();

            // lblTitle
            lblTitle.Text = "➕ Thêm Thành Viên Mới";
            lblTitle.Location = new Point(20, 15);
            lblTitle.Size = new Size(360, 30);

            // lblUsername
            lblUsername.Text = "Tên người dùng:";
            lblUsername.Location = new Point(20, 55);
            lblUsername.Size = new Size(120, 25);

            // txtUsername
            txtUsername.Location = new Point(20, 80);
            txtUsername.Size = new Size(360, 30);

            // lblSuggestions  
            lblSuggestions.Text = "Gợi ý (click để chọn):";
            lblSuggestions.Location = new Point(20, 120);
            lblSuggestions.Size = new Size(200, 25);

            // lstSuggestions
            lstSuggestions.Location = new Point(20, 145);
            lstSuggestions.Size = new Size(360, 150);

            // btnOK
            btnOK.Text = "✅ Thêm";
            btnOK.Location = new Point(180, 310);
            btnOK.Size = new Size(100, 35);

            // btnCancel
            btnCancel.Text = "❌ Hủy";
            btnCancel.Location = new Point(290, 310);
            btnCancel.Size = new Size(90, 35);

            // AddMemberDialog
            Controls.AddRange(new Control[] { lblTitle, lblUsername, txtUsername, lblSuggestions, lstSuggestions, btnOK, btnCancel });
            ClientSize = new Size(400, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Thêm thành viên";
            ResumeLayout(false);
        }

        private void ApplyModernStyling()
        {
            BackColor = Color.FromArgb(245, 246, 250);

            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(28, 30, 33);

            lblUsername.Font = new Font("Segoe UI", 9F);
            lblSuggestions.Font = new Font("Segoe UI", 9F);

            txtUsername.Font = new Font("Segoe UI", 10F);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;

            lstSuggestions.Font = new Font("Segoe UI", 9F);
            lstSuggestions.BorderStyle = BorderStyle.FixedSingle;

            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.BackColor = Color.FromArgb(40, 167, 69);
            btnOK.ForeColor = Color.White;
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Cursor = Cursors.Hand;

            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.FromArgb(108, 117, 125);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Cursor = Cursors.Hand;
        }

        private void SetupControls()
        {
            txtUsername.TextChanged += (s, e) => FilterSuggestions(txtUsername.Text);

            lstSuggestions.Click += (s, e) =>
            {
                if (lstSuggestions.SelectedItem != null)
                    txtUsername.Text = lstSuggestions.SelectedItem.ToString();
            };

            lstSuggestions.DoubleClick += (s, e) =>
            {
                if (lstSuggestions.SelectedItem != null)
                {
                    txtUsername.Text = lstSuggestions.SelectedItem.ToString();
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên người dùng!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            // Load sample users
            lstSuggestions.Items.AddRange(new object[]
            {
                "nguoidung1", "nguoidung2", "nguoidung3", "nguoidung4", "nguoidung5", "quantrivien1", "admin"
            });
        }

        private void FilterSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            for (int i = 0; i < lstSuggestions.Items.Count; i++)
            {
                var item = lstSuggestions.Items[i]?.ToString();
                if (item != null && item.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    lstSuggestions.SelectedIndex = i;
                    break;
                }
            }
        }
    }
}

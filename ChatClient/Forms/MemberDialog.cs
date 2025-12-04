using System;
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

        public MembersDialog(SocketClientService socketClient, User currentUser, string conversationId)
        {
            _socketClient = socketClient;
            _currentUser = currentUser;
            _conversationId = conversationId;

            InitializeComponent();
            SetupControls();
        }

        private void SetupControls()
        {
            btnAddMember.Click += async (s, e) => await AddMemberAsync();
            btnRemoveMember.Click += async (s, e) => await RemoveMemberAsync();
            btnBanMember.Click += async (s, e) => await BanMemberAsync();
            btnUnbanMember.Click += async (s, e) => await UnbanMemberAsync();
            btnMuteMember.Click += async (s, e) => await MuteMemberAsync();
            btnUnmuteMember.Click += async (s, e) => await UnmuteMemberAsync();
            btnPromote.Click += async (s, e) => await PromoteMemberAsync();
            btnClose.Click += (s, e) => Close();

            lstMembers.SelectedIndexChanged += (s, e) =>
            {
                var hasSelection = lstMembers.SelectedItems.Count > 0;
                btnRemoveMember.Enabled = hasSelection;
                btnBanMember.Enabled = hasSelection;
                btnUnbanMember.Enabled = hasSelection;
                btnMuteMember.Enabled = hasSelection;
                btnUnmuteMember.Enabled = hasSelection;
                btnPromote.Enabled = hasSelection;
            };
        }

        public async Task LoadMembersAsync()
        {
            try
            {
                lblStatus.Text = "Đang tải danh sách thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.GetConversationMembersAsync(_currentUser, _conversationId);

                if (response == null || !response.Success)
                {
                    lblStatus.Text = response?.Message ?? "Lỗi tải danh sách thành viên.";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                    return;
                }

                lstMembers.Items.Clear();
                foreach (var member in response.Members)
                {
                    var item = new ListViewItem(member.Username);
                    item.SubItems.Add(member.Role);
                    item.SubItems.Add(member.IsBanned ? "Đã chặn" : "Bình thường");
                    item.SubItems.Add(member.IsMuted ? "Đã tắt tiếng" : "Bình thường");
                    item.SubItems.Add(member.JoinedDate.ToString("dd/MM/yyyy HH:mm"));
                    item.Tag = member.Username;
                    lstMembers.Items.Add(item);
                }

                lblStatus.Text = $"Đã tải {response.Members.Length} thành viên.";
                lblStatus.ForeColor = System.Drawing.Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        private async Task AddMemberAsync()
        {
            var username = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập tên người dùng cần thêm:", "Thêm thành viên", "");

            if (string.IsNullOrWhiteSpace(username)) return;

            try
            {
                btnAddMember.Enabled = false;
                lblStatus.Text = "Đang thêm thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.AddMemberToConversationAsync(
                    _currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi thêm thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã thêm thành viên thành công!", "Thành công",
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

            var result = MessageBox.Show($"Bạn có chắc muốn xóa {username} khỏi cuộc trò chuyện?",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                lblStatus.Text = "Đang xóa thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.RemoveMemberFromConversationAsync(
                    _currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã xóa thành viên thành công!", "Thành công",
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

            try
            {
                lblStatus.Text = "Đang chặn thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.BanMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi chặn thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã chặn thành viên thành công!", "Thành công",
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

            try
            {
                lblStatus.Text = "Đang bỏ chặn thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.UnbanMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi bỏ chặn thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã bỏ chặn thành viên thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task MuteMemberAsync()
        {
            if (lstMembers.SelectedItems.Count == 0) return;

            var username = lstMembers.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(username)) return;

            try
            {
                lblStatus.Text = "Đang tắt tiếng thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.MuteMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tắt tiếng thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã tắt tiếng thành viên thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UnmuteMemberAsync()
        {
            if (lstMembers.SelectedItems.Count == 0) return;

            var username = lstMembers.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(username)) return;

            try
            {
                lblStatus.Text = "Đang bỏ tắt tiếng thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.UnmuteMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi bỏ tắt tiếng thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã bỏ tắt tiếng thành viên thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PromoteMemberAsync()
        {
            if (lstMembers.SelectedItems.Count == 0) return;

            var username = lstMembers.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(username)) return;

            try
            {
                lblStatus.Text = "Đang thăng cấp thành viên...";
                lblStatus.ForeColor = System.Drawing.Color.Blue;

                var response = await _socketClient.PromoteMemberAsync(_currentUser, _conversationId, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi thăng cấp thành viên.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã thăng cấp thành viên thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadMembersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Designer Code
        private System.ComponentModel.IContainer components = null;
        private ListView lstMembers;
        private ColumnHeader colUsername;
        private ColumnHeader colRole;
        private ColumnHeader colBanStatus;
        private ColumnHeader colMuteStatus;
        private ColumnHeader colJoinedDate;
        private Button btnAddMember;
        private Button btnRemoveMember;
        private Button btnBanMember;
        private Button btnUnbanMember;
        private Button btnMuteMember;
        private Button btnUnmuteMember;
        private Button btnPromote;
        private Button btnClose;
        private Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstMembers = new ListView();
            this.colUsername = new ColumnHeader();
            this.colRole = new ColumnHeader();
            this.colBanStatus = new ColumnHeader();
            this.colMuteStatus = new ColumnHeader();
            this.colJoinedDate = new ColumnHeader();
            this.btnAddMember = new Button();
            this.btnRemoveMember = new Button();
            this.btnBanMember = new Button();
            this.btnUnbanMember = new Button();
            this.btnMuteMember = new Button();
            this.btnUnmuteMember = new Button();
            this.btnPromote = new Button();
            this.btnClose = new Button();
            this.lblStatus = new Label();
            this.SuspendLayout();

            // lstMembers
            this.lstMembers.Columns.AddRange(new ColumnHeader[] {
                this.colUsername, this.colRole, this.colBanStatus,
                this.colMuteStatus, this.colJoinedDate});
            this.lstMembers.FullRowSelect = true;
            this.lstMembers.GridLines = true;
            this.lstMembers.HideSelection = false;
            this.lstMembers.Location = new System.Drawing.Point(12, 12);
            this.lstMembers.MultiSelect = false;
            this.lstMembers.Name = "lstMembers";
            this.lstMembers.Size = new System.Drawing.Size(760, 400);
            this.lstMembers.TabIndex = 0;
            this.lstMembers.UseCompatibleStateImageBehavior = false;
            this.lstMembers.View = View.Details;

            // Columns
            this.colUsername.Text = "Người dùng";
            this.colUsername.Width = 150;
            this.colRole.Text = "Vai trò";
            this.colRole.Width = 120;
            this.colBanStatus.Text = "Trạng thái chặn";
            this.colBanStatus.Width = 120;
            this.colMuteStatus.Text = "Trạng thái tắt tiếng";
            this.colMuteStatus.Width = 140;
            this.colJoinedDate.Text = "Ngày tham gia";
            this.colJoinedDate.Width = 150;

            // Buttons
            int btnY = 420;
            this.btnAddMember.Location = new System.Drawing.Point(12, btnY);
            this.btnAddMember.Name = "btnAddMember";
            this.btnAddMember.Size = new System.Drawing.Size(100, 30);
            this.btnAddMember.Text = "➕ Thêm";
            this.btnAddMember.UseVisualStyleBackColor = true;

            this.btnRemoveMember.Enabled = false;
            this.btnRemoveMember.Location = new System.Drawing.Point(122, btnY);
            this.btnRemoveMember.Name = "btnRemoveMember";
            this.btnRemoveMember.Size = new System.Drawing.Size(100, 30);
            this.btnRemoveMember.Text = "❌ Xóa";
            this.btnRemoveMember.UseVisualStyleBackColor = true;

            this.btnBanMember.Enabled = false;
            this.btnBanMember.Location = new System.Drawing.Point(232, btnY);
            this.btnBanMember.Name = "btnBanMember";
            this.btnBanMember.Size = new System.Drawing.Size(100, 30);
            this.btnBanMember.Text = "🚫 Chặn";
            this.btnBanMember.UseVisualStyleBackColor = true;

            this.btnUnbanMember.Enabled = false;
            this.btnUnbanMember.Location = new System.Drawing.Point(342, btnY);
            this.btnUnbanMember.Name = "btnUnbanMember";
            this.btnUnbanMember.Size = new System.Drawing.Size(100, 30);
            this.btnUnbanMember.Text = "✅ Bỏ chặn";
            this.btnUnbanMember.UseVisualStyleBackColor = true;

            this.btnMuteMember.Enabled = false;
            this.btnMuteMember.Location = new System.Drawing.Point(452, btnY);
            this.btnMuteMember.Name = "btnMuteMember";
            this.btnMuteMember.Size = new System.Drawing.Size(100, 30);
            this.btnMuteMember.Text = "🔇 Tắt tiếng";
            this.btnMuteMember.UseVisualStyleBackColor = true;

            this.btnUnmuteMember.Enabled = false;
            this.btnUnmuteMember.Location = new System.Drawing.Point(562, btnY);
            this.btnUnmuteMember.Name = "btnUnmuteMember";
            this.btnUnmuteMember.Size = new System.Drawing.Size(100, 30);
            this.btnUnmuteMember.Text = "🔊 Bật tiếng";
            this.btnUnmuteMember.UseVisualStyleBackColor = true;

            this.btnPromote.Enabled = false;
            this.btnPromote.Location = new System.Drawing.Point(672, btnY);
            this.btnPromote.Name = "btnPromote";
            this.btnPromote.Size = new System.Drawing.Size(100, 30);
            this.btnPromote.Text = "⬆️ Thăng";
            this.btnPromote.UseVisualStyleBackColor = true;

            this.btnClose.Location = new System.Drawing.Point(672, 460);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 30);
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;

            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 468);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 15);
            this.lblStatus.TabIndex = 9;

            // MembersDialog
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 501);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnPromote);
            this.Controls.Add(this.btnUnmuteMember);
            this.Controls.Add(this.btnMuteMember);
            this.Controls.Add(this.btnUnbanMember);
            this.Controls.Add(this.btnBanMember);
            this.Controls.Add(this.btnRemoveMember);
            this.Controls.Add(this.btnAddMember);
            this.Controls.Add(this.lstMembers);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MembersDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Quản lý thành viên";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
}
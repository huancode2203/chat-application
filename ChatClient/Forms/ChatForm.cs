using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;
using Timer = System.Windows.Forms.Timer;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form chat chính với các tính năng nâng cao
    /// </summary>
    public partial class ChatForm : Form
    {
        private readonly User _currentUser;
        private readonly SocketClientService _socketClient;
        private Timer _autoRefreshTimer;
        private string _currentConversationId;

        public ChatForm(User currentUser)
        {
            _currentUser = currentUser;
            _socketClient = new SocketClientService("127.0.0.1", 9000);

            InitializeComponent();
            SetupEventHandlers();
            SetupAutoRefresh();
            Shown += async (_, _) => await ConnectToServerAsync();
        }

        private void SetupEventHandlers()
        {
            Text = $"Chat Nội Bộ - {_currentUser.Username} (Mức: {_currentUser.ClearanceLevel})";
            cbLabel.SelectedIndex = 0;

            // Button events
            btnSend.Click += async (_, _) => await SendMessageAsync();
            btnRefresh.Click += async (_, _) => await LoadMessagesAsync();
            btnCreateGroup.Click += async (_, _) => await CreateGroupAsync();
            btnPrivateChat.Click += async (_, _) => await CreatePrivateChatAsync();
            btnViewMembers.Click += async (_, _) => await ViewMembersAsync();
            btnAttachment.Click += async (_, _) => await SendAttachmentAsync();
            btnLogout.Click += (_, _) => HandleLogout();

            // Conversation list events
            lstConversations.SelectedIndexChanged += async (_, _) => await OnConversationSelected();

            // Enter key to send
            txtMessage.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter && !ModifierKeys.HasFlag(Keys.Shift))
                {
                    e.Handled = true;
                    btnSend.PerformClick();
                }
            };

            // Context menu for messages
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Xóa tin nhắn", null, async (s, e) => await DeleteMessageAsync());
            lstMessages.ContextMenuStrip = contextMenu;
        }

        private void SetupAutoRefresh()
        {
            _autoRefreshTimer = new Timer { Interval = 5000 }; // 5 seconds
            _autoRefreshTimer.Tick += async (_, _) => await LoadMessagesAsync();
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                await _socketClient.ConnectAsync();
                await LoadConversationsAsync();
                _autoRefreshTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối server: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                var response = await _socketClient.GetConversationsAsync(_currentUser);
                if (response == null || !response.Success)
                {
                    UpdateStatus("Lỗi tải danh sách cuộc trò chuyện.", true);
                    return;
                }

                lstConversations.Items.Clear();
                foreach (var conv in response.Conversations)
                {
                    var item = new ListViewItem(conv.ConversationName);
                    item.SubItems.Add(conv.MemberCount.ToString());
                    item.SubItems.Add(conv.IsPrivate ? "Riêng tư" : "Nhóm");
                    item.Tag = conv.ConversationId;
                    lstConversations.Items.Add(item);
                }

                UpdateStatus($"Đã tải {response.Conversations.Length} cuộc trò chuyện.", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}", true);
            }
        }

        private async Task OnConversationSelected()
        {
            if (lstConversations.SelectedItems.Count == 0) return;

            _currentConversationId = lstConversations.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(_currentConversationId)) return;

            await LoadMessagesAsync();
            txtReceiver.Text = _currentConversationId;
            grpChat.Enabled = true;
        }

        private async Task SendMessageAsync()
        {
            var content = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Vui lòng nhập nội dung tin nhắn.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var label = cbLabel.SelectedIndex + 1;

            try
            {
                btnSend.Enabled = false;
                var response = await _socketClient.SendMessageToConversationAsync(
                    _currentUser, _currentConversationId, content, label);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi gửi tin nhắn.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                txtMessage.Clear();
                await LoadMessagesAsync();
                UpdateStatus("Đã gửi tin nhắn.", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi tin: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }

        private async Task LoadMessagesAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId)) return;

            try
            {
                var response = await _socketClient.GetConversationMessagesAsync(
                    _currentUser, _currentConversationId);

                if (response == null || !response.Success)
                {
                    UpdateStatus(response?.Message ?? "Lỗi tải tin nhắn.", true);
                    return;
                }

                lstMessages.Items.Clear();
                foreach (var msg in response.Messages.OrderBy(m => m.Timestamp))
                {
                    var item = new ListViewItem(msg.Timestamp.ToString("HH:mm:ss"));
                    item.SubItems.Add(msg.Sender);
                    item.SubItems.Add(msg.Content);
                    item.SubItems.Add(msg.SecurityLabel.ToString());
                    item.Tag = msg.MessageId;
                    lstMessages.Items.Add(item);
                }

                // Scroll to bottom
                if (lstMessages.Items.Count > 0)
                {
                    lstMessages.Items[lstMessages.Items.Count - 1].EnsureVisible();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải tin nhắn: {ex.Message}", true);
            }
        }

        private async Task CreateGroupAsync()
        {
            using var dialog = new CreateGroupDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var response = await _socketClient.CreateGroupAsync(
                    _currentUser, dialog.GroupName, dialog.GroupType, dialog.Members.ToArray());

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tạo nhóm.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Tạo nhóm thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CreatePrivateChatAsync()
        {
            var username = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập tên người dùng để chat riêng:", "Chat riêng", "");

            if (string.IsNullOrWhiteSpace(username)) return;

            try
            {
                var response = await _socketClient.CreatePrivateChatAsync(
                    _currentUser, username);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tạo chat riêng.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Tạo chat riêng thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ViewMembersAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new MembersDialog(_socketClient, _currentUser, _currentConversationId);
            await dialog.LoadMembersAsync();
            dialog.ShowDialog();
        }

        private async Task SendAttachmentAsync()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*",
                Title = "Chọn file đính kèm"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                btnAttachment.Enabled = false;
                UpdateStatus("Đang tải file lên...", false);

                var response = await _socketClient.UploadAttachmentAsync(
                    _currentUser, openFileDialog.FileName);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tải file.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Send message with attachment
                var label = cbLabel.SelectedIndex + 1;
                var msgResponse = await _socketClient.SendMessageWithAttachmentAsync(
                    _currentUser, _currentConversationId,
                    $"[File: {System.IO.Path.GetFileName(openFileDialog.FileName)}]",
                    label, response.AttachmentId);

                if (msgResponse == null || !msgResponse.Success)
                {
                    MessageBox.Show(msgResponse?.Message ?? "Lỗi gửi tin nhắn.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus("Đã gửi file thành công.", false);
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAttachment.Enabled = true;
            }
        }

        private async Task DeleteMessageAsync()
        {
            if (lstMessages.SelectedItems.Count == 0) return;

            var messageId = lstMessages.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(messageId)) return;

            var result = MessageBox.Show("Bạn có chắc muốn xóa tin nhắn này?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await _socketClient.DeleteMessageAsync(_currentUser, messageId);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa tin nhắn.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                UpdateStatus("Đã xóa tin nhắn.", false);
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleLogout()
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _autoRefreshTimer?.Stop();
                Close();
            }
        }

        private void UpdateStatus(string message, bool isError)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = isError ? System.Drawing.Color.Red : System.Drawing.Color.Green;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer?.Dispose();
            _socketClient.Dispose();
        }
    }
}
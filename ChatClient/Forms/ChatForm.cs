using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form chat chính với các tính năng nâng cao
    /// </summary>
    public partial class ChatForm : Form
    {
        private readonly User _currentUser;
        private readonly SocketClientService _socketClient;
        private readonly EncryptionService _encryptionService;
        private WinFormsTimer? _autoRefreshTimer;
        private string? _currentConversationId;
        private bool _currentConversationIsPrivate = false;
        private bool _autoRefreshEnabled = true;
        private bool _isLoadingMessages = false;
        private bool _autoImagePreviewEnabled = true;
        
        // Message tracking
        private ChatMessageDto[] _currentMessages = [];
        private readonly Dictionary<int, int> _messageHeights = [];
        private readonly Dictionary<int, Image> _inlineImageCache = [];
        private readonly object _inlineImageLock = new();
        private readonly Dictionary<string, string> _conversationNames = [];
        
        // Search
        private TextBox? _searchTextBox;
        private int _lastSearchIndex = -1;
        private string _lastSearchTerm = string.Empty;
        
        // Profile menu
        private ToolStripMenuItem? _profileMenuItem;

        public ChatForm(User currentUser)
        {
            _currentUser = currentUser;
            _socketClient = new SocketClientService("127.0.0.1", 9000);
            _encryptionService = new EncryptionService();

            InitializeComponent();
            SetupEventHandlers();
            SetupProfileMenu();
            SetupMessageListDrawing();
            SetupAutoRefresh();
            InitializeSecurityLabels();
            AddModernFeatures();
            
            Shown += async (_, _) => await ConnectToServerAsync();
        }

        private void InitializeSecurityLabels()
        {
            // Giới hạn security labels theo clearance level của user
            if (cbLabel != null)
            {
                cbLabel.Items.Clear();
                for (int i = 1; i <= Math.Min(_currentUser.ClearanceLevel, 5); i++)
                {
                    var labelName = i switch
                    {
                        1 => "1 - Công khai",
                        2 => "2 - Nội bộ", 
                        3 => "3 - Bí mật",
                        4 => "4 - Tối mật",
                        5 => "5 - Tuyệt mật",
                        _ => $"{i}"
                    };
                    cbLabel.Items.Add(labelName);
                }
                if (cbLabel.Items.Count > 0)
                    cbLabel.SelectedIndex = 0;
            }
        }

        private void SetupEventHandlers()
        {
            Text = $"Chat Nội Bộ - {_currentUser.Username} (Mức bảo mật: {_currentUser.ClearanceLevel})";
            if (cbLabel?.Items.Count > 0) cbLabel.SelectedIndex = 0;

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
            lstConversations.MouseClick += LstConversations_MouseClick;

            // Enter key to send
            txtMessage.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter && !ModifierKeys.HasFlag(Keys.Shift))
                {
                    e.Handled = true;
                    btnSend.PerformClick();
                }
            };

            // Context menu for messages - will be created dynamically
            lstMessages.MouseClick += LstMessages_MouseClick;
        }

        private void LstMessages_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || lstMessages.SelectedItems.Count == 0)
                return;

            var item = lstMessages.SelectedItems[0];
            var isAttachment = IsAttachmentMessage(item);

            var contextMenu = new ContextMenuStrip();

            // Copy message
            var copyItem = new ToolStripMenuItem("📋 Sao chép nội dung");
            copyItem.Click += (s, args) => CopyMessageContent();
            contextMenu.Items.Add(copyItem);

            if (isAttachment)
            {
                contextMenu.Items.Add(new ToolStripSeparator());

                // Preview attachment (for images)
                var content = item.SubItems.Count > 2 ? item.SubItems[2].Text : string.Empty;
                if (TryParseAttachmentFileName(content, out var fileName) && HasImageExtension(fileName))
                {
                    var previewItem = new ToolStripMenuItem("🖼️ Xem trước ảnh");
                    previewItem.Click += async (s, args) => await PreviewAttachmentAsync();
                    contextMenu.Items.Add(previewItem);
                }

                // Download attachment
                var downloadItem = new ToolStripMenuItem("💾 Tải xuống file");
                downloadItem.Click += async (s, args) => await DownloadAttachmentAsync();
                contextMenu.Items.Add(downloadItem);
            }

            // Delete message
            contextMenu.Items.Add(new ToolStripSeparator());
            var deleteItem = new ToolStripMenuItem("🗑️ Xóa tin nhắn");
            deleteItem.Click += async (s, args) => await DeleteMessageAsync();
            contextMenu.Items.Add(deleteItem);

            contextMenu.Show(lstMessages, e.Location);
        }

        private void CopyMessageContent()
        {
            if (lstMessages.SelectedItems.Count == 0) return;
            var item = lstMessages.SelectedItems[0];
            if (item.SubItems.Count > 2)
            {
                var content = item.SubItems[2].Text;
                Clipboard.SetText(content);
                UpdateStatus("Đã sao chép nội dung.", false);
            }
        }

        private void LstConversations_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || lstConversations.SelectedItems.Count == 0)
                return;

            var selectedItem = lstConversations.SelectedItems[0];
            var isPrivate = selectedItem.SubItems.Count > 2 && selectedItem.SubItems[2].Text.Contains("Riêng tư");
            var isArchived = selectedItem.SubItems.Count > 2 && selectedItem.SubItems[2].Text.Contains("Archive");

            var contextMenu = new ContextMenuStrip();

            // View members
            var viewMembersItem = new ToolStripMenuItem("👥 Xem thành viên");
            viewMembersItem.Click += async (s, args) => await ViewMembersAsync();
            contextMenu.Items.Add(viewMembersItem);

            // View details
            var viewDetailsItem = new ToolStripMenuItem("📊 Chi tiết cuộc trò chuyện");
            viewDetailsItem.Click += async (s, args) => await ViewConversationDetailsAsync();
            contextMenu.Items.Add(viewDetailsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            if (isArchived)
            {
                // Archived group - only delete archive option
                var deleteArchiveItem = new ToolStripMenuItem("🗑️ Xóa khỏi danh sách");
                deleteArchiveItem.Click += async (s, args) => await DeleteArchiveAsync();
                contextMenu.Items.Add(deleteArchiveItem);
            }
            else if (isPrivate)
            {
                // Private chat - one-sided delete
                var deletePrivateItem = new ToolStripMenuItem("�️ Xóa cuộc trò chuyện");
                deletePrivateItem.Click += async (s, args) => await DeletePrivateChatOneSideAsync();
                contextMenu.Items.Add(deletePrivateItem);
            }
            else
            {
                // Group - leave or delete (for owner)
                var leaveItem = new ToolStripMenuItem("🚪 Rời nhóm");
                leaveItem.Click += async (s, args) => await LeaveGroupAsync();
                contextMenu.Items.Add(leaveItem);

                var deleteGroupItem = new ToolStripMenuItem("🗑️ Xóa nhóm (Chủ nhóm)");
                deleteGroupItem.Click += async (s, args) => await DeleteGroupAsync();
                contextMenu.Items.Add(deleteGroupItem);
            }

            contextMenu.Show(lstConversations, e.Location);
        }

        private async Task LeaveConversationAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = _currentConversationIsPrivate
                ? "Bạn có chắc muốn rời cuộc trò chuyện này?\nBạn sẽ không thể xem tin nhắn cũ."
                : "Bạn có chắc muốn rời nhóm này?\nBạn sẽ không thể xem tin nhắn cũ.";

            var result = MessageBox.Show(message, "Xác nhận rời",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await _socketClient.LeaveConversationAsync(_currentUser, _currentConversationId);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi rời cuộc trò chuyện.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã rời cuộc trò chuyện.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _currentConversationId = null;
                lstMessages.Items.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteGroupAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa nhóm này?\n\n" +
                "⚠️ Tất cả thành viên sẽ không thể gửi tin nhắn mới.\n" +
                "⚠️ Nhóm sẽ được chuyển vào Archive để xem lại tin nhắn cũ.",
                "Xác nhận xóa nhóm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await _socketClient.DeleteGroupAsync(_currentUser, _currentConversationId);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa nhóm. Bạn có thể không phải chủ nhóm.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã xóa nhóm. Nhóm đã được chuyển vào Archive.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _currentConversationId = null;
                lstMessages.Items.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LeaveGroupAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn nhóm.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "Bạn có chắc muốn rời nhóm này?\n\n" +
                "⚠️ Bạn sẽ không thể xem tin nhắn cũ.\n" +
                "⚠️ Bạn có thể được thêm lại bởi admin.",
                "Xác nhận rời nhóm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await _socketClient.LeaveGroupAsync(_currentUser, _currentConversationId);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi rời nhóm.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã rời nhóm thành công.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _currentConversationId = null;
                lstMessages.Items.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeletePrivateChatOneSideAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa cuộc trò chuyện này?\n\n" +
                "⚠️ Người còn lại vẫn có thể xem tin nhắn.\n" +
                "⚠️ Khi họ gửi tin nhắn mới, bạn sẽ nhận được nhưng không thấy tin cũ.",
                "Xác nhận xóa cuộc trò chuyện",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await _socketClient.DeletePrivateChatOneSideAsync(_currentUser, _currentConversationId);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa cuộc trò chuyện.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã xóa cuộc trò chuyện.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _currentConversationId = null;
                lstMessages.Items.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteArchiveAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn nhóm archive.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "Bạn có chắc muốn xóa nhóm này khỏi danh sách?\n\n" +
                "⚠️ Bạn sẽ không thể xem lại tin nhắn cũ.",
                "Xác nhận xóa archive",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                var response = await _socketClient.DeleteArchiveAsync(_currentUser, _currentConversationId);

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa archive.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã xóa archive thành công.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _currentConversationId = null;
                lstMessages.Items.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupAutoRefresh()
        {
            _autoRefreshTimer = new WinFormsTimer { Interval = 5000 }; // 5 seconds
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

            var selectedItem = lstConversations.SelectedItems[0];
            _currentConversationId = selectedItem.Tag?.ToString();
            if (string.IsNullOrEmpty(_currentConversationId)) return;

            // Check if private conversation (column 3 shows type)
            _currentConversationIsPrivate = selectedItem.SubItems.Count > 2 && 
                selectedItem.SubItems[2].Text.Contains("Riêng tư");

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

                // Sử dụng BeginUpdate/EndUpdate để tránh flicker
                lstMessages.BeginUpdate();
                try
                {
                    lstMessages.Items.Clear();
                    _currentMessages = response.Messages.OrderBy(m => m.Timestamp).ToArray();
                    
                    foreach (var msg in _currentMessages)
                    {
                        var isMine = string.Equals(msg.Sender, _currentUser.Matk, StringComparison.OrdinalIgnoreCase);
                        var senderDisplay = isMine ? "Bạn" : msg.Sender;
                        
                        var item = new ListViewItem(msg.Timestamp.ToString("HH:mm"));
                        item.SubItems.Add(senderDisplay);
                        item.SubItems.Add(msg.Content ?? string.Empty);
                        item.SubItems.Add($"Mức {msg.SecurityLabel}");
                        item.Tag = msg.MessageId;
                        
                        // Height tự động điều chỉnh dựa trên có ảnh hay không
                        bool hasImage = false;
                        if (TryParseAttachmentFileName(msg.Content ?? "", out var fileName) && HasImageExtension(fileName))
                        {
                            hasImage = true;
                            // Preload image asynchronously
                            _ = PreloadInlineImageAsync(msg.MessageId);
                        }
                        
                        _messageHeights[msg.MessageId] = hasImage ? 230 : 55;
                        lstMessages.Items.Add(item);
                    }
                }
                finally
                {
                    lstMessages.EndUpdate();
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

            using var dialog = new MembersDialog(_socketClient, _currentUser, _currentConversationId, _currentConversationIsPrivate);
            await dialog.LoadMembersAsync();
            dialog.ShowDialog();
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

        private static void AddMessageToList(ListView lstMessages, ChatMessageDto msg, string currentUserMatk, Dictionary<int, int> messageHeights)
        {
            if (lstMessages == null) return;

            var isMine = string.Equals(msg.Sender, currentUserMatk, StringComparison.OrdinalIgnoreCase);
            var senderDisplay = isMine ? "Bạn" : msg.Sender;

            var item = new ListViewItem(msg.Timestamp.ToString("HH:mm"));
            item.SubItems.Add(senderDisplay);
            item.SubItems.Add(msg.Content ?? string.Empty);
            item.SubItems.Add($"Mức {msg.SecurityLabel}");
            item.Tag = msg.MessageId;

            // Set colors
            if (isMine)
            {
                item.BackColor = Color.FromArgb(220, 248, 198);
                item.ForeColor = Color.Black;
            }

            messageHeights[msg.MessageId] = 30;
            lstMessages.Items.Add(item);
        }

        private static string GetFileIcon(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".pdf" => "📄",
                ".doc" or ".docx" => "📝",
                ".xls" or ".xlsx" => "📊",
                ".zip" or ".rar" => "📦",
                ".mp4" or ".avi" or ".mkv" => "🎬",
                _ => "📎"
            };
        }

        private static bool HasImageExtension(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
        }

        private static bool TryParseAttachmentFileName(string content, out string fileName)
        {
            fileName = string.Empty;
            if (string.IsNullOrEmpty(content)) return false;

            var start = content.IndexOf("[File:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return false;

            var end = content.IndexOf(']', start + 1);
            if (end <= start) return false;

            fileName = content.Substring(start + 6, end - start - 6).Trim();
            return fileName.Length > 0;
        }

        private async Task PreloadInlineImageAsync(int messageId)
        {
            try
            {
                lock (_inlineImageLock)
                {
                    if (_inlineImageCache.ContainsKey(messageId))
                        return;
                }

                var response = await _socketClient.DownloadAttachmentAsync(_currentUser, messageId);

                if (response?.Success == true && !string.IsNullOrEmpty(response.AttachmentContentBase64))
                {
                    var bytes = Convert.FromBase64String(response.AttachmentContentBase64);
                    using var ms = new System.IO.MemoryStream(bytes);
                    var image = Image.FromStream(ms);

                    lock (_inlineImageLock)
                    {
                        _inlineImageCache[messageId] = image;
                    }

                    lstMessages?.Invalidate();
                }
            }
            catch
            {
                // Silently fail
            }
        }

        private void SetupProfileMenu()
        {
            try
            {
                if (toolStrip1 == null) return;

                var profileButton = new ToolStripDropDownButton($"👤 {_currentUser.Username}")
                {
                    Alignment = ToolStripItemAlignment.Right
                };

                _profileMenuItem = new ToolStripMenuItem("📋 Thông tin của tôi");
                _profileMenuItem.Click += (_, _) => OpenMyProfile();

                var settingsMenuItem = new ToolStripMenuItem("⚙️ Cài đặt");
                settingsMenuItem.Click += (_, _) => OpenSettings();
                
                var detailsMenuItem = new ToolStripMenuItem("📊 Chi tiết cuộc trò chuyện");
                detailsMenuItem.Click += async (_, _) => await ViewConversationDetailsAsync();

                profileButton.DropDownItems.Add(_profileMenuItem);
                profileButton.DropDownItems.Add(detailsMenuItem);
                profileButton.DropDownItems.Add(new ToolStripSeparator());
                profileButton.DropDownItems.Add(settingsMenuItem);

                toolStrip1.Items.Add(profileButton);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetupProfileMenu error: {ex.Message}");
            }
        }

        private void OpenMyProfile()
        {
            try
            {
                using var profileForm = new UserProfileForm(_socketClient, _currentUser, null);
                profileForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở thông tin: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenSettings()
        {
            using var settingsForm = new Form
            {
                Text = "⚙️ Cài đặt",
                Size = new Size(550, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };

            var lblTitle = new Label
            {
                Text = "Cài đặt ứng dụng",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            // Group: Giao diện
            var grpAppearance = new GroupBox
            {
                Text = "🎨 Giao diện",
                Location = new Point(20, 60),
                Size = new Size(490, 100),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            
            var chkDarkMode = new CheckBox
            {
                Text = "Chế độ tối (Dark Mode)",
                Location = new Point(20, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                Checked = _isDarkMode
            };
            
            var chkAutoPreview = new CheckBox
            {
                Text = "Tự động xem trước ảnh trong tin nhắn",
                Location = new Point(20, 65),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                Checked = _autoImagePreviewEnabled
            };
            
            grpAppearance.Controls.AddRange(new Control[] { chkDarkMode, chkAutoPreview });

            // Group: Mã hóa
            var grpEncryption = new GroupBox
            {
                Text = "🔐 Mã hóa",
                Location = new Point(20, 170),
                Size = new Size(490, 130),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            
            var lblEncInfo = new Label
            {
                Text = "Ứng dụng hỗ trợ 3 phương thức mã hóa:\n\n" +
                       "• AES-256: Mã hóa đối xứng - nhanh, dùng cho nội dung tin nhắn\n" +
                       "• RSA-2048: Mã hóa bất đối xứng - an toàn, dùng cho trao đổi khóa\n" +
                       "• Hybrid (AES+RSA): Kết hợp cả hai - vừa nhanh vừa an toàn",
                Location = new Point(20, 30),
                Size = new Size(450, 85),
                Font = new Font("Segoe UI", 9F)
            };
            
            grpEncryption.Controls.Add(lblEncInfo);

            // Group: Thông báo
            var grpNotification = new GroupBox
            {
                Text = "🔔 Thông báo",
                Location = new Point(20, 310),
                Size = new Size(490, 80),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            
            var chkAutoRefresh = new CheckBox
            {
                Text = "Tự động làm mới tin nhắn",
                Location = new Point(20, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                Checked = _autoRefreshEnabled
            };
            
            grpNotification.Controls.Add(chkAutoRefresh);

            // Buttons
            var btnSave = new Button
            {
                Text = "💾 Lưu",
                Size = new Size(120, 40),
                Location = new Point(270, 405),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(100, 40),
                Location = new Point(400, 405),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnSave.Click += (s, e) =>
            {
                _autoImagePreviewEnabled = chkAutoPreview.Checked;
                _autoRefreshEnabled = chkAutoRefresh.Checked;
                
                if (chkDarkMode.Checked != _isDarkMode)
                {
                    ApplyDarkMode(chkDarkMode.Checked);
                }
                
                if (_autoRefreshEnabled)
                    _autoRefreshTimer?.Start();
                else
                    _autoRefreshTimer?.Stop();
                
                MessageBox.Show("Đã lưu cài đặt!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                settingsForm.DialogResult = DialogResult.OK;
            };

            settingsForm.Controls.AddRange(new Control[] { lblTitle, grpAppearance, grpEncryption, grpNotification, btnSave, btnCancel });
            settingsForm.ShowDialog(this);
        }

        private async Task ViewConversationDetailsAsync()
        {
            if (lstConversations == null || lstConversations.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedItem = lstConversations.SelectedItems[0];
            var conversationId = selectedItem.Tag?.ToString() ?? string.Empty;
            var conversationName = selectedItem.Text;
            var memberCount = selectedItem.SubItems.Count > 1 ? selectedItem.SubItems[1].Text : "0";
            var type = selectedItem.SubItems.Count > 2 ? selectedItem.SubItems[2].Text : "";

            using var detailsForm = new Form
            {
                Text = $"Chi tiết: {conversationName}",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };

            var lblTitle = new Label
            {
                Text = conversationName,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 120, 215)
            };

            var lblType = new Label { Text = $"📁 Loại: {type}", Location = new Point(20, 70), AutoSize = true };
            var lblMembers = new Label { Text = $"👥 Số thành viên: {memberCount}", Location = new Point(20, 100), AutoSize = true };
            var lblId = new Label { Text = $"🔑 ID: {conversationId}", Location = new Point(20, 130), AutoSize = true };
            var lblMessages = new Label { Text = $"💬 Tin nhắn: {lstMessages?.Items.Count ?? 0}", Location = new Point(20, 160), AutoSize = true };

            var btnViewMembers = new Button
            {
                Text = "👥 Xem danh sách thành viên",
                Size = new Size(200, 40),
                Location = new Point(20, 210),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnViewMembers.FlatAppearance.BorderSize = 0;
            btnViewMembers.Click += async (s, e) =>
            {
                await ViewMembersAsync();
            };

            var btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 40),
                Location = new Point(370, 310),
                DialogResult = DialogResult.OK
            };

            detailsForm.Controls.AddRange(new Control[] { lblTitle, lblType, lblMembers, lblId, lblMessages, btnViewMembers, btnClose });
            detailsForm.ShowDialog(this);
        }

        private void PerformSearch()
        {
            if (_searchTextBox == null || lstMessages == null) return;
            
            var searchText = _searchTextBox.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(searchText)) return;
            
            var startIndex = searchText == _lastSearchTerm ? _lastSearchIndex + 1 : 0;
            _lastSearchTerm = searchText;
            
            for (int i = startIndex; i < lstMessages.Items.Count; i++)
            {
                var item = lstMessages.Items[i];
                if (item.SubItems[2].Text.ToLowerInvariant().Contains(searchText))
                {
                    lstMessages.SelectedItems.Clear();
                    item.Selected = true;
                    item.EnsureVisible();
                    _lastSearchIndex = i;
                    return;
                }
            }
            
            _lastSearchIndex = -1;
            MessageBox.Show("Không tìm thấy.", "Tìm kiếm", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ResetSearchState()
        {
            _lastSearchIndex = -1;
        }

        private static bool IsAttachmentMessage(ListViewItem item)
        {
            if (item?.SubItems.Count < 3) return false;
            var content = item.SubItems[2].Text;
            return content.Contains("[File:", StringComparison.OrdinalIgnoreCase);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer?.Dispose();
            _socketClient.Dispose();
            _encryptionService.Dispose();
        }
    }
}
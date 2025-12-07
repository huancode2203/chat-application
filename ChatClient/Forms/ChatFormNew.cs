using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Controls;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Forms
{
    public partial class ChatFormNew : Form
    {
        private readonly User _currentUser;
        private readonly SocketClientService _socketClient;
        private string? _currentConversationId;
        private string? _currentConversationName;
        private int? _replyToMessageId;
        private string? _replyToContent;
        private ChatMessageDto[] _messages = Array.Empty<ChatMessageDto>();
        private readonly FlowLayoutPanel _messageContainer;

        public ChatFormNew(User currentUser)
        {
            _currentUser = currentUser;
            _socketClient = new SocketClientService("127.0.0.1", 9000);

            InitializeComponent();

            // Create message container - fill entire panel
            _messageContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            pnlMessages.Controls.Add(_messageContainer);
            pnlMessages.Resize += (s, e) => RefreshMessageLayout();

            // Add context menu to chat title for conversation details
            lblChatTitle.Cursor = Cursors.Hand;
            lblChatTitle.Click += async (s, e) => await ViewConversationDetailsAsync();

            SetupForm();
            SetupEvents();
        }

        private void SetupForm()
        {
            Text = $"💬 Chat - {_currentUser.Username} (Mức: {_currentUser.ClearanceLevel})";
            pnlInput.Enabled = false;

            // Security labels
            cbSecurityLabel.Items.Clear();
            for (int i = 1; i <= Math.Min(_currentUser.ClearanceLevel, 5); i++)
            {
                cbSecurityLabel.Items.Add($"Mức {i}");
            }
            if (cbSecurityLabel.Items.Count > 0) cbSecurityLabel.SelectedIndex = 0;
        }

        private void SetupEvents()
        {
            Shown += async (s, e) => await ConnectAsync();
            FormClosing += (s, e) => _socketClient?.Dispose();

            lstConversations.SelectedIndexChanged += async (s, e) => await OnConversationSelected();
            lstConversations.MouseClick += LstConversations_MouseClick;
            
            btnCreateGroup.Click += async (s, e) => await CreateGroupAsync();
            btnPrivateChat.Click += async (s, e) => await CreatePrivateChatAsync();
            btnViewMembers.Click += async (s, e) => await ViewMembersAsync();
            btnRefresh.Click += async (s, e) => await RefreshAsync();
            btnProfile.Click += (s, e) => ShowUserProfile();
            btnLogout.Click += BtnLogout_Click;
            
            btnSend.Click += async (s, e) => await SendMessageAsync();
            btnAttachment.Click += async (s, e) => await SendAttachmentAsync();
            btnCancelReply.Click += (s, e) => CancelReply();

            txtMessage.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    await SendMessageAsync();
                }
            };
        }

        private async Task ConnectAsync()
        {
            try
            {
                UpdateStatus("Đang kết nối...");
                await _socketClient.ConnectAsync();
                await LoadConversationsAsync();
                UpdateStatus("✓ Đã kết nối", false);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}", true);
                MessageBox.Show($"Không thể kết nối server:\n{ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadConversationsAsync()
        {
            var response = await _socketClient.GetConversationsAsync(_currentUser);
            if (response?.Success != true) return;

            lstConversations.BeginUpdate();
            lstConversations.Items.Clear();
            foreach (var conv in response.Conversations)
            {
                var item = new ListViewItem(conv.ConversationName);
                item.SubItems.Add(conv.MemberCount.ToString());
                item.SubItems.Add(conv.IsPrivate ? "🔒" : "👥");
                item.Tag = conv;
                lstConversations.Items.Add(item);
            }
            lstConversations.EndUpdate();
        }

        private async Task OnConversationSelected()
        {
            if (lstConversations.SelectedItems.Count == 0) return;

            var conv = lstConversations.SelectedItems[0].Tag as ConversationDto;
            if (conv == null) return;

            _currentConversationId = conv.ConversationId;
            _currentConversationName = conv.ConversationName;
            lblChatTitle.Text = $"💬 {conv.ConversationName}";
            
            CancelReply();
            await LoadMessagesAsync();
            pnlInput.Enabled = true;
            txtMessage.Focus();
        }

        private async Task LoadMessagesAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId)) return;

            var response = await _socketClient.GetConversationMessagesAsync(_currentUser, _currentConversationId);
            if (response?.Success != true) return;

            _messages = response.Messages.OrderBy(m => m.Timestamp).ToArray();
            RenderMessages();
        }

        private void RenderMessages()
        {
            _messageContainer.SuspendLayout();
            _messageContainer.Controls.Clear();

            foreach (var msg in _messages)
            {
                var bubble = CreateMessageBubble(msg);
                _messageContainer.Controls.Add(bubble);
            }

            _messageContainer.ResumeLayout();
            ScrollToBottom();
        }

        private void RefreshMessageLayout()
        {
            foreach (Control container in _messageContainer.Controls)
            {
                container.Width = pnlMessages.ClientSize.Width - 25;
                foreach (Control ctrl in container.Controls)
                {
                    if (ctrl is Panel bubble && ctrl.Tag is bool isMine)
                    {
                        if (isMine)
                            bubble.Location = new Point(container.Width - bubble.Width - 5, bubble.Location.Y);
                        else
                            bubble.Location = new Point(45, bubble.Location.Y);
                    }
                }
            }
        }

        private void ScrollToBottom()
        {
            pnlMessages.VerticalScroll.Value = pnlMessages.VerticalScroll.Maximum;
            pnlMessages.PerformLayout();
            pnlMessages.Invalidate();
        }

        private Panel CreateMessageBubble(ChatMessageDto msg)
        {
            var isMine = string.Equals(msg.Sender, _currentUser.Matk, StringComparison.OrdinalIgnoreCase);
            var content = msg.Content ?? "";
            var isImage = IsImageFile(content);
            int containerWidth = pnlMessages.ClientSize.Width - 25;

            // Parse reply reference
            string? replyContent = null;
            if (content.StartsWith("[Reply:"))
            {
                var endIdx = content.IndexOf(']');
                if (endIdx > 0)
                {
                    var replyIdStr = content.Substring(7, endIdx - 7);
                    if (int.TryParse(replyIdStr, out int replyId))
                    {
                        var replyMsg = _messages.FirstOrDefault(m => m.MessageId == replyId);
                        if (replyMsg != null)
                        {
                            replyContent = replyMsg.Content?.Length > 50 
                                ? replyMsg.Content.Substring(0, 50) + "..." 
                                : replyMsg.Content;
                        }
                    }
                    content = content.Substring(endIdx + 1).Trim();
                }
            }

            // Main container
            var container = new Panel
            {
                Width = containerWidth,
                AutoSize = false,
                MinimumSize = new Size(100, 40),
                Padding = new Padding(0),
                Margin = new Padding(0, 2, 0, 2),
                BackColor = Color.Transparent
            };

            // Bubble panel - different style for images
            int bubblePadding = isImage ? 0 : 8;
            var bubble = new Panel
            {
                AutoSize = true,
                MaximumSize = new Size(isImage ? 290 : 380, 0),
                MinimumSize = new Size(0, 0),
                Padding = new Padding(0),
                BackColor = isImage ? Color.Transparent : (isMine ? Color.FromArgb(0, 132, 255) : Color.FromArgb(233, 236, 239)),
                Tag = isMine
            };
            if (!isImage)
                bubble.Paint += (s, e) => DrawRoundedPanel(bubble, e, 10);

            int y = bubblePadding;

            // Reply preview
            if (!string.IsNullOrEmpty(replyContent))
            {
                var replyPanel = new Panel
                {
                    Size = new Size(250, 24),
                    BackColor = isMine ? Color.FromArgb(0, 100, 200) : Color.FromArgb(210, 215, 220),
                    Margin = new Padding(0)
                };
                var lblReply = new Label
                {
                    Text = $"↩ {replyContent}",
                    AutoSize = false,
                    Size = new Size(240, 20),
                    Location = new Point(5, 2),
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                    ForeColor = isMine ? Color.FromArgb(180, 200, 255) : Color.FromArgb(80, 80, 80)
                };
                replyPanel.Controls.Add(lblReply);
                replyPanel.Location = new Point(bubblePadding, y);
                bubble.Controls.Add(replyPanel);
                y += 26;
            }

            // Sender name (for others, not images)
            if (!isMine && !isImage)
            {
                var lblSender = new Label
                {
                    Text = msg.Sender,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Location = new Point(bubblePadding, y)
                };
                bubble.Controls.Add(lblSender);
                y += lblSender.Height + 2;
            }

            // Content or Image
            if (isImage)
            {
                var pic = new PictureBox
                {
                    Size = new Size(280, 200),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Cursor = Cursors.Hand,
                    BackColor = Color.FromArgb(240, 240, 240),
                    Tag = msg.MessageId,
                    Location = new Point(0, y)
                };
                pic.Click += (s, e) => PreviewImage(msg.MessageId);
                pic.DoubleClick += (s, e) => DownloadFile(msg.MessageId);
                bubble.Controls.Add(pic);
                y += pic.Height;

                _ = LoadImageAsync(pic, msg.MessageId);
            }
            else
            {
                var lblContent = new Label
                {
                    Text = content,
                    AutoSize = true,
                    MaximumSize = new Size(340, 0),
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = isMine ? Color.White : Color.FromArgb(33, 37, 41),
                    Location = new Point(bubblePadding, y)
                };
                bubble.Controls.Add(lblContent);
                y += lblContent.Height + 2;

                // Time (inline for text)
                var lblTime = new Label
                {
                    Text = msg.Timestamp.ToString("HH:mm"),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 7F),
                    ForeColor = isMine ? Color.FromArgb(180, 200, 255) : Color.FromArgb(140, 140, 140),
                    Location = new Point(bubblePadding, y)
                };
                bubble.Controls.Add(lblTime);
                y += lblTime.Height;
            }

            // Calculate bubble size
            int maxWidth = bubblePadding * 2;
            foreach (Control ctrl in bubble.Controls)
            {
                maxWidth = Math.Max(maxWidth, ctrl.Right + bubblePadding);
            }
            bubble.Height = y + bubblePadding;
            bubble.Width = isImage ? 280 : Math.Max(60, maxWidth);

            // Position - RIGHT for mine, LEFT for others
            if (isMine)
                bubble.Location = new Point(containerWidth - bubble.Width - 5, 3);
            else
                bubble.Location = new Point(45, 3);

            // Avatar for others
            if (!isMine)
            {
                var avatar = CreateAvatar(msg.Sender);
                avatar.Location = new Point(5, 3);
                container.Controls.Add(avatar);
            }

            // Context menu
            bubble.ContextMenuStrip = CreateContextMenu(msg);
            container.Controls.Add(bubble);
            container.Height = bubble.Height + 8;

            return container;
        }

        private Panel CreateAvatar(string name)
        {
            var avatar = new Panel { Size = new Size(35, 35) };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var color = GetAvatarColor(name);
                using var brush = new SolidBrush(color);
                e.Graphics.FillEllipse(brush, 0, 0, 33, 33);

                var initial = name.Length > 0 ? name[0].ToString().ToUpper() : "?";
                using var font = new Font("Segoe UI", 11F, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(initial, font, textBrush, new RectangleF(0, 0, 34, 34), sf);
            };
            return avatar;
        }

        private ContextMenuStrip CreateContextMenu(ChatMessageDto msg)
        {
            var menu = new ContextMenuStrip();
            
            var replyItem = new ToolStripMenuItem("↩️ Trả lời");
            replyItem.Click += (s, e) => SetReplyTo(msg);
            
            var copyItem = new ToolStripMenuItem("📋 Sao chép");
            copyItem.Click += (s, e) => Clipboard.SetText(msg.Content ?? "");
            
            var downloadItem = new ToolStripMenuItem("💾 Tải xuống");
            downloadItem.Click += (s, e) => DownloadFile(msg.MessageId);
            downloadItem.Visible = IsAttachment(msg.Content);
            
            var deleteItem = new ToolStripMenuItem("🗑️ Xóa");
            deleteItem.Click += async (s, e) => await DeleteMessageAsync(msg.MessageId);

            menu.Items.AddRange(new ToolStripItem[] { replyItem, copyItem, downloadItem, new ToolStripSeparator(), deleteItem });
            return menu;
        }

        private void DrawRoundedPanel(Panel panel, PaintEventArgs e, int radius)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
            using var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            using var brush = new SolidBrush(panel.BackColor);
            e.Graphics.FillPath(brush, path);
        }

        private async Task LoadImageAsync(PictureBox pic, int messageId)
        {
            try
            {
                var response = await _socketClient.DownloadAttachmentAsync(_currentUser, messageId);
                if (response?.Success == true && !string.IsNullOrEmpty(response.AttachmentContentBase64))
                {
                    var bytes = Convert.FromBase64String(response.AttachmentContentBase64);
                    using var ms = new MemoryStream(bytes);
                    var image = Image.FromStream(ms);
                    
                    if (pic.InvokeRequired)
                        pic.Invoke(() => pic.Image = image);
                    else
                        pic.Image = image;
                }
            }
            catch { }
        }

        #region Send Message
        private async Task SendMessageAsync()
        {
            var content = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(_currentConversationId)) return;

            var label = cbSecurityLabel.SelectedIndex + 1;

            if (_replyToMessageId.HasValue)
                content = $"[Reply:{_replyToMessageId}] {content}";

            try
            {
                btnSend.Enabled = false;
                var response = await _socketClient.SendMessageToConversationAsync(_currentUser, _currentConversationId, content, label);

                if (response?.Success == true)
                {
                    txtMessage.Clear();
                    CancelReply();
                    await LoadMessagesAsync();
                    UpdateStatus($"✓ Đã gửi (Mức {label})", false);
                }
                else
                {
                    MessageBox.Show(response?.Message ?? "Lỗi gửi tin nhắn", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                btnSend.Enabled = true;
                txtMessage.Focus();
            }
        }

        private async Task SendAttachmentAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file đính kèm",
                Filter = "Tất cả file (*.*)|*.*|Ảnh (*.jpg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|Tài liệu (*.pdf;*.doc;*.docx)|*.pdf;*.doc;*.docx"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                btnAttachment.Enabled = false;
                UpdateStatus("📤 Đang tải lên...");

                var fileBytes = await File.ReadAllBytesAsync(ofd.FileName);
                var fileName = Path.GetFileName(ofd.FileName);
                var label = cbSecurityLabel.SelectedIndex + 1;

                var uploadResponse = await _socketClient.UploadAttachmentAsync(_currentUser, fileName, fileBytes);
                if (uploadResponse?.Success != true || uploadResponse.AttachmentId <= 0)
                {
                    MessageBox.Show(uploadResponse?.Message ?? "Lỗi tải file", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var sendResponse = await _socketClient.SendMessageWithAttachmentAsync(
                    _currentUser, _currentConversationId, $"[File: {fileName}]", label, uploadResponse.AttachmentId);

                if (sendResponse?.Success == true)
                {
                    await LoadMessagesAsync();
                    UpdateStatus($"✓ Đã gửi: {fileName}", false);
                }
                else
                {
                    MessageBox.Show(sendResponse?.Message ?? "Lỗi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAttachment.Enabled = true;
                UpdateStatus("✓ Sẵn sàng", false);
            }
        }
        #endregion

        #region Reply
        private void SetReplyTo(ChatMessageDto msg)
        {
            _replyToMessageId = msg.MessageId;
            _replyToContent = msg.Content;
            var preview = msg.Content?.Length > 40 ? msg.Content.Substring(0, 40) + "..." : msg.Content;
            lblReplyTo.Text = $"↩ Trả lời {msg.Sender}: {preview}";
            pnlReply.Visible = true;
            txtMessage.Focus();
        }

        private void CancelReply()
        {
            _replyToMessageId = null;
            _replyToContent = null;
            pnlReply.Visible = false;
        }
        #endregion

        #region File Operations
        private void PreviewImage(int messageId)
        {
            var pic = FindPictureBox(messageId);
            if (pic?.Image == null) return;

            var form = new Form
            {
                Text = "Xem ảnh",
                Size = new Size(Math.Min(pic.Image.Width + 50, 1000), Math.Min(pic.Image.Height + 100, 800)),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var picBox = new PictureBox
            {
                Image = pic.Image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill
            };

            var btnClose = new Button
            {
                Text = "Đóng",
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };

            form.Controls.Add(picBox);
            form.Controls.Add(btnClose);
            form.ShowDialog(this);
        }

        private async void DownloadFile(int messageId)
        {
            try
            {
                var response = await _socketClient.DownloadAttachmentAsync(_currentUser, messageId);
                if (response?.Success != true || string.IsNullOrEmpty(response.AttachmentContentBase64))
                {
                    MessageBox.Show("Không thể tải file", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    FileName = response.AttachmentFileName ?? "attachment",
                    Title = "Lưu file"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var bytes = Convert.FromBase64String(response.AttachmentContentBase64);
                    await File.WriteAllBytesAsync(sfd.FileName, bytes);
                    UpdateStatus($"✓ Đã lưu: {Path.GetFileName(sfd.FileName)}", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private PictureBox? FindPictureBox(int messageId)
        {
            foreach (Control container in _messageContainer.Controls)
            {
                foreach (Control ctrl in container.Controls)
                {
                    if (ctrl is Panel bubble)
                    {
                        foreach (Control c in bubble.Controls)
                        {
                            if (c is PictureBox pic && pic.Tag is int id && id == messageId)
                                return pic;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region Delete Message
        private async Task DeleteMessageAsync(int messageId)
        {
            if (MessageBox.Show("Xóa tin nhắn này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var response = await _socketClient.DeleteMessageAsync(_currentUser, messageId.ToString());
            if (response?.Success == true)
            {
                await LoadMessagesAsync();
                UpdateStatus("✓ Đã xóa", false);
            }
            else
            {
                MessageBox.Show(response?.Message ?? "Lỗi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Group/Chat Operations
        private async Task CreateGroupAsync()
        {
            using var dialog = new CreateGroupDialog(_socketClient, _currentUser);
            if (dialog.ShowDialog() != DialogResult.OK) return;

            var response = await _socketClient.CreateGroupAsync(_currentUser, dialog.GroupName, dialog.GroupType, dialog.Members.ToArray());
            if (response?.Success == true)
            {
                MessageBox.Show("Tạo nhóm thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadConversationsAsync();
            }
            else
            {
                MessageBox.Show(response?.Message ?? "Lỗi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CreatePrivateChatAsync()
        {
            // Load danh sách users từ server
            var usersResponse = await _socketClient.GetUsersForChatAsync(_currentUser);
            if (usersResponse?.Success != true || usersResponse.UserList == null || usersResponse.UserList.Length == 0)
            {
                MessageBox.Show("Không tìm thấy người dùng khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Hiển thị dialog chọn user
            using var selectDialog = new Form
            {
                Text = "💬 Chọn người dùng để chat",
                Size = new Size(350, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblTitle = new Label
            {
                Text = "Chọn người dùng:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            var lstUsers = new ListBox
            {
                Location = new Point(15, 45),
                Size = new Size(300, 300),
                Font = new Font("Segoe UI", 10)
            };
            lstUsers.Items.AddRange(usersResponse.UserList);

            var btnOK = new Button
            {
                Text = "✅ Chat",
                Location = new Point(120, 360),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 132, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(230, 360),
                Size = new Size(80, 35),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            selectDialog.Controls.AddRange(new Control[] { lblTitle, lstUsers, btnOK, btnCancel });
            selectDialog.AcceptButton = btnOK;
            selectDialog.CancelButton = btnCancel;

            lstUsers.DoubleClick += (s, e) =>
            {
                if (lstUsers.SelectedItem != null)
                    selectDialog.DialogResult = DialogResult.OK;
            };

            if (selectDialog.ShowDialog() != DialogResult.OK || lstUsers.SelectedItem == null)
                return;

            var username = lstUsers.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(username)) return;

            var response = await _socketClient.CreatePrivateChatAsync(_currentUser, username);
            if (response?.Success == true)
            {
                await LoadConversationsAsync();
                UpdateStatus($"✓ Đã tạo chat với {username}", false);
            }
            else
            {
                MessageBox.Show(response?.Message ?? "Lỗi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ViewMembersAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Chọn cuộc trò chuyện", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var conv = lstConversations.SelectedItems.Count > 0 
                ? lstConversations.SelectedItems[0].Tag as ConversationDto 
                : null;
            var isPrivate = conv?.IsPrivate ?? false;

            using var dialog = new MembersDialog(_socketClient, _currentUser, _currentConversationId, isPrivate);
            await dialog.LoadMembersAsync();
            dialog.ShowDialog();
        }

        private async Task RefreshAsync()
        {
            await LoadConversationsAsync();
            if (!string.IsNullOrEmpty(_currentConversationId))
                await LoadMessagesAsync();
            UpdateStatus("✓ Đã làm mới", false);
        }

        private void LstConversations_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || lstConversations.SelectedItems.Count == 0)
                return;

            var conv = lstConversations.SelectedItems[0].Tag as ConversationDto;
            var isPrivate = conv?.IsPrivate ?? false;

            var contextMenu = new ContextMenuStrip();

            var viewMembersItem = new ToolStripMenuItem("👥 Xem thành viên");
            viewMembersItem.Click += async (s, args) => await ViewMembersAsync();
            contextMenu.Items.Add(viewMembersItem);

            var viewDetailsItem = new ToolStripMenuItem("📊 Chi tiết cuộc trò chuyện");
            viewDetailsItem.Click += async (s, args) => await ViewConversationDetailsAsync();
            contextMenu.Items.Add(viewDetailsItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            if (isPrivate)
            {
                var leaveItem = new ToolStripMenuItem("🚪 Rời cuộc trò chuyện");
                leaveItem.Click += async (s, args) => await LeaveConversationAsync();
                contextMenu.Items.Add(leaveItem);
            }
            else
            {
                var leaveItem = new ToolStripMenuItem("🚪 Rời nhóm");
                leaveItem.Click += async (s, args) => await LeaveConversationAsync();
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
                MessageBox.Show("Chọn cuộc trò chuyện", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var conv = lstConversations.SelectedItems.Count > 0 
                ? lstConversations.SelectedItems[0].Tag as ConversationDto 
                : null;
            var isPrivate = conv?.IsPrivate ?? false;

            var message = isPrivate
                ? "Bạn có chắc muốn rời cuộc trò chuyện này?\nBạn sẽ không thể xem tin nhắn cũ."
                : "Bạn có chắc muốn rời nhóm này?\nBạn sẽ không thể xem tin nhắn cũ.";

            if (MessageBox.Show(message, "Xác nhận rời", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                var response = await _socketClient.LeaveConversationAsync(_currentUser, _currentConversationId);
                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi rời cuộc trò chuyện.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã rời cuộc trò chuyện.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _currentConversationId = null;
                _messageContainer.Controls.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteGroupAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Chọn cuộc trò chuyện", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "Bạn có chắc muốn xóa nhóm này?\n\n⚠️ Tất cả thành viên sẽ không thể gửi tin nhắn mới.\n⚠️ Tin nhắn cũ vẫn còn nhưng nhóm sẽ bị đóng.",
                "Xác nhận xóa nhóm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                var response = await _socketClient.DeleteConversationAsync(_currentUser, _currentConversationId);
                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi xóa nhóm. Bạn có thể không phải chủ nhóm.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã xóa nhóm.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _currentConversationId = null;
                _messageContainer.Controls.Clear();
                await LoadConversationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region View Conversation Details
        private async Task ViewConversationDetailsAsync()
        {
            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Chọn cuộc trò chuyện", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var conv = lstConversations.SelectedItems.Count > 0 
                ? lstConversations.SelectedItems[0].Tag as ConversationDto 
                : null;

            var info = $"📋 Chi tiết cuộc trò chuyện\n\n" +
                       $"Tên: {conv?.ConversationName ?? _currentConversationName}\n" +
                       $"Mã: {_currentConversationId}\n" +
                       $"Loại: {(conv?.IsPrivate == true ? "Riêng tư" : "Nhóm")}\n" +
                       $"Số thành viên: {conv?.MemberCount ?? 0}\n" +
                       $"Ngày tạo: {conv?.CreatedAt:dd/MM/yyyy HH:mm}";

            var result = MessageBox.Show($"{info}\n\nBạn có muốn xem danh sách thành viên?", 
                "Chi tiết", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            
            if (result == DialogResult.Yes)
                await ViewMembersAsync();
        }
        #endregion

        #region User Profile
        private void ShowUserProfile()
        {
            try
            {
                var profileForm = new UserProfileForm(_socketClient, _currentUser, _currentUser.Matk);
                profileForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Helpers
        private void UpdateStatus(string message, bool isError = false)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = isError ? Color.Red : Color.FromArgb(40, 167, 69);
        }

        private static Color GetAvatarColor(string name)
        {
            var hash = Math.Abs(name.GetHashCode());
            var colors = new[] {
                Color.FromArgb(220, 20, 60), Color.FromArgb(30, 144, 255),
                Color.FromArgb(60, 179, 113), Color.FromArgb(255, 140, 0),
                Color.FromArgb(147, 112, 219), Color.FromArgb(255, 69, 0)
            };
            return colors[hash % colors.Length];
        }

        private static bool IsImageFile(string content)
        {
            var ext = Path.GetExtension(ExtractFileName(content)).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp";
        }

        private static bool IsAttachment(string? content)
        {
            return content?.Contains("[File:") == true;
        }

        private static string ExtractFileName(string content)
        {
            var start = content.IndexOf("[File:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            var end = content.IndexOf(']', start);
            if (end <= start) return "";
            return content.Substring(start + 6, end - start - 6).Trim();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Đăng xuất khỏi tài khoản?\n\nBạn sẽ quay về màn hình đăng nhập.",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _socketClient?.Dispose();
                }
                catch { /* Ignore disposal errors */ }

                // Close this form và trigger showing LoginForm in Program.cs
                this.DialogResult = DialogResult.Cancel; // Signal logout
                this.Close();
            }
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Controls
{
    /// <summary>
    /// Panel chứa các tin nhắn chat với scroll
    /// </summary>
    public class ChatPanel : Panel
    {
        private readonly FlowLayoutPanel _messagesContainer;
        private readonly Dictionary<int, MessageBubble> _messageBubbles = new();
        private readonly Dictionary<int, Image> _imageCache = new();
        private readonly object _lockObj = new();

        public event EventHandler<int>? ReplyToMessage;
        public event EventHandler<int>? DownloadAttachment;
        public event EventHandler<int>? PreviewImage;
        public event EventHandler<int>? DeleteMessage;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SocketClientService? SocketClient { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public User? CurrentUser { get; set; }

        public ChatPanel()
        {
            this.AutoScroll = true;
            this.BackColor = Color.FromArgb(245, 245, 248);
            this.Padding = new Padding(5);
            this.DoubleBuffered = true;

            _messagesContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5),
                BackColor = Color.Transparent
            };

            this.Controls.Add(_messagesContainer);
            this.Resize += (s, e) => AdjustBubbleWidths();
        }

        public void ClearMessages()
        {
            _messagesContainer.SuspendLayout();
            _messagesContainer.Controls.Clear();
            _messageBubbles.Clear();
            _messagesContainer.ResumeLayout();
        }

        public void LoadMessages(ChatMessageDto[] messages, string currentUserMatk)
        {
            _messagesContainer.SuspendLayout();
            ClearMessages();

            foreach (var msg in messages.OrderBy(m => m.Timestamp))
            {
                AddMessageBubble(msg, currentUserMatk);
            }

            _messagesContainer.ResumeLayout();
            ScrollToBottom();
        }

        public void AddMessage(ChatMessageDto msg, string currentUserMatk)
        {
            _messagesContainer.SuspendLayout();
            AddMessageBubble(msg, currentUserMatk);
            _messagesContainer.ResumeLayout();
            ScrollToBottom();
        }

        private void AddMessageBubble(ChatMessageDto msg, string currentUserMatk)
        {
            var isMine = string.Equals(msg.Sender, currentUserMatk, StringComparison.OrdinalIgnoreCase);
            var isImage = IsImageAttachment(msg.Content);
            var fileName = ExtractFileName(msg.Content);

            var bubble = new MessageBubble
            {
                MessageId = msg.MessageId,
                SenderName = isMine ? "Bạn" : msg.Sender,
                Content = msg.Content,
                Timestamp = msg.Timestamp,
                IsMine = isMine,
                IsImage = isImage,
                AttachmentFileName = fileName,
                Width = _messagesContainer.Width - 20,
                Margin = new Padding(0, 2, 0, 2)
            };

            // Events
            bubble.ReplyClicked += (s, id) => ReplyToMessage?.Invoke(this, id);
            bubble.DownloadClicked += (s, id) => DownloadAttachment?.Invoke(this, id);
            bubble.PreviewClicked += (s, id) => PreviewImage?.Invoke(this, id);
            bubble.DeleteClicked += (s, id) => DeleteMessage?.Invoke(this, id);

            bubble.UpdateLayout();
            _messagesContainer.Controls.Add(bubble);
            _messageBubbles[msg.MessageId] = bubble;

            // Load image async
            if (isImage && !string.IsNullOrEmpty(fileName))
            {
                _ = LoadImageAsync(msg.MessageId, bubble);
            }
        }

        private async Task LoadImageAsync(int messageId, MessageBubble bubble)
        {
            if (SocketClient == null || CurrentUser == null) return;

            try
            {
                lock (_lockObj)
                {
                    if (_imageCache.TryGetValue(messageId, out var cached))
                    {
                        bubble.AttachmentImage = cached;
                        this.BeginInvoke(() => bubble.UpdateLayout());
                        return;
                    }
                }

                var response = await SocketClient.DownloadAttachmentAsync(CurrentUser, messageId);
                if (response?.Success == true && !string.IsNullOrEmpty(response.AttachmentContentBase64))
                {
                    var bytes = Convert.FromBase64String(response.AttachmentContentBase64);
                    using var ms = new System.IO.MemoryStream(bytes);
                    var image = Image.FromStream(ms);

                    lock (_lockObj)
                    {
                        _imageCache[messageId] = image;
                    }

                    bubble.AttachmentImage = image;
                    
                    if (this.InvokeRequired)
                        this.BeginInvoke(() => bubble.UpdateLayout());
                    else
                        bubble.UpdateLayout();
                }
            }
            catch { /* Ignore */ }
        }

        public void ScrollToBottom()
        {
            this.BeginInvoke(() =>
            {
                this.VerticalScroll.Value = this.VerticalScroll.Maximum;
                this.PerformLayout();
            });
        }

        private void AdjustBubbleWidths()
        {
            foreach (var bubble in _messageBubbles.Values)
            {
                bubble.Width = _messagesContainer.Width - 20;
            }
        }

        public MessageBubble? GetBubble(int messageId)
        {
            _messageBubbles.TryGetValue(messageId, out var bubble);
            return bubble;
        }

        public Image? GetCachedImage(int messageId)
        {
            lock (_lockObj)
            {
                _imageCache.TryGetValue(messageId, out var image);
                return image;
            }
        }

        private static bool IsImageAttachment(string content)
        {
            var fileName = ExtractFileName(content);
            if (string.IsNullOrEmpty(fileName)) return false;
            var ext = System.IO.Path.GetExtension(fileName).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".bmp" || ext == ".webp";
        }

        private static string ExtractFileName(string content)
        {
            if (string.IsNullOrEmpty(content)) return "";
            var start = content.IndexOf("[File:", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return "";
            var end = content.IndexOf(']', start);
            if (end <= start) return "";
            return content.Substring(start + 6, end - start - 6).Trim();
        }
    }
}

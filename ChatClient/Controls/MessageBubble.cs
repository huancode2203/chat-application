using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace ChatClient.Controls
{
    /// <summary>
    /// Custom control để hiển thị một tin nhắn dạng bubble
    /// </summary>
    public class MessageBubble : UserControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MessageId { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SenderName { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Content { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime Timestamp { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsMine { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsImage { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? AttachmentFileName { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? AttachmentImage { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? ReplyToMessageId { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? ReplyToContent { get; set; }

        // Events
        public event EventHandler<int>? ReplyClicked;
        public event EventHandler<int>? DownloadClicked;
        public event EventHandler<int>? PreviewClicked;
        public event EventHandler<int>? DeleteClicked;

        // UI Elements
        private Panel bubblePanel = null!;
        private Label lblSender = null!;
        private Label lblContent = null!;
        private Label lblTime = null!;
        private PictureBox? picImage;
        private Panel? replyPanel;
        private Label? lblReply;
        private Panel avatarPanel = null!;
        private ContextMenuStrip contextMenu = null!;

        // Colors
        private readonly Color MyBubbleColor = Color.FromArgb(0, 132, 255);
        private readonly Color OtherBubbleColor = Color.FromArgb(233, 236, 239);
        private readonly Color MyTextColor = Color.White;
        private readonly Color OtherTextColor = Color.FromArgb(33, 37, 41);
        private readonly Color ReplyBgColor = Color.FromArgb(200, 220, 240);

        public MessageBubble()
        {
            InitializeControls();
            SetupContextMenu();
        }

        private void InitializeControls()
        {
            this.BackColor = Color.Transparent;
            this.Padding = new Padding(5, 3, 5, 3);
            this.Margin = new Padding(0);
            this.AutoSize = true;
            this.MinimumSize = new Size(100, 50);

            // Avatar panel (for others)
            avatarPanel = new Panel
            {
                Size = new Size(36, 36),
                Location = new Point(8, 5),
                BackColor = Color.Transparent
            };
            avatarPanel.Paint += AvatarPanel_Paint;

            // Main bubble panel
            bubblePanel = new Panel
            {
                AutoSize = true,
                MinimumSize = new Size(120, 40),
                MaximumSize = new Size(420, 0),
                Padding = new Padding(12, 8, 12, 8)
            };
            bubblePanel.Paint += BubblePanel_Paint;

            // Sender label
            lblSender = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100),
                Margin = new Padding(0, 0, 0, 2)
            };

            // Content label
            lblContent = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Font = new Font("Segoe UI", 9.5F),
                Margin = new Padding(0, 2, 0, 2)
            };

            // Time label
            lblTime = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = Color.FromArgb(150, 150, 150),
                Margin = new Padding(0, 2, 0, 0)
            };

            // Add controls to bubble
            bubblePanel.Controls.Add(lblSender);
            bubblePanel.Controls.Add(lblContent);
            bubblePanel.Controls.Add(lblTime);

            this.Controls.Add(avatarPanel);
            this.Controls.Add(bubblePanel);
        }

        private void SetupContextMenu()
        {
            contextMenu = new ContextMenuStrip();
            
            var replyItem = new ToolStripMenuItem("↩️ Trả lời");
            replyItem.Click += (s, e) => ReplyClicked?.Invoke(this, MessageId);
            
            var copyItem = new ToolStripMenuItem("📋 Sao chép");
            copyItem.Click += (s, e) => Clipboard.SetText(Content);
            
            var downloadItem = new ToolStripMenuItem("💾 Tải xuống");
            downloadItem.Click += (s, e) => DownloadClicked?.Invoke(this, MessageId);
            
            var previewItem = new ToolStripMenuItem("🖼️ Xem ảnh");
            previewItem.Click += (s, e) => PreviewClicked?.Invoke(this, MessageId);
            
            var deleteItem = new ToolStripMenuItem("🗑️ Xóa");
            deleteItem.Click += (s, e) => DeleteClicked?.Invoke(this, MessageId);

            contextMenu.Items.AddRange(new ToolStripItem[] { replyItem, copyItem, new ToolStripSeparator(), downloadItem, previewItem, new ToolStripSeparator(), deleteItem });
            
            bubblePanel.ContextMenuStrip = contextMenu;
            lblContent.ContextMenuStrip = contextMenu;
        }

        public void UpdateLayout()
        {
            SuspendLayout();

            // Colors based on sender
            var bubbleColor = IsMine ? MyBubbleColor : OtherBubbleColor;
            var textColor = IsMine ? MyTextColor : OtherTextColor;

            bubblePanel.BackColor = bubbleColor;
            lblContent.ForeColor = textColor;
            lblTime.ForeColor = IsMine ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(130, 130, 130);

            // Sender name (only for others)
            lblSender.Visible = !IsMine;
            lblSender.Text = SenderName;

            // Content
            lblContent.Text = Content;

            // Time
            lblTime.Text = Timestamp.ToString("HH:mm");

            // Avatar visibility
            avatarPanel.Visible = !IsMine;

            // Handle reply panel
            SetupReplyPanel();

            // Handle image
            SetupImagePanel();

            // Layout positions
            LayoutControls();

            // Update context menu
            UpdateContextMenu();

            ResumeLayout(true);
        }

        private void SetupReplyPanel()
        {
            if (!string.IsNullOrEmpty(ReplyToContent))
            {
                if (replyPanel == null)
                {
                    replyPanel = new Panel
                    {
                        AutoSize = true,
                        BackColor = ReplyBgColor,
                        Padding = new Padding(8, 4, 8, 4),
                        Margin = new Padding(0, 0, 0, 6)
                    };

                    lblReply = new Label
                    {
                        AutoSize = true,
                        MaximumSize = new Size(350, 40),
                        Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                        ForeColor = Color.FromArgb(80, 80, 80)
                    };
                    replyPanel.Controls.Add(lblReply);
                    bubblePanel.Controls.Add(replyPanel);
                }

                lblReply!.Text = "↩ " + (ReplyToContent.Length > 50 ? ReplyToContent.Substring(0, 50) + "..." : ReplyToContent);
                replyPanel.Visible = true;
            }
            else if (replyPanel != null)
            {
                replyPanel.Visible = false;
            }
        }

        private void SetupImagePanel()
        {
            if (AttachmentImage != null)
            {
                if (picImage == null)
                {
                    picImage = new PictureBox
                    {
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Cursor = Cursors.Hand,
                        Margin = new Padding(0, 4, 0, 4)
                    };
                    picImage.Click += (s, e) => PreviewClicked?.Invoke(this, MessageId);
                    picImage.DoubleClick += (s, e) => PreviewClicked?.Invoke(this, MessageId);
                    bubblePanel.Controls.Add(picImage);
                }

                // Scale image
                float maxWidth = 280, maxHeight = 200;
                float scale = Math.Min(maxWidth / AttachmentImage.Width, maxHeight / AttachmentImage.Height);
                if (scale > 1) scale = 1;

                picImage.Size = new Size((int)(AttachmentImage.Width * scale), (int)(AttachmentImage.Height * scale));
                picImage.Image = AttachmentImage;
                picImage.Visible = true;

                // Hide text content for pure images
                if (IsImage && Content.StartsWith("[File:"))
                {
                    lblContent.Visible = false;
                }
            }
            else if (picImage != null)
            {
                picImage.Visible = false;
            }
        }

        private void LayoutControls()
        {
            int bubbleTop = 5;
            int bubbleLeft = IsMine ? (this.Parent?.Width ?? 600) - bubblePanel.Width - 15 : 50;

            // Position avatar
            if (!IsMine)
            {
                avatarPanel.Location = new Point(8, bubbleTop);
            }

            // Position bubble panel
            int yOffset = 8;
            
            // Reply panel
            if (replyPanel != null && replyPanel.Visible)
            {
                replyPanel.Location = new Point(12, yOffset);
                yOffset += replyPanel.Height + 4;
            }

            // Sender
            if (lblSender.Visible)
            {
                lblSender.Location = new Point(12, yOffset);
                yOffset += lblSender.Height + 2;
            }

            // Image
            if (picImage != null && picImage.Visible)
            {
                picImage.Location = new Point(12, yOffset);
                yOffset += picImage.Height + 4;
            }

            // Content
            if (lblContent.Visible)
            {
                lblContent.Location = new Point(12, yOffset);
                yOffset += lblContent.Height + 2;
            }

            // Time
            lblTime.Location = new Point(bubblePanel.Width - lblTime.Width - 12, yOffset);

            // Calculate bubble size
            int bubbleWidth = Math.Max(120, Math.Max(lblContent.Visible ? lblContent.Width : 0, picImage?.Width ?? 0) + 24);
            int bubbleHeight = yOffset + lblTime.Height + 8;
            bubblePanel.Size = new Size(Math.Min(bubbleWidth, 420), bubbleHeight);

            // Re-position bubble for right alignment
            if (IsMine && this.Parent != null)
            {
                bubbleLeft = this.Parent.Width - bubblePanel.Width - 15;
            }
            bubblePanel.Location = new Point(bubbleLeft, bubbleTop);

            // Set control height
            this.Height = bubblePanel.Height + 10;
        }

        private void UpdateContextMenu()
        {
            // Show/hide relevant menu items
            foreach (ToolStripItem item in contextMenu.Items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text.Contains("Tải xuống") || menuItem.Text.Contains("Xem ảnh"))
                    {
                        menuItem.Visible = !string.IsNullOrEmpty(AttachmentFileName);
                    }
                }
            }
        }

        private void AvatarPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (IsMine) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw circle
            var color = GetAvatarColor(SenderName);
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 0, 0, 34, 34);

            // Draw initial
            var initial = SenderName.Length > 0 ? SenderName[0].ToString().ToUpper() : "?";
            using var font = new Font("Segoe UI", 12F, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(initial, font, textBrush, new RectangleF(0, 0, 34, 34), sf);
        }

        private void BubblePanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, bubblePanel.Width - 1, bubblePanel.Height - 1);
            var color = IsMine ? MyBubbleColor : OtherBubbleColor;

            using var path = GetRoundedRectPath(rect, 12);
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Color GetAvatarColor(string name)
        {
            var hash = Math.Abs(name.GetHashCode());
            var colors = new[] {
                Color.FromArgb(220, 20, 60), Color.FromArgb(30, 144, 255),
                Color.FromArgb(60, 179, 113), Color.FromArgb(255, 140, 0),
                Color.FromArgb(147, 112, 219), Color.FromArgb(255, 69, 0),
                Color.FromArgb(72, 209, 204), Color.FromArgb(199, 21, 133)
            };
            return colors[hash % colors.Length];
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent != null)
            {
                this.Width = Parent.Width - 20;
                LayoutControls();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }
    }
}

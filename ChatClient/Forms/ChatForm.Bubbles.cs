using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChatClient.Forms
{
    /// <summary>
    /// Partial class for bubble-related functionality in ChatForm.
    /// </summary>
    public partial class ChatForm
    {
        private bool _isDarkMode = false;
        private const int BUBBLE_MARGIN = 8;
        private const int BUBBLE_PADDING = 10;
        private const int BUBBLE_RADIUS = 12;
        private const int MAX_BUBBLE_WIDTH = 420;
        private const int ROW_HEIGHT = 60;
        private ImageList? _rowHeightImageList;

        private void SetupMessageListDrawing()
        {
            if (lstMessages == null) return;

            lstMessages.OwnerDraw = true;
            lstMessages.View = View.Details;
            lstMessages.FullRowSelect = true;
            lstMessages.GridLines = false;
            lstMessages.HoverSelection = false;
            lstMessages.HotTracking = false;
            lstMessages.BorderStyle = BorderStyle.None;
            lstMessages.MultiSelect = false;
            lstMessages.HeaderStyle = ColumnHeaderStyle.None;

            // Row height via ImageList
            _rowHeightImageList = new ImageList { ImageSize = new Size(1, ROW_HEIGHT) };
            lstMessages.SmallImageList = _rowHeightImageList;

            lstMessages.DrawItem += LstMessages_DrawItem;
            lstMessages.DrawSubItem += LstMessages_DrawSubItem;
            lstMessages.DrawColumnHeader += LstMessages_DrawColumnHeader;
        }

        private void LstMessages_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = false;
            using var brush = new SolidBrush(_isDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(240, 240, 240));
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        private void LstMessages_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            // Handled in DrawItem
        }

        private void LstMessages_DrawItem(object? sender, DrawListViewItemEventArgs e)
        {
            if (e.Item == null) return;

            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Background
                var bgColor = _isDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 245, 248);
                using (var bgBrush = new SolidBrush(bgColor))
                    g.FillRectangle(bgBrush, e.Bounds);

                // Get data
                var isMine = e.Item.SubItems.Count > 1 && e.Item.SubItems[1].Text == "Bạn";
                var content = e.Item.SubItems.Count > 2 ? e.Item.SubItems[2].Text : "";
                var senderName = e.Item.SubItems.Count > 1 ? e.Item.SubItems[1].Text : "";
                var time = e.Item.SubItems.Count > 0 ? e.Item.SubItems[0].Text : "";
                var messageId = e.Item.Tag != null ? Convert.ToInt32(e.Item.Tag) : 0;
                var isSelected = e.Item.Selected;

                // Fonts
                using var textFont = new Font("Segoe UI", 9F);
                using var senderFont = new Font("Segoe UI", 8F, FontStyle.Bold);
                using var timeFont = new Font("Segoe UI", 7F);

                // Measure text
                var textSize = g.MeasureString(content, textFont, MAX_BUBBLE_WIDTH - BUBBLE_PADDING * 2);
                var bubbleWidth = Math.Min((int)textSize.Width + BUBBLE_PADDING * 2 + 10, MAX_BUBBLE_WIDTH);
                var bubbleHeight = Math.Min((int)textSize.Height + BUBBLE_PADDING + 16, ROW_HEIGHT - 6);

                // Check for image
                bool isImageAttachment = TryParseAttachmentFileName(content, out _) && HasImageExtension(content);
                Image? inlineImage = null;
                if (isImageAttachment && messageId > 0)
                {
                    lock (_inlineImageLock)
                        _inlineImageCache.TryGetValue(messageId, out inlineImage);
                }

                // Position
                int topMargin = e.Bounds.Top + 3;
                Rectangle bubbleRect;
                Color bubbleColor, textColor;

                if (isMine)
                {
                    bubbleColor = _isDarkMode ? Color.FromArgb(0, 92, 75) : Color.FromArgb(0, 132, 255);
                    textColor = Color.White;
                    bubbleRect = new Rectangle(e.Bounds.Right - bubbleWidth - BUBBLE_MARGIN, topMargin, bubbleWidth, bubbleHeight);
                }
                else
                {
                    bubbleColor = _isDarkMode ? Color.FromArgb(58, 58, 60) : Color.FromArgb(232, 234, 237);
                    textColor = _isDarkMode ? Color.White : Color.Black;
                    int x = BUBBLE_MARGIN + 38;
                    bubbleRect = new Rectangle(x, topMargin, bubbleWidth, bubbleHeight);

                    // Sender name above bubble
                    using (var nameBrush = new SolidBrush(Color.FromArgb(120, 120, 120)))
                        g.DrawString(senderName, senderFont, nameBrush, x, topMargin - 1);
                    bubbleRect.Y += 12;
                    bubbleRect.Height -= 12;
                }

                // Draw bubble
                DrawRoundedRectangle(g, bubbleRect, BUBBLE_RADIUS, bubbleColor, isSelected);

                // Avatar for others
                if (!isMine)
                {
                    var avatarRect = new Rectangle(BUBBLE_MARGIN, topMargin + 8, 28, 28);
                    using (var avatarBrush = new SolidBrush(GetAvatarColor(senderName)))
                        g.FillEllipse(avatarBrush, avatarRect);
                    
                    var initial = senderName.Length > 0 ? senderName[0].ToString().ToUpper() : "?";
                    using (var initialBrush = new SolidBrush(Color.White))
                    using (var initialFont = new Font("Segoe UI", 10F, FontStyle.Bold))
                    {
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        g.DrawString(initial, initialFont, initialBrush, avatarRect, sf);
                    }
                }

                // Content
                int contentY = bubbleRect.Y + 2;
                
                // Draw image if available
                if (inlineImage != null)
                {
                    float scale = Math.Min(200f / inlineImage.Width, 150f / inlineImage.Height);
                    if (scale > 1) scale = 1;
                    var imgRect = new Rectangle(bubbleRect.X + 2, contentY, (int)(inlineImage.Width * scale), (int)(inlineImage.Height * scale));
                    try { g.DrawImage(inlineImage, imgRect); } catch { }
                }
                else if (!isImageAttachment)
                {
                    // Text content
                    var textRect = new RectangleF(bubbleRect.X + BUBBLE_PADDING, contentY, bubbleRect.Width - BUBBLE_PADDING * 2, textSize.Height);
                    using (var textBrush = new SolidBrush(textColor))
                        g.DrawString(content, textFont, textBrush, textRect);
                }

                // Timestamp
                var timeColor = isMine ? Color.FromArgb(200, 255, 255, 255) : Color.FromArgb(140, 140, 140);
                using (var timeBrush = new SolidBrush(timeColor))
                {
                    var ts = g.MeasureString(time, timeFont);
                    g.DrawString(time, timeFont, timeBrush, bubbleRect.Right - ts.Width - 6, bubbleRect.Bottom - 14);
                }
            }
            catch { /* Ignore draw errors */ }
        }

        private void DrawRoundedRectangle(Graphics g, Rectangle bounds, int radius, Color color, bool drawBorder)
        {
            using var path = GetRoundedRectanglePath(bounds, radius);
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);

            if (drawBorder)
            {
                using var pen = new Pen(Color.FromArgb(0, 120, 215), 2);
                g.DrawPath(pen, path);
            }
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            var arc = new Rectangle(bounds.Location, new Size(d, d));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Color GetAvatarColor(string name)
        {
            var hash = name.GetHashCode();
            var colors = new[] {
                Color.FromArgb(220, 20, 60), Color.FromArgb(30, 144, 255),
                Color.FromArgb(60, 179, 113), Color.FromArgb(255, 140, 0),
                Color.FromArgb(147, 112, 219), Color.FromArgb(255, 69, 0),
                Color.FromArgb(72, 209, 204), Color.FromArgb(199, 21, 133)
            };
            return colors[Math.Abs(hash) % colors.Length];
        }

        public void ApplyDarkMode(bool enabled)
        {
            _isDarkMode = enabled;
            var bgColor = enabled ? Color.FromArgb(30, 30, 30) : Color.White;
            var fgColor = enabled ? Color.FromArgb(220, 220, 220) : Color.Black;
            
            this.BackColor = bgColor;
            this.ForeColor = fgColor;
            ApplyDarkModeToControls(this.Controls, enabled);
            lstMessages?.Invalidate();
        }

        private void ApplyDarkModeToControls(Control.ControlCollection controls, bool enabled)
        {
            var bgColor = enabled ? Color.FromArgb(30, 30, 30) : Color.White;
            var fgColor = enabled ? Color.FromArgb(220, 220, 220) : Color.Black;
            var panelBg = enabled ? Color.FromArgb(45, 45, 48) : Color.FromArgb(245, 245, 245);

            foreach (Control ctrl in controls)
            {
                if (ctrl is TextBox txt) { txt.BackColor = enabled ? Color.FromArgb(60, 60, 60) : Color.White; txt.ForeColor = fgColor; }
                else if (ctrl is ComboBox cbo) { cbo.BackColor = enabled ? Color.FromArgb(60, 60, 60) : Color.White; cbo.ForeColor = fgColor; }
                else if (ctrl is ListView lv) { lv.BackColor = bgColor; lv.ForeColor = fgColor; }
                else if (ctrl is Panel pnl) { pnl.BackColor = panelBg; pnl.ForeColor = fgColor; }
                else if (ctrl is GroupBox grp) { grp.BackColor = panelBg; grp.ForeColor = fgColor; }
                else if (ctrl is Label lbl) { lbl.ForeColor = fgColor; }
                
                if (ctrl.HasChildren) ApplyDarkModeToControls(ctrl.Controls, enabled);
            }
        }
    }
}

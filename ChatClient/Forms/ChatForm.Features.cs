using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace ChatClient.Forms
{
    /// <summary>
    /// Additional features for ChatForm: emoji picker, search, reactions
    /// </summary>
    public partial class ChatForm
    {
        private Button? _btnEmoji;
        private Button? _btnSearch;
        
        private void AddModernFeatures()
        {
            // Add emoji button next to send button
            _btnEmoji = new Button
            {
                Text = "😊",
                Size = new Size(50, 40),
                Location = new Point(620, 95),
                Font = new Font("Segoe UI", 14F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                TabIndex = 10
            };
            _btnEmoji.FlatAppearance.BorderSize = 1;
            _btnEmoji.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnEmoji.Click += (s, e) => ShowEmojiPicker();
            
            // Add search button to toolbar
            if (toolStrip1 != null)
            {
                var searchButton = new ToolStripButton("🔍 Tìm kiếm");
                searchButton.Click += (s, e) => ShowSearchDialog();
                toolStrip1.Items.Insert(0, searchButton);
            }
            
            if (grpChat != null)
            {
                grpChat.Controls.Add(_btnEmoji);
                _btnEmoji.BringToFront();
            }
        }
        
        private void ShowEmojiPicker()
        {
            var emojiForm = new Form
            {
                Text = "Chọn emoji",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            var emojiPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            
            var emojis = new[]
            {
                "😊", "😂", "❤️", "👍", "👎", "🔥", "🎉", "😍", "😎", "🤔",
                "😢", "😭", "😡", "😱", "🤗", "🥳", "😴", "🤯", "💯", "✅",
                "❌", "⭐", "💪", "👏", "🙏", "🤝", "💼", "📱", "💻", "📧",
                "📎", "🖼️", "🎵", "🎮", "⚽", "🏀", "🎯", "🎲", "🍕", "🍔",
                "☕", "🎂", "🌈", "🌟", "⚡", "🔔", "🔓", "🔐", "🛡️", "🚀"
            };
            
            foreach (var emoji in emojis)
            {
                var btn = new Button
                {
                    Text = emoji,
                    Size = new Size(50, 50),
                    Font = new Font("Segoe UI", 20F),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    if (txtMessage != null)
                    {
                        txtMessage.Text += emoji;
                        txtMessage.Focus();
                        txtMessage.SelectionStart = txtMessage.Text.Length;
                    }
                    emojiForm.Close();
                };
                emojiPanel.Controls.Add(btn);
            }
            
            emojiForm.Controls.Add(emojiPanel);
            emojiForm.ShowDialog(this);
        }
        
        private void ShowSearchDialog()
        {
            var searchForm = new Form
            {
                Text = "Tìm kiếm tin nhắn",
                Size = new Size(500, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };
            
            var lblSearch = new Label
            {
                Text = "Từ khóa:",
                Location = new Point(20, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };
            
            var txtSearch = new TextBox
            {
                Location = new Point(100, 27),
                Size = new Size(250, 27),
                Font = new Font("Segoe UI", 10F)
            };
            
            var btnFind = new Button
            {
                Text = "Tìm",
                Location = new Point(360, 25),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnFind.FlatAppearance.BorderSize = 0;
            
            var lblResult = new Label
            {
                Text = "",
                Location = new Point(20, 70),
                Size = new Size(450, 40),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray
            };
            
            btnFind.Click += (s, e) =>
            {
                var keyword = txtSearch.Text.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(keyword))
                {
                    lblResult.Text = "Vui lòng nhập từ khóa tìm kiếm.";
                    return;
                }
                
                if (lstMessages == null || lstMessages.Items.Count == 0)
                {
                    lblResult.Text = "Không có tin nhắn để tìm kiếm.";
                    return;
                }
                
                lstMessages.SelectedItems.Clear();
                int foundCount = 0;
                
                for (int i = 0; i < lstMessages.Items.Count; i++)
                {
                    var item = lstMessages.Items[i];
                    if (item.SubItems.Count > 2)
                    {
                        var content = item.SubItems[2].Text.ToLowerInvariant();
                        if (content.Contains(keyword))
                        {
                            if (foundCount == 0)
                            {
                                item.Selected = true;
                                item.EnsureVisible();
                            }
                            foundCount++;
                        }
                    }
                }
                
                if (foundCount > 0)
                {
                    lblResult.Text = $"Tìm thấy {foundCount} tin nhắn chứa \"{txtSearch.Text}\".";
                    lblResult.ForeColor = Color.Green;
                }
                else
                {
                    lblResult.Text = $"Không tìm thấy tin nhắn nào chứa \"{txtSearch.Text}\".";
                    lblResult.ForeColor = Color.Red;
                }
            };
            
            txtSearch.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    btnFind.PerformClick();
                }
            };
            
            searchForm.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnFind, lblResult });
            searchForm.ShowDialog(this);
        }
        
        private void AddReactionToMessage(string emoji)
        {
            if (lstMessages == null || lstMessages.SelectedItems.Count == 0) return;
            
            var item = lstMessages.SelectedItems[0];
            // Add emoji reaction indicator
            if (item.SubItems.Count > 2)
            {
                var currentContent = item.SubItems[2].Text;
                if (!currentContent.EndsWith(" " + emoji))
                {
                    item.SubItems[2].Text = currentContent + " " + emoji;
                }
            }
            
            UpdateStatus($"Đã thêm phản ứng {emoji}", false);
        }
    }
}

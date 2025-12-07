using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient.Forms
{
    public partial class ChatForm
    {
        private Panel? _searchPanel;
        private TextBox? _conversationSearchTextBox;
        private readonly List<ListViewItem> _allConversations = new();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                // Căn chỉnh font và màu nền tổng thể cho form chat
                this.Font = new Font("Segoe UI", 10F);
                this.BackColor = Color.White;

                if (panel2 != null)
                {
                    panel2.BackColor = Color.WhiteSmoke;
                }

                InitializeConversationSearchUi();
            }
            catch
            {
                // Không để UI crash nếu control chưa sẵn sàng
            }
        }

        private void InitializeConversationSearchUi()
        {
            if (_searchPanel != null)
            {
                return;
            }

            if (splitContainer1 == null)
            {
                return;
            }

            _searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(10, 10, 10, 0),
                BackColor = Color.WhiteSmoke
            };

            _conversationSearchTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Name = "txtConversationSearch",
                Font = new Font("Segoe UI", 9F)
            };

            try
            {
                _conversationSearchTextBox.PlaceholderText = "Tìm cuộc trò chuyện...";
            }
            catch
            {
                // PlaceholderText có thể không tồn tại ở .NET cũ, bỏ qua
            }

            _conversationSearchTextBox.TextChanged += (_, _) => ApplyConversationFilter(_conversationSearchTextBox.Text);

            _searchPanel.Controls.Add(_conversationSearchTextBox);
            splitContainer1.Panel1.Controls.Add(_searchPanel);
            // Đưa panel search lên trên cùng bên trái
            splitContainer1.Panel1.Controls.SetChildIndex(_searchPanel, 0);
        }

        private void ApplyConversationFilter(string query)
        {
            if (lstConversations == null)
            {
                return;
            }

            // Lần đầu filter thì chụp lại toàn bộ danh sách hiện tại
            if (_allConversations.Count == 0)
            {
                foreach (ListViewItem item in lstConversations.Items)
                {
                    _allConversations.Add((ListViewItem)item.Clone());
                }
            }

            lstConversations.BeginUpdate();
            lstConversations.Items.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                foreach (var item in _allConversations)
                {
                    lstConversations.Items.Add((ListViewItem)item.Clone());
                }

                lstConversations.EndUpdate();
                return;
            }

            var q = query.Trim();

            foreach (var item in _allConversations)
            {
                var name = item.Text ?? string.Empty;
                var members = item.SubItems.Count > 1 ? item.SubItems[1].Text ?? string.Empty : string.Empty;
                var type = item.SubItems.Count > 2 ? item.SubItems[2].Text ?? string.Empty : string.Empty;

                if (name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    members.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    type.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    lstConversations.Items.Add((ListViewItem)item.Clone());
                }
            }

            lstConversations.EndUpdate();
        }
    }
}
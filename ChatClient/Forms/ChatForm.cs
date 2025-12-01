using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form chat chính.
    /// - Kết nối SocketClientService tới server.
    /// - Gửi tin: tạo ChatRequest, mã hóa, gửi qua TCP.
    /// - Nhận tin: gọi GetMessagesAsync, server lọc theo MAC/VPD và trả về danh sách tin.
    /// </summary>
    public partial class ChatForm : Form
    {
        private readonly User _currentUser;
        private readonly SocketClientService _socketClient;

        public ChatForm(User currentUser)
        {
            _currentUser = currentUser;
            _socketClient = new SocketClientService("127.0.0.1", 9000);

            InitializeComponent();
            SetupEventHandlers();
            Shown += async (_, _) => await ConnectToServerAsync();
        }

        private void SetupEventHandlers()
        {
            Text = $"Chat Nội Bộ - {_currentUser.Username} (Mức: {_currentUser.ClearanceLevel})";
            cbLabel.SelectedIndex = 0;

            btnSend.Click += async (_, _) => await SendMessageAsync();
            btnRefresh.Click += async (_, _) => await LoadMessagesAsync();
            btnLogout.Click += (_, _) =>
            {
                if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Close();
                }
            };
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                await _socketClient.ConnectAsync();
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối server: {ex.Message}");
            }
        }

        private async Task SendMessageAsync()
        {
            var receiver = txtReceiver.Text.Trim();
            var content = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(receiver) || string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Vui lòng nhập người nhận và nội dung tin nhắn.");
                return;
            }

            var label = cbLabel.SelectedIndex + 1;

            try
            {
                var response = await _socketClient.SendChatMessageAsync(_currentUser, receiver, content, label);
                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi gửi tin nhắn.");
                    return;
                }

                txtMessage.Clear();
                await LoadMessagesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi tin: {ex.Message}");
            }
        }

        private async Task LoadMessagesAsync()
        {
            try
            {
                var response = await _socketClient.GetMessagesAsync(_currentUser);
                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tải tin nhắn.");
                    return;
                }

                lstMessages.Items.Clear();
                foreach (var msg in response.Messages.OrderBy(m => m.Timestamp))
                {
                    var line = new StringBuilder();
                    line.Append($"[{msg.Timestamp:HH:mm:ss}] ");
                    line.Append($"{msg.Sender} -> {msg.Receiver} ");
                    line.Append($"(Label={msg.SecurityLabel}): ");
                    line.Append(msg.Content);
                    lstMessages.Items.Add(line.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải tin nhắn: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _socketClient.Dispose();
        }
    }
}

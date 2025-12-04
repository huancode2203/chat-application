using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatServer.Database;
using ChatServer.Services;

namespace ChatServer.Forms
{
    public partial class AdminPanelForm : Form
    {
        private readonly DbContext _dbContext;
        private readonly string _adminUsername;
        private readonly int _clearanceLevel;

        public AdminPanelForm(DbContext dbContext, string adminUsername, int clearanceLevel)
        {
            _dbContext = dbContext;
            _adminUsername = adminUsername;
            _clearanceLevel = clearanceLevel;
            InitializeComponent();
            SetupEventHandlers();
            lblCurrentUser.Text = $"Đăng nhập bởi: {adminUsername} (Level {clearanceLevel})";
            
            // Load initial data asynchronously
            _ = Task.Run(async () =>
            {
                await Task.Delay(100); // Small delay to ensure form is fully loaded
                if (InvokeRequired)
                {
                    Invoke(new Action(async () => await LoadUsersAsync()));
                }
                else
                {
                    await LoadUsersAsync();
                }
            });
        }

        private void SetupEventHandlers()
        {
            btnRefreshUsers.Click += async (_, _) => await LoadUsersAsync();
            btnCreateUser.Click += (_, _) => ShowCreateUserDialog();
            btnEditUser.Click += (_, _) => ShowEditUserDialog();
            btnDeleteUser.Click += async (_, _) => await DeleteUserAsync();
            btnBanUser.Click += async (_, _) => await BanUserAsync();
            btnUnbanUser.Click += async (_, _) => await UnbanUserAsync();

            btnRefreshConversations.Click += async (_, _) => await LoadConversationsAsync();
            btnDeleteConversation.Click += async (_, _) => await DeleteConversationAsync();
            btnViewMessages.Click += async (_, _) => await LoadConversationMessagesAsync();

            btnRefreshMessages.Click += async (_, _) => await LoadMessagesAsync();
            btnDeleteMessage.Click += async (_, _) => await DeleteMessageAsync();

            btnRefreshLogs.Click += async (_, _) => await LoadAuditLogsAsync();

            tabControl.SelectedIndexChanged += async (_, _) => await OnTabChangedAsync();
        }

        private async Task OnTabChangedAsync()
        {
            switch (tabControl.SelectedIndex)
            {
                case 0: await LoadUsersAsync(); break;
                case 1: await LoadConversationsAsync(); break;
                case 2: await LoadMessagesAsync(); break;
                case 3: await LoadAuditLogsAsync(); break;
            }
        }

        // Users Management
        private async Task LoadUsersAsync()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(async () => await LoadUsersAsync()));
                    return;
                }

                btnRefreshUsers.Enabled = false;
                var users = await Task.Run(() => _dbContext.GetAllUsersAsync().Result);
                
                dgvUsers.DataSource = users.Select(u => new
                {
                    u.Username,
                    u.Email,
                    u.Hovaten,
                    u.Phone,
                    u.ClearanceLevel,
                    IsBanned = u.IsBannedGlobal ? "Có" : "Không",
                    IsVerified = u.IsOtpVerified ? "Có" : "Không",
                    u.NgayTao
                }).ToList();
                
                btnRefreshUsers.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRefreshUsers.Enabled = true;
            }
        }

        private void ShowCreateUserDialog()
        {
            MessageBox.Show("Create user dialog - To be implemented", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowEditUserDialog()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một user để chỉnh sửa.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("Edit user dialog - To be implemented", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task DeleteUserAsync()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một user để xóa.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();
            if (string.IsNullOrEmpty(username))
                return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa user '{username}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    using var cmd = _dbContext.Connection.CreateCommand();
                    cmd.CommandText = "BEGIN SP_XOA_TAIKHOAN_TOAN_BO(:p_matk); END;";
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_matk", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = username });
                    await cmd.ExecuteNonQueryAsync();

                    await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_DELETE_USER", username, 0);
                    MessageBox.Show("User deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadUsersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task BanUserAsync()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một user để cấm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();
            if (string.IsNullOrEmpty(username))
                return;

            try
            {
                await _dbContext.BanUserGlobalAsync(username);
                await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_BAN_USER", username, 0);
                MessageBox.Show("User banned successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task UnbanUserAsync()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một user để bỏ cấm.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();
            if (string.IsNullOrEmpty(username))
                return;

            try
            {
                await _dbContext.UnbanUserGlobalAsync(username);
                await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_UNBAN_USER", username, 0);
                MessageBox.Show("User unbanned successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Conversations Management
        private async Task LoadConversationsAsync()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(async () => await LoadConversationsAsync()));
                    return;
                }

                btnRefreshConversations.Enabled = false;
                var conversations = await Task.Run(() => _dbContext.GetAllConversationsAsync().Result);
                
                dgvConversations.DataSource = conversations.Select(c => new
                {
                    c.Mactc,
                    c.Tenctc,
                    c.Maloaictc,
                    IsPrivate = c.IsPrivate ? "Có" : "Không",
                    c.Nguoiql,
                    c.MemberCount,
                    c.MessageCount,
                    c.NgayTao
                }).ToList();
                
                btnRefreshConversations.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRefreshConversations.Enabled = true;
            }
        }

        private async Task DeleteConversationAsync()
        {
            if (dgvConversations.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một conversation để xóa.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var mactc = dgvConversations.SelectedRows[0].Cells["Mactc"].Value?.ToString();
            if (string.IsNullOrEmpty(mactc))
                return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa conversation này?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    await _dbContext.DeleteConversationAsync(mactc);
                    await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_DELETE_CONVERSATION", mactc, 0);
                    MessageBox.Show("Conversation deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadConversationsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task LoadConversationMessagesAsync()
        {
            if (dgvConversations.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một conversation để xem messages.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var mactc = dgvConversations.SelectedRows[0].Cells["Mactc"].Value?.ToString();
            if (string.IsNullOrEmpty(mactc))
                return;

            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(async () => await LoadConversationMessagesAsync()));
                    return;
                }

                btnViewMessages.Enabled = false;
                btnRefreshMessages.Enabled = false;
                
                var messages = await Task.Run(() => _dbContext.GetConversationMessagesAdminAsync(mactc, 100).Result);
                
                dgvMessages.DataSource = messages.Select(m => new
                {
                    m.Matn,
                    m.Username,
                    m.Noidung,
                    SecurityLabel = m.SecurityLabel,
                    m.Ngaygui,
                    m.Maloaitn,
                    m.Matrangthai
                }).ToList();
                
                btnViewMessages.Enabled = true;
                btnRefreshMessages.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnViewMessages.Enabled = true;
                btnRefreshMessages.Enabled = true;
            }
        }

        // Messages Management
        private async Task LoadMessagesAsync()
        {
            await LoadConversationMessagesAsync();
        }

        private async Task DeleteMessageAsync()
        {
            if (dgvMessages.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một message để xóa.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var matnStr = dgvMessages.SelectedRows[0].Cells["Matn"].Value?.ToString();
            if (string.IsNullOrEmpty(matnStr) || !int.TryParse(matnStr, out var matn))
                return;

            if (MessageBox.Show("Bạn có chắc chắn muốn xóa message này?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    await _dbContext.DeleteMessageAsync(matn);
                    await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_DELETE_MESSAGE", matn.ToString(), 0);
                    MessageBox.Show("Message deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await LoadMessagesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Audit Logs
        private async Task LoadAuditLogsAsync()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(async () => await LoadAuditLogsAsync()));
                    return;
                }

                btnRefreshLogs.Enabled = false;
                var logs = await Task.Run(() => _dbContext.GetAuditLogsAsync(100).Result);
                
                dgvAuditLogs.DataSource = logs.Select(l => new
                {
                    l.LogId,
                    l.Matk,
                    l.Action,
                    l.Target,
                    SecurityLabel = l.SecurityLabel,
                    l.Timestamp
                }).ToList();
                
                btnRefreshLogs.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRefreshLogs.Enabled = true;
            }
        }
    }
}


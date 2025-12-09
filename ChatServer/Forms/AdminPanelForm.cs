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
                
                // Set MAC context với admin level để bypass VPD restrictions cho toàn bộ admin session
                try
                {
                    await _dbContext.SetMacContextAsync(adminUsername, clearanceLevel >= 4 ? clearanceLevel : 5);
                }
                catch { /* Ignore if procedure doesn't exist */ }
                
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
            btnUnlockAccount.Click += async (_, _) => await UnlockAccountAsync();

            btnRefreshConversations.Click += async (_, _) => await LoadConversationsAsync();
            btnDeleteConversation.Click += async (_, _) => await DeleteConversationAsync();
            btnViewMessages.Click += async (_, _) => await LoadConversationMessagesAsync();

            btnRefreshMessages.Click += async (_, _) => await LoadMessagesAsync();
            btnDeleteMessage.Click += async (_, _) => await DeleteMessageAsync();

            btnRefreshLogs.Click += async (_, _) => await LoadAuditLogsAsync();
            
            // Policy Management button - gắn trực tiếp
            btnPolicyManagement.Click += (_, _) => OpenPolicyManagement();
            
            // Encryption Test button
            btnEncryptionTest.Click += (_, _) => OpenEncryptionTest();

            tabControl.SelectedIndexChanged += async (_, _) => await OnTabChangedAsync();
        }

        private void OpenPolicyManagement()
        {
            try
            {
                using var policyForm = new PolicyManagementForm(_dbContext, _adminUsername);
                policyForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở Policy Management: {ex.Message}\n\nChi tiết: {ex.StackTrace}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    u.Matk,
                    u.Username,
                    u.Hovaten,
                    u.Chucvu,
                    u.Phongban,
                    u.Email,
                    u.Phone,
                    u.ClearanceLevel,
                    IsBanned = u.IsBannedGlobal ? "Có" : "Không",
                    IsVerified = u.IsOtpVerified ? "Có" : "Không",
                    AccountLocked = u.IsAccountLocked ? $"🔒 Khóa ({u.FailedLoginAttempts} lần)" : 
                                   (u.FailedLoginAttempts > 0 ? $"⚠️ {u.FailedLoginAttempts}/5 lần sai" : "✓ Bình thường"),
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
            try
            {
                using var createForm = new UserEditForm(_dbContext);
                if (createForm.ShowDialog(this) == DialogResult.OK)
                {
                    _ = LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở form tạo user: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowEditUserDialog()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một user để chỉnh sửa.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();
            if (string.IsNullOrEmpty(username))
                return;

            try
            {
                using var editForm = new UserEditForm(_dbContext, username);
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    _ = LoadUsersAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở form chỉnh sửa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa user '{username}'?\n\nLưu ý: Tất cả dữ liệu liên quan sẽ bị xóa!", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // Get MATK from username first
                    string matk = username;
                    using (var lookupCmd = _dbContext.Connection.CreateCommand())
                    {
                        lookupCmd.CommandText = "SELECT MATK FROM TAIKHOAN WHERE MATK = :p_value OR TENTK = :p_value";
                        lookupCmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_value", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = username });
                        var result = await lookupCmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                            matk = result.ToString()!;
                    }

                    // Delete from child tables in correct order (due to FK constraints)
                    var deleteCommands = new[] {
                        "DELETE FROM XACTHUCOTP WHERE MATK = :p",
                        "DELETE FROM AUDIT_LOGS WHERE MATK = :p",
                        "DELETE FROM ENCRYPTION_KEYS WHERE MATK = :p",
                        "DELETE FROM TINNHAN_ATTACH WHERE MATN IN (SELECT MATN FROM TINNHAN WHERE MATK = :p)",
                        "DELETE FROM ATTACHMENT WHERE MATK = :p",
                        "DELETE FROM TINNHAN WHERE MATK = :p",
                        "DELETE FROM THANHVIEN WHERE MATK = :p",
                        "DELETE FROM NGUOIDUNG WHERE MATK = :p",
                        "UPDATE CUOCTROCHUYEN SET NGUOIQL = NULL WHERE NGUOIQL = :p",
                        "DELETE FROM TAIKHOAN WHERE MATK = :p"
                    };

                    foreach (var sql in deleteCommands)
                    {
                        try
                        {
                            using var cmd = _dbContext.Connection.CreateCommand();
                            cmd.CommandText = sql;
                            cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = matk });
                            await cmd.ExecuteNonQueryAsync();
                        }
                        catch (Exception tableEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DeleteUser] Error: {tableEx.Message}");
                        }
                    }

                    await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_DELETE_USER", $"Deleted: {username} (MATK: {matk})", 0);
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

        private async Task UnlockAccountAsync()
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một user để mở khóa.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var matk = dgvUsers.SelectedRows[0].Cells["Matk"].Value?.ToString();
            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();
            if (string.IsNullOrEmpty(matk))
                return;

            try
            {
                await _dbContext.UnlockAccountAsync(matk);
                await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_UNLOCK_ACCOUNT", username ?? matk, 0);
                MessageBox.Show($"Đã mở khóa tài khoản {username}.\nUser có thể đăng nhập lại.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                
                // Call async method directly without Task.Run and .Result
                var conversations = await _dbContext.GetAllConversationsAsync();
                
                dgvConversations.DataSource = conversations.Select(c => new
                {
                    c.Mactc,
                    c.Tenctc,
                    c.Maloaictc,
                    IsPrivate = c.IsPrivate ? "Có" : "Không",
                    c.Nguoiql,
                    c.MemberCount,
                    c.MessageCount,
                    NgayTao = c.NgayTao.ToString("dd/MM/yyyy HH:mm")
                }).ToList();
                
                btnRefreshConversations.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải conversations:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                
                // Call async method directly without Task.Run and .Result
                var messages = await _dbContext.GetConversationMessagesAdminAsync(mactc, 100);
                
                dgvMessages.DataSource = messages.Select(m => new
                {
                    m.Matn,
                    m.Username,
                    Noidung = m.Noidung.Length > 100 ? m.Noidung.Substring(0, 100) + "..." : m.Noidung,
                    SecurityLabel = $"Mức {m.SecurityLabel}",
                    Ngaygui = m.Ngaygui.ToString("dd/MM/yyyy HH:mm:ss"),
                    m.Maloaitn,
                    m.Matrangthai
                }).ToList();
                
                btnViewMessages.Enabled = true;
                btnRefreshMessages.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải messages:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                
                var logs = await Task.Run(async () =>
                {
                    try
                    {
                        return await _dbContext.GetAuditLogsAsync(100);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetAuditLogsAsync error: {ex.Message}");
                        return new System.Collections.Generic.List<AuditLogInfo>();
                    }
                });
                
                if (logs != null && logs.Count > 0)
                {
                    dgvAuditLogs.DataSource = logs.Select(l => new
                    {
                        ID = l.LogId,
                        User = l.Matk ?? "N/A",
                        Action = l.Action ?? "N/A",
                        Target = l.Target ?? "N/A",
                        Level = l.SecurityLabel,
                        Time = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();
                }
                else
                {
                    dgvAuditLogs.DataSource = null;
                    MessageBox.Show("Không có audit log nào hoặc xảy ra lỗi khi tải.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                btnRefreshLogs.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải audit logs: {ex.Message}\n\nChi tiết: {ex.StackTrace}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRefreshLogs.Enabled = true;
            }
        }
    }
}


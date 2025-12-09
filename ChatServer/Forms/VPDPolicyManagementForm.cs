using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using ChatServer.Database;
using Oracle.ManagedDataAccess.Client;

namespace ChatServer.Forms
{
    /// <summary>
    /// Quản lý VPD (Virtual Private Database), RLS (Row Level Security) và FGA (Fine-Grained Auditing) Policies
    /// </summary>
    public partial class VPDPolicyManagementForm : Form
    {
        private readonly DbContext _dbContext;
        private readonly string _adminUsername;
        private TabControl tabControl;
        private TabPage tabVPD;
        private TabPage tabFGA;
        private TabPage tabAdminPolicy;
        private TabPage tabPolicyLogs;
        private DataGridView dgvVPDPolicies;
        private DataGridView dgvFGAPolicies;
        private DataGridView dgvAdminPolicies;
        private DataGridView dgvPolicyLogs;
        private Button btnRefresh;
        private Button btnClose;
        private Label lblTitle;

        public VPDPolicyManagementForm(DbContext dbContext, string adminUsername)
        {
            _dbContext = dbContext;
            _adminUsername = adminUsername;
            InitializeComponent();
            SetupUI();
            _ = LoadVPDPoliciesAsync();
        }

        private void SetupUI()
        {
            lblTitle = new Label
            {
                Text = "🔒 Quản lý Oracle VPD/RLS/FGA Policies",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 132, 255)
            };

            tabControl = new TabControl
            {
                Location = new Point(20, 60),
                Size = new Size(940, 520),
                Font = new Font("Segoe UI", 9F)
            };

            // VPD Tab
            tabVPD = new TabPage("VPD/RLS Policies");
            SetupVPDTab();
            tabControl.TabPages.Add(tabVPD);

            // FGA Tab
            tabFGA = new TabPage("FGA Audit Policies");
            SetupFGATab();
            tabControl.TabPages.Add(tabFGA);

            // Admin Policy Tab (Quản lý từ bảng ADMIN_POLICY)
            tabAdminPolicy = new TabPage("📋 Quản lý Policies");
            SetupAdminPolicyTab();
            tabControl.TabPages.Add(tabAdminPolicy);

            // Policy Change Logs Tab
            tabPolicyLogs = new TabPage("📜 Lịch sử thay đổi");
            SetupPolicyLogsTab();
            tabControl.TabPages.Add(tabPolicyLogs);

            btnRefresh = new Button
            {
                Text = "🔄 Tải lại",
                Size = new Size(120, 35),
                Location = new Point(720, 600),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshCurrentTabAsync();

            btnClose = new Button
            {
                Text = "Đóng",
                Size = new Size(100, 35),
                Location = new Point(860, 600),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { lblTitle, tabControl, btnRefresh, btnClose });
            this.AcceptButton = btnClose;
        }

        private void SetupVPDTab()
        {
            var lblInfo = new Label
            {
                Text = "VPD (Virtual Private Database) - Row Level Security Policies",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dgvVPDPolicies = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(900, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            var btnEnablePolicy = new Button
            {
                Text = "✓ Enable Policy",
                Size = new Size(130, 30),
                Location = new Point(10, 350),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEnablePolicy.FlatAppearance.BorderSize = 0;
            btnEnablePolicy.Click += async (s, e) => await ToggleVPDPolicyAsync(true);

            var btnDisablePolicy = new Button
            {
                Text = "✗ Disable Policy",
                Size = new Size(130, 30),
                Location = new Point(150, 350),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDisablePolicy.FlatAppearance.BorderSize = 0;
            btnDisablePolicy.Click += async (s, e) => await ToggleVPDPolicyAsync(false);

            var btnDropPolicy = new Button
            {
                Text = "🗑️ Drop Policy",
                Size = new Size(130, 30),
                Location = new Point(290, 350),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDropPolicy.FlatAppearance.BorderSize = 0;
            btnDropPolicy.Click += async (s, e) => await DropVPDPolicyAsync();

            var btnAddPolicy = new Button
            {
                Text = "➕ Add VPD Policy",
                Size = new Size(150, 30),
                Location = new Point(430, 350),
                BackColor = Color.FromArgb(0, 132, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddPolicy.FlatAppearance.BorderSize = 0;
            btnAddPolicy.Click += (s, e) => ShowAddVPDPolicyDialog();

            var txtPolicyInfo = new TextBox
            {
                Location = new Point(10, 390),
                Size = new Size(900, 80),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Text = "VPD Policies đang active sẽ tự động áp dụng Row Level Security.\n" +
                       "Example: TINNHAN_MAC_POLICY_V2 giới hạn user chỉ xem tin nhắn có SECURITYLABEL <= clearance level của họ.\n" +
                       "Enable/Disable policy để test, Drop để xóa hoàn toàn."
            };

            tabVPD.Controls.AddRange(new Control[] { 
                lblInfo, dgvVPDPolicies, 
                btnEnablePolicy, btnDisablePolicy, btnDropPolicy, btnAddPolicy,
                txtPolicyInfo
            });
        }

        private void SetupFGATab()
        {
            var lblInfo = new Label
            {
                Text = "FGA (Fine-Grained Auditing) - Audit Policies",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dgvFGAPolicies = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(900, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            var btnEnableFGA = new Button
            {
                Text = "✓ Enable FGA",
                Size = new Size(130, 30),
                Location = new Point(10, 350),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEnableFGA.FlatAppearance.BorderSize = 0;
            btnEnableFGA.Click += async (s, e) => await ToggleFGAPolicyAsync(true);

            var btnDisableFGA = new Button
            {
                Text = "✗ Disable FGA",
                Size = new Size(130, 30),
                Location = new Point(150, 350),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDisableFGA.FlatAppearance.BorderSize = 0;
            btnDisableFGA.Click += async (s, e) => await ToggleFGAPolicyAsync(false);

            var btnDropFGA = new Button
            {
                Text = "🗑️ Drop FGA",
                Size = new Size(130, 30),
                Location = new Point(290, 350),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDropFGA.FlatAppearance.BorderSize = 0;
            btnDropFGA.Click += async (s, e) => await DropFGAPolicyAsync();

            var btnAddFGA = new Button
            {
                Text = "➕ Add FGA Policy",
                Size = new Size(150, 30),
                Location = new Point(430, 350),
                BackColor = Color.FromArgb(0, 132, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddFGA.FlatAppearance.BorderSize = 0;
            btnAddFGA.Click += (s, e) => ShowAddFGAPolicyDialog();

            var txtFGAInfo = new TextBox
            {
                Location = new Point(10, 390),
                Size = new Size(900, 80),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Text = "FGA Policies tự động ghi log các operations (SELECT, INSERT, UPDATE, DELETE) vào FGA_LOG$.\n" +
                       "Dùng để audit và theo dõi ai đã truy cập dữ liệu nhạy cảm.\n" +
                       "View audit logs trong tab Audit Logs của Admin Panel."
            };

            tabFGA.Controls.AddRange(new Control[] { 
                lblInfo, dgvFGAPolicies,
                btnEnableFGA, btnDisableFGA, btnDropFGA, btnAddFGA,
                txtFGAInfo
            });
        }

        #region VPD Policy Operations

        private async Task LoadVPDPoliciesAsync()
        {
            try
            {
                btnRefresh.Enabled = false;

                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT OBJECT_NAME, POLICY_NAME, 
                           NVL(PF_OWNER, USER) || '.' || NVL(FUNCTION, 'N/A') AS POLICY_FUNCTION, 
                           ENABLE, 
                           NVL(STATIC_POLICY, 'N/A') AS STATIC_POLICY, 
                           NVL(POLICY_TYPE, 'DYNAMIC') AS POLICY_TYPE
                    FROM USER_POLICIES
                    ORDER BY OBJECT_NAME, POLICY_NAME";

                var policies = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    policies.Add(new
                    {
                        Object = reader.IsDBNull(0) ? "N/A" : reader.GetString(0),
                        PolicyName = reader.IsDBNull(1) ? "N/A" : reader.GetString(1),
                        Function = reader.IsDBNull(2) ? "N/A" : reader.GetString(2),
                        Enabled = reader.IsDBNull(3) ? "NO" : reader.GetString(3),
                        Static = reader.IsDBNull(4) ? "N/A" : reader.GetString(4),
                        Type = reader.IsDBNull(5) ? "N/A" : reader.GetString(5)
                    });
                }

                dgvVPDPolicies.DataSource = policies;
                btnRefresh.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải VPD policies: {ex.Message}\n\nLưu ý: Cần có quyền để xem USER_POLICIES.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnRefresh.Enabled = true;
            }
        }

        private async Task ToggleVPDPolicyAsync(bool enable)
        {
            if (dgvVPDPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một policy.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvVPDPolicies.SelectedRows[0];
            var objectName = row.Cells["Object"].Value?.ToString();
            var policyName = row.Cells["PolicyName"].Value?.ToString();

            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(policyName))
                return;

            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = $@"
                    BEGIN
                        DBMS_RLS.{(enable ? "ENABLE" : "DISABLE")}_POLICY(
                            object_schema => USER,
                            object_name   => :obj,
                            policy_name   => :pol
                        );
                    END;";
                cmd.Parameters.Add(new OracleParameter("obj", OracleDbType.Varchar2) { Value = objectName });
                cmd.Parameters.Add(new OracleParameter("pol", OracleDbType.Varchar2) { Value = policyName });

                await cmd.ExecuteNonQueryAsync();

                await _dbContext.WriteAuditLogAsync(_adminUsername, 
                    enable ? "VPD_ENABLE" : "VPD_DISABLE", 
                    $"{objectName}.{policyName}", 0);

                MessageBox.Show($"Policy {(enable ? "enabled" : "disabled")} thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadVPDPoliciesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DropVPDPolicyAsync()
        {
            if (dgvVPDPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một policy.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvVPDPolicies.SelectedRows[0];
            var objectName = row.Cells["Object"].Value?.ToString();
            var policyName = row.Cells["PolicyName"].Value?.ToString();

            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(policyName))
                return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn DROP policy '{policyName}' trên {objectName}?\n\nLưu ý: Không thể hoàn tác!",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = @"
                    BEGIN
                        DBMS_RLS.DROP_POLICY(
                            object_schema => USER,
                            object_name   => :obj,
                            policy_name   => :pol
                        );
                    END;";
                cmd.Parameters.Add(new OracleParameter("obj", OracleDbType.Varchar2) { Value = objectName });
                cmd.Parameters.Add(new OracleParameter("pol", OracleDbType.Varchar2) { Value = policyName });

                await cmd.ExecuteNonQueryAsync();

                await _dbContext.WriteAuditLogAsync(_adminUsername, "VPD_DROP", $"{objectName}.{policyName}", 0);

                MessageBox.Show("Policy đã được drop!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadVPDPoliciesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddVPDPolicyDialog()
        {
            using var dlg = new Form
            {
                Text = "Thêm VPD Policy",
                Size = new Size(650, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };

            var lblTitle = new Label { Text = "Thêm VPD/RLS Policy mới", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(20, 20), AutoSize = true, ForeColor = Color.FromArgb(0, 132, 255) };

            var lblTable = new Label { Text = "Bảng:", Location = new Point(20, 70), AutoSize = true };
            var cboTable = new ComboBox { Location = new Point(180, 67), Size = new Size(420, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTable.Items.AddRange(new object[] { "TINNHAN", "TAIKHOAN", "CUOCTROCHUYEN", "THANHVIEN", "ATTACHMENT" });
            cboTable.SelectedIndex = 0;

            var lblPolicyName = new Label { Text = "Tên Policy:", Location = new Point(20, 115), AutoSize = true };
            var txtPolicyName = new TextBox { Location = new Point(180, 112), Size = new Size(420, 30) };

            var lblFunction = new Label { Text = "Function:", Location = new Point(20, 160), AutoSize = true };
            var cboFunction = new ComboBox { Location = new Point(180, 157), Size = new Size(420, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cboFunction.Items.AddRange(new object[] { "TINNHAN_POLICY_FN_V2", "MAC_CTX_PKG.GET_POLICY_PREDICATE", "TINNHAN_MAC_POLICY_FN" });
            cboFunction.SelectedIndex = 0;

            var lblStatementTypes = new Label { Text = "Statement Types:", Location = new Point(20, 205), AutoSize = true };
            var chkSelect = new CheckBox { Text = "SELECT", Location = new Point(180, 203), AutoSize = true, Checked = true };
            var chkInsert = new CheckBox { Text = "INSERT", Location = new Point(280, 203), AutoSize = true };
            var chkUpdate = new CheckBox { Text = "UPDATE", Location = new Point(380, 203), AutoSize = true };
            var chkDelete = new CheckBox { Text = "DELETE", Location = new Point(480, 203), AutoSize = true };

            var chkEnable = new CheckBox { Text = "Enable ngay sau khi tạo", Location = new Point(180, 245), AutoSize = true, Checked = true };

            var lblInfo = new Label
            {
                Text = "Lưu ý: Policy function phải tồn tại trong database.\nFunction trả về WHERE clause để filter dữ liệu theo điều kiện bảo mật.\n\nVí dụ: RETURN 'SECURITYLABEL <= ' || SYS_CONTEXT('MAC_CTX', 'CLEARANCE');",
                Location = new Point(20, 290),
                Size = new Size(590, 80),
                ForeColor = Color.Gray
            };

            var btnAdd = new Button
            {
                Text = "Thêm Policy",
                Size = new Size(140, 40),
                Location = new Point(350, 420),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(120, 40),
                Location = new Point(500, 420),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10F)
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnAdd.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPolicyName.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên policy.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var stmtTypes = new List<string>();
                if (chkSelect.Checked) stmtTypes.Add("SELECT");
                if (chkInsert.Checked) stmtTypes.Add("INSERT");
                if (chkUpdate.Checked) stmtTypes.Add("UPDATE");
                if (chkDelete.Checked) stmtTypes.Add("DELETE");

                if (stmtTypes.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một statement type.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var cmd = _dbContext.Connection.CreateCommand();
                    cmd.CommandText = $@"
                        BEGIN
                            DBMS_RLS.ADD_POLICY(
                                object_schema   => USER,
                                object_name     => :obj,
                                policy_name     => :pol,
                                function_schema => USER,
                                policy_function => :func,
                                statement_types => :stmt,
                                enable          => {(chkEnable.Checked ? "TRUE" : "FALSE")}
                            );
                        END;";
                    cmd.Parameters.Add(new OracleParameter("obj", OracleDbType.Varchar2) { Value = cboTable.SelectedItem?.ToString() });
                    cmd.Parameters.Add(new OracleParameter("pol", OracleDbType.Varchar2) { Value = txtPolicyName.Text.Trim() });
                    cmd.Parameters.Add(new OracleParameter("func", OracleDbType.Varchar2) { Value = cboFunction.SelectedItem?.ToString() });
                    cmd.Parameters.Add(new OracleParameter("stmt", OracleDbType.Varchar2) { Value = string.Join(",", stmtTypes) });

                    await cmd.ExecuteNonQueryAsync();

                    await _dbContext.WriteAuditLogAsync(_adminUsername, "VPD_ADD", $"{cboTable.SelectedItem}.{txtPolicyName.Text}", 0);

                    MessageBox.Show("Đã thêm VPD Policy thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    await LoadVPDPoliciesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblTitle, lblTable, cboTable, lblPolicyName, txtPolicyName, lblFunction, cboFunction, lblStatementTypes, chkSelect, chkInsert, chkUpdate, chkDelete, chkEnable, lblInfo, btnAdd, btnCancel });
            dlg.CancelButton = btnCancel;
            dlg.ShowDialog(this);
        }

        #endregion

        #region FGA Policy Operations

        private async Task LoadFGAPoliciesAsync()
        {
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT OBJECT_NAME, POLICY_NAME, ENABLED, AUDIT_COLUMN, HANDLER_MODULE, STATEMENT_TYPES
                    FROM USER_AUDIT_POLICIES
                    ORDER BY OBJECT_NAME, POLICY_NAME";

                var policies = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    policies.Add(new
                    {
                        Object = reader.GetString(0),
                        PolicyName = reader.GetString(1),
                        Enabled = reader.GetString(2),
                        AuditColumn = reader.IsDBNull(3) ? "ALL" : reader.GetString(3),
                        Handler = reader.IsDBNull(4) ? "N/A" : reader.GetString(4),
                        StatementTypes = reader.IsDBNull(5) ? "N/A" : reader.GetString(5)
                    });
                }

                dgvFGAPolicies.DataSource = policies;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải FGA policies: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ToggleFGAPolicyAsync(bool enable)
        {
            if (dgvFGAPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một FGA policy.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvFGAPolicies.SelectedRows[0];
            var objectName = row.Cells["Object"].Value?.ToString();
            var policyName = row.Cells["PolicyName"].Value?.ToString();

            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(policyName))
                return;

            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = $@"
                    BEGIN
                        DBMS_FGA.{(enable ? "ENABLE" : "DISABLE")}_POLICY(
                            object_schema => USER,
                            object_name   => :obj,
                            policy_name   => :pol
                        );
                    END;";
                cmd.Parameters.Add(new OracleParameter("obj", OracleDbType.Varchar2) { Value = objectName });
                cmd.Parameters.Add(new OracleParameter("pol", OracleDbType.Varchar2) { Value = policyName });

                await cmd.ExecuteNonQueryAsync();

                await _dbContext.WriteAuditLogAsync(_adminUsername,
                    enable ? "FGA_ENABLE" : "FGA_DISABLE",
                    $"{objectName}.{policyName}", 0);

                MessageBox.Show($"FGA Policy {(enable ? "enabled" : "disabled")} thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadFGAPoliciesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DropFGAPolicyAsync()
        {
            if (dgvFGAPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một FGA policy.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvFGAPolicies.SelectedRows[0];
            var objectName = row.Cells["Object"].Value?.ToString();
            var policyName = row.Cells["PolicyName"].Value?.ToString();

            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(policyName))
                return;

            if (MessageBox.Show($"Bạn có chắc chắn muốn DROP FGA policy '{policyName}'?\n\nLưu ý: Không thể hoàn tác!",
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = @"
                    BEGIN
                        DBMS_FGA.DROP_POLICY(
                            object_schema => USER,
                            object_name   => :obj,
                            policy_name   => :pol
                        );
                    END;";
                cmd.Parameters.Add(new OracleParameter("obj", OracleDbType.Varchar2) { Value = objectName });
                cmd.Parameters.Add(new OracleParameter("pol", OracleDbType.Varchar2) { Value = policyName });

                await cmd.ExecuteNonQueryAsync();

                await _dbContext.WriteAuditLogAsync(_adminUsername, "FGA_DROP", $"{objectName}.{policyName}", 0);

                MessageBox.Show("FGA Policy đã được drop!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadFGAPoliciesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAddFGAPolicyDialog()
        {
            using var dlg = new Form
            {
                Text = "Thêm FGA Audit Policy",
                Size = new Size(500, 420),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            var lblTitle = new Label { Text = "Thêm FGA Audit Policy mới", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true, ForeColor = Color.FromArgb(0, 132, 255) };

            var lblTable = new Label { Text = "Bảng:", Location = new Point(20, 55), AutoSize = true };
            var cboTable = new ComboBox { Location = new Point(150, 52), Size = new Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTable.Items.AddRange(new object[] { "TINNHAN", "TAIKHOAN", "CUOCTROCHUYEN", "THANHVIEN", "ATTACHMENT", "AUDIT_LOGS" });
            cboTable.SelectedIndex = 0;

            var lblPolicyName = new Label { Text = "Tên Policy:", Location = new Point(20, 90), AutoSize = true };
            var txtPolicyName = new TextBox { Location = new Point(150, 87), Size = new Size(300, 25) };

            var lblAuditColumn = new Label { Text = "Audit Column:", Location = new Point(20, 125), AutoSize = true };
            var txtAuditColumn = new TextBox { Location = new Point(150, 122), Size = new Size(300, 25), Text = "NOIDUNG" };

            var lblCondition = new Label { Text = "Audit Condition:", Location = new Point(20, 160), AutoSize = true };
            var txtCondition = new TextBox { Location = new Point(150, 157), Size = new Size(300, 25), Text = "SECURITYLABEL >= 3" };

            var lblStatementTypes = new Label { Text = "Statement Types:", Location = new Point(20, 195), AutoSize = true };
            var chkSelect = new CheckBox { Text = "SELECT", Location = new Point(150, 193), AutoSize = true, Checked = true };
            var chkInsert = new CheckBox { Text = "INSERT", Location = new Point(230, 193), AutoSize = true };
            var chkUpdate = new CheckBox { Text = "UPDATE", Location = new Point(310, 193), AutoSize = true };
            var chkDelete = new CheckBox { Text = "DELETE", Location = new Point(390, 193), AutoSize = true };

            var chkEnable = new CheckBox { Text = "Enable ngay sau khi tạo", Location = new Point(150, 230), AutoSize = true, Checked = true };

            var lblInfo = new Label
            {
                Text = "FGA sẽ ghi log vào FGA_LOG$ khi condition được thỏa mãn.\nĐể xem logs: SELECT * FROM DBA_FGA_AUDIT_TRAIL",
                Location = new Point(20, 265),
                Size = new Size(440, 40),
                ForeColor = Color.Gray
            };

            var btnAdd = new Button
            {
                Text = "Thêm Policy",
                Size = new Size(120, 35),
                Location = new Point(250, 320),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(100, 35),
                Location = new Point(380, 320),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnAdd.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPolicyName.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên policy.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var stmtTypes = new List<string>();
                if (chkSelect.Checked) stmtTypes.Add("SELECT");
                if (chkInsert.Checked) stmtTypes.Add("INSERT");
                if (chkUpdate.Checked) stmtTypes.Add("UPDATE");
                if (chkDelete.Checked) stmtTypes.Add("DELETE");

                if (stmtTypes.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một statement type.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var cmd = _dbContext.Connection.CreateCommand();
                    cmd.CommandText = $@"
                        BEGIN
                            DBMS_FGA.ADD_POLICY(
                                object_schema   => USER,
                                object_name     => :obj,
                                policy_name     => :pol,
                                audit_column    => :col,
                                audit_condition => :cond,
                                statement_types => :stmt,
                                enable          => {(chkEnable.Checked ? "TRUE" : "FALSE")}
                            );
                        END;";
                    cmd.Parameters.Add(new OracleParameter("obj", OracleDbType.Varchar2) { Value = cboTable.SelectedItem?.ToString() });
                    cmd.Parameters.Add(new OracleParameter("pol", OracleDbType.Varchar2) { Value = txtPolicyName.Text.Trim() });
                    cmd.Parameters.Add(new OracleParameter("col", OracleDbType.Varchar2) { Value = string.IsNullOrWhiteSpace(txtAuditColumn.Text) ? DBNull.Value : txtAuditColumn.Text.Trim() });
                    cmd.Parameters.Add(new OracleParameter("cond", OracleDbType.Varchar2) { Value = string.IsNullOrWhiteSpace(txtCondition.Text) ? DBNull.Value : txtCondition.Text.Trim() });
                    cmd.Parameters.Add(new OracleParameter("stmt", OracleDbType.Varchar2) { Value = string.Join(",", stmtTypes) });

                    await cmd.ExecuteNonQueryAsync();

                    await _dbContext.WriteAuditLogAsync(_adminUsername, "FGA_ADD", $"{cboTable.SelectedItem}.{txtPolicyName.Text}", 0);

                    MessageBox.Show("Đã thêm FGA Policy thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    await LoadFGAPoliciesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblTitle, lblTable, cboTable, lblPolicyName, txtPolicyName, lblAuditColumn, txtAuditColumn, lblCondition, txtCondition, lblStatementTypes, chkSelect, chkInsert, chkUpdate, chkDelete, chkEnable, lblInfo, btnAdd, btnCancel });
            dlg.CancelButton = btnCancel;
            dlg.ShowDialog(this);
        }

        #endregion

        #region Admin Policy Operations (Bảng ADMIN_POLICY)

        private void SetupAdminPolicyTab()
        {
            var lblInfo = new Label
            {
                Text = "Quản lý tất cả Policies từ bảng ADMIN_POLICY (DAC, MAC, VPD, FGA, RBAC, OLS)",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dgvAdminPolicies = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(900, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            var btnAddPolicy = new Button
            {
                Text = "➕ Thêm Policy",
                Size = new Size(130, 30),
                Location = new Point(10, 350),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAddPolicy.FlatAppearance.BorderSize = 0;
            btnAddPolicy.Click += (s, e) => ShowAddAdminPolicyDialog();

            var btnEditPolicy = new Button
            {
                Text = "✏️ Sửa Policy",
                Size = new Size(130, 30),
                Location = new Point(150, 350),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEditPolicy.FlatAppearance.BorderSize = 0;
            btnEditPolicy.Click += (s, e) => ShowEditAdminPolicyDialog();

            var btnEnablePolicy = new Button
            {
                Text = "✓ Bật",
                Size = new Size(80, 30),
                Location = new Point(290, 350),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEnablePolicy.FlatAppearance.BorderSize = 0;
            btnEnablePolicy.Click += async (s, e) => await ToggleAdminPolicyAsync(true);

            var btnDisablePolicy = new Button
            {
                Text = "✗ Tắt",
                Size = new Size(80, 30),
                Location = new Point(380, 350),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDisablePolicy.FlatAppearance.BorderSize = 0;
            btnDisablePolicy.Click += async (s, e) => await ToggleAdminPolicyAsync(false);

            var btnDeletePolicy = new Button
            {
                Text = "🗑️ Xóa",
                Size = new Size(80, 30),
                Location = new Point(470, 350),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDeletePolicy.FlatAppearance.BorderSize = 0;
            btnDeletePolicy.Click += async (s, e) => await DeleteAdminPolicyAsync();

            var txtInfo = new TextBox
            {
                Location = new Point(10, 390),
                Size = new Size(900, 80),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Text = "Bảng ADMIN_POLICY lưu trữ metadata của tất cả policies trong hệ thống.\n" +
                       "Các loại policy: VPD (Row-Level Security), FGA (Fine-Grained Auditing), DAC (Discretionary Access Control),\n" +
                       "MAC (Mandatory Access Control), RBAC (Role-Based Access Control), OLS (Oracle Label Security)"
            };

            tabAdminPolicy.Controls.AddRange(new Control[] { 
                lblInfo, dgvAdminPolicies, 
                btnAddPolicy, btnEditPolicy, btnEnablePolicy, btnDisablePolicy, btnDeletePolicy,
                txtInfo
            });
        }

        private async Task LoadAdminPoliciesAsync()
        {
            try
            {
                var policies = await _dbContext.GetAdminPoliciesAsync();
                dgvAdminPolicies.DataSource = policies.Select(p => new
                {
                    ID = p.PolicyId,
                    Tên = p.PolicyName,
                    Loại = p.PolicyType,
                    Bảng = p.TableName,
                    MôTả = p.Description.Length > 50 ? p.Description.Substring(0, 50) + "..." : p.Description,
                    Function = p.PolicyFunction,
                    Statements = p.StatementTypes,
                    Trạng_Thái = p.IsEnabled ? "✓ Bật" : "✗ Tắt",
                    Tạo_Bởi = p.CreatedBy,
                    Ngày_Tạo = p.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải policies từ ADMIN_POLICY: {ex.Message}\n\nCó thể bảng ADMIN_POLICY chưa được tạo.", 
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowAddAdminPolicyDialog()
        {
            using var dlg = new Form
            {
                Text = "Thêm Policy mới",
                Size = new Size(550, 450),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            var lblTitle = new Label { Text = "Thêm Policy mới vào ADMIN_POLICY", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true, ForeColor = Color.FromArgb(0, 132, 255) };

            var lblName = new Label { Text = "Tên Policy:", Location = new Point(20, 55), AutoSize = true };
            var txtName = new TextBox { Location = new Point(150, 52), Size = new Size(350, 25) };

            var lblType = new Label { Text = "Loại Policy:", Location = new Point(20, 90), AutoSize = true };
            var cboType = new ComboBox { Location = new Point(150, 87), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboType.Items.AddRange(new object[] { "VPD", "FGA", "DAC", "MAC", "RBAC", "OLS" });
            cboType.SelectedIndex = 0;

            var lblTable = new Label { Text = "Bảng áp dụng:", Location = new Point(20, 125), AutoSize = true };
            var cboTable = new ComboBox { Location = new Point(150, 122), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTable.Items.AddRange(new object[] { "TINNHAN", "TAIKHOAN", "CUOCTROCHUYEN", "THANHVIEN", "ATTACHMENT", "AUDIT_LOGS", "NGUOIDUNG" });
            cboTable.SelectedIndex = 0;

            var lblDesc = new Label { Text = "Mô tả:", Location = new Point(20, 160), AutoSize = true };
            var txtDesc = new TextBox { Location = new Point(150, 157), Size = new Size(350, 60), Multiline = true };

            var lblFunc = new Label { Text = "Policy Function:", Location = new Point(20, 230), AutoSize = true };
            var txtFunc = new TextBox { Location = new Point(150, 227), Size = new Size(350, 25) };

            var lblStmt = new Label { Text = "Statement Types:", Location = new Point(20, 265), AutoSize = true };
            var txtStmt = new TextBox { Location = new Point(150, 262), Size = new Size(350, 25), Text = "SELECT,INSERT,UPDATE,DELETE" };

            var btnAdd = new Button
            {
                Text = "Thêm",
                Size = new Size(100, 35),
                Location = new Point(300, 350),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(100, 35),
                Location = new Point(410, 350),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnAdd.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên policy.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var policyId = await _dbContext.CreateAdminPolicyAsync(
                        txtName.Text.Trim(),
                        cboType.SelectedItem?.ToString() ?? "VPD",
                        cboTable.SelectedItem?.ToString() ?? "TINNHAN",
                        txtDesc.Text.Trim(),
                        string.IsNullOrWhiteSpace(txtFunc.Text) ? null : txtFunc.Text.Trim(),
                        string.IsNullOrWhiteSpace(txtStmt.Text) ? null : txtStmt.Text.Trim(),
                        _adminUsername
                    );

                    await _dbContext.LogPolicyChangeAsync(policyId, "CREATE", _adminUsername, null, txtName.Text, "Tạo policy mới từ Admin Panel");
                    await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_CREATE_POLICY", txtName.Text, 0);

                    MessageBox.Show("Đã thêm policy thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    await LoadAdminPoliciesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblTitle, lblName, txtName, lblType, cboType, lblTable, cboTable, lblDesc, txtDesc, lblFunc, txtFunc, lblStmt, txtStmt, btnAdd, btnCancel });
            dlg.CancelButton = btnCancel;
            dlg.ShowDialog(this);
        }

        private void ShowEditAdminPolicyDialog()
        {
            if (dgvAdminPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một policy để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvAdminPolicies.SelectedRows[0];
            var policyIdStr = row.Cells["ID"].Value?.ToString();
            if (!int.TryParse(policyIdStr, out var policyId)) return;

            var policyName = row.Cells["Tên"].Value?.ToString() ?? "";
            var description = row.Cells["MôTả"].Value?.ToString() ?? "";
            var statements = row.Cells["Statements"].Value?.ToString() ?? "";

            using var dlg = new Form
            {
                Text = "Sửa Policy",
                Size = new Size(450, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            var lblTitle = new Label { Text = $"Sửa Policy: {policyName}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(20, 15), AutoSize = true, ForeColor = Color.FromArgb(0, 132, 255) };

            var lblDesc = new Label { Text = "Mô tả:", Location = new Point(20, 60), AutoSize = true };
            var txtDesc = new TextBox { Location = new Point(150, 57), Size = new Size(260, 60), Multiline = true, Text = description };

            var lblStmt = new Label { Text = "Statement Types:", Location = new Point(20, 130), AutoSize = true };
            var txtStmt = new TextBox { Location = new Point(150, 127), Size = new Size(260, 25), Text = statements };

            var btnSave = new Button
            {
                Text = "Lưu",
                Size = new Size(100, 35),
                Location = new Point(200, 200),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(100, 35),
                Location = new Point(310, 200),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            btnSave.Click += async (s, e) =>
            {
                try
                {
                    await _dbContext.UpdateAdminPolicyAsync(policyId, txtDesc.Text.Trim(), txtStmt.Text.Trim(), null);
                    await _dbContext.LogPolicyChangeAsync(policyId, "UPDATE", _adminUsername, description, txtDesc.Text, "Cập nhật từ Admin Panel");
                    await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_UPDATE_POLICY", policyName, 0);

                    MessageBox.Show("Đã cập nhật policy!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    await LoadAdminPoliciesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { lblTitle, lblDesc, txtDesc, lblStmt, txtStmt, btnSave, btnCancel });
            dlg.CancelButton = btnCancel;
            dlg.ShowDialog(this);
        }

        private async Task ToggleAdminPolicyAsync(bool enable)
        {
            if (dgvAdminPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một policy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvAdminPolicies.SelectedRows[0];
            var policyIdStr = row.Cells["ID"].Value?.ToString();
            if (!int.TryParse(policyIdStr, out var policyId)) return;

            var policyName = row.Cells["Tên"].Value?.ToString() ?? "";

            try
            {
                await _dbContext.ToggleAdminPolicyAsync(policyId, enable);
                await _dbContext.LogPolicyChangeAsync(policyId, enable ? "ENABLE" : "DISABLE", _adminUsername, null, null, "Thay đổi trạng thái từ Admin Panel");
                await _dbContext.WriteAuditLogAsync(_adminUsername, enable ? "ADMIN_ENABLE_POLICY" : "ADMIN_DISABLE_POLICY", policyName, 0);

                MessageBox.Show($"Policy đã được {(enable ? "bật" : "tắt")}!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadAdminPoliciesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DeleteAdminPolicyAsync()
        {
            if (dgvAdminPolicies.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một policy để xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row = dgvAdminPolicies.SelectedRows[0];
            var policyIdStr = row.Cells["ID"].Value?.ToString();
            if (!int.TryParse(policyIdStr, out var policyId)) return;

            var policyName = row.Cells["Tên"].Value?.ToString() ?? "";

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa policy '{policyName}'?\n\nLưu ý: Không thể hoàn tác!", 
                "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                await _dbContext.LogPolicyChangeAsync(policyId, "DELETE", _adminUsername, policyName, null, "Xóa từ Admin Panel");
                await _dbContext.DeleteAdminPolicyAsync(policyId);
                await _dbContext.WriteAuditLogAsync(_adminUsername, "ADMIN_DELETE_POLICY", policyName, 0);

                MessageBox.Show("Policy đã được xóa!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadAdminPoliciesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Policy Change Logs

        private void SetupPolicyLogsTab()
        {
            var lblInfo = new Label
            {
                Text = "Lịch sử thay đổi Policies (POLICY_CHANGE_LOG)",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            dgvPolicyLogs = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(900, 400),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            var txtInfo = new TextBox
            {
                Location = new Point(10, 450),
                Size = new Size(900, 30),
                Multiline = false,
                ReadOnly = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Text = "Theo dõi tất cả các thay đổi policy: CREATE, UPDATE, DELETE, ENABLE, DISABLE"
            };

            tabPolicyLogs.Controls.AddRange(new Control[] { lblInfo, dgvPolicyLogs, txtInfo });
        }

        private async Task LoadPolicyLogsAsync()
        {
            try
            {
                var logs = await _dbContext.GetPolicyChangeLogsAsync();
                dgvPolicyLogs.DataSource = logs.Select(l => new
                {
                    ID = l.LogId,
                    Policy = l.PolicyName,
                    Hành_Động = l.Action,
                    Người_Thực_Hiện = l.ChangedBy,
                    Thời_Gian = l.ChangedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    Giá_Trị_Cũ = l.OldValue.Length > 30 ? l.OldValue.Substring(0, 30) + "..." : l.OldValue,
                    Giá_Trị_Mới = l.NewValue.Length > 30 ? l.NewValue.Substring(0, 30) + "..." : l.NewValue,
                    Lý_Do = l.Reason
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải policy logs: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        private async Task RefreshCurrentTabAsync()
        {
            if (tabControl.SelectedTab == tabVPD)
            {
                await LoadVPDPoliciesAsync();
            }
            else if (tabControl.SelectedTab == tabFGA)
            {
                await LoadFGAPoliciesAsync();
            }
            else if (tabControl.SelectedTab == tabAdminPolicy)
            {
                await LoadAdminPoliciesAsync();
            }
            else if (tabControl.SelectedTab == tabPolicyLogs)
            {
                await LoadPolicyLogsAsync();
            }
        }
    }
}

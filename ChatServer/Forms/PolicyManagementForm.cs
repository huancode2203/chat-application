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
    /// Policy Management Form V2 - Dễ sử dụng hơn với preset policies
    /// </summary>
    public class PolicyManagementForm : Form
    {
        private readonly DbContext _dbContext;
        private readonly string _adminUsername;

        // Controls
        private TabControl tabControl = null!;
        private DataGridView dgvVPD = null!, dgvFGA = null!, dgvMAC = null!;
        private Label lblStatus = null!;
        private ListBox lstPresetVPD = null!, lstPresetFGA = null!;

        public PolicyManagementForm(DbContext dbContext, string adminUsername)
        {
            _dbContext = dbContext;
            _adminUsername = adminUsername;
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "🔐 Oracle Security Policy Manager";
            this.Size = new Size(1300, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9F);

            var lblTitle = new Label
            {
                Text = "🔐 Oracle Security Policy Manager",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Location = new Point(20, 12),
                AutoSize = true
            };

            tabControl = new TabControl
            {
                Location = new Point(20, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 120),
                Font = new Font("Segoe UI", 10F)
            };
            tabControl.SelectedIndexChanged += async (s, e) => await LoadCurrentTabAsync();

            // ===== VPD TAB =====
            CreateVPDTab();
            
            // ===== FGA TAB =====
            CreateFGATab();
            
            // ===== MAC TAB =====
            CreateMACTab();
            
            // ===== HELP TAB =====
            CreateHelpTab();

            // Bottom controls (anchored to bottom)
            lblStatus = new Label { Text = "Sẵn sàng", AutoSize = true, ForeColor = Color.Gray, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            var btnRefresh = CreateBtn("🔄 Tải lại", Color.FromArgb(0, 123, 255), Point.Empty);
            btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnRefresh.Click += async (s, e) => await LoadCurrentTabAsync();
            var btnClose = CreateBtn("Đóng", Color.FromArgb(108, 117, 125), Point.Empty);
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.DialogResult = DialogResult.OK;

            this.Controls.AddRange(new Control[] { lblTitle, tabControl, lblStatus, btnRefresh, btnClose });
            this.AcceptButton = btnClose;
            
            // Position bottom controls after adding to form
            this.Resize += (s, e) => RepositionBottomControls(lblStatus, btnRefresh, btnClose);
            this.Shown += async (s, e) => { RepositionBottomControls(lblStatus, btnRefresh, btnClose); await LoadCurrentTabAsync(); };
        }

        #region ===== VPD TAB =====
        private void CreateVPDTab()
        {
            var tabVPD = new TabPage("🛡️ VPD / RLS");
            tabVPD.BackColor = Color.White;

            // Info panel - Dock top, auto width
            var panelInfo = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(232, 245, 233) };
            panelInfo.Controls.Add(new Label
            {
                Text = "🛡️ VPD (Virtual Private Database) / RLS (Row Level Security)\n" +
                       "• Tự động thêm WHERE vào mọi query  • User chỉ thấy data phù hợp với quyền  • Package: DBMS_RLS",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 0),
                Font = new Font("Segoe UI", 9F)
            });

            // Main content panel
            var panelContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Split container for left/right
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 700,
                SplitterWidth = 8
            };

            // LEFT: Policies hiện có
            var lblCurrent = new Label { Text = "📋 Policies hiện có:", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            dgvVPD = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            
            var panelVPDButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 0) };
            var btnEnable = CreateBtn("✓ Enable", Color.FromArgb(40, 167, 69), Point.Empty);
            btnEnable.Click += async (s, e) => await ToggleVPDAsync(true);
            var btnDisable = CreateBtn("✗ Disable", Color.FromArgb(255, 193, 7), Point.Empty, Color.Black);
            btnDisable.Click += async (s, e) => await ToggleVPDAsync(false);
            var btnDrop = CreateBtn("🗑️ Xóa", Color.FromArgb(220, 53, 69), Point.Empty);
            btnDrop.Click += async (s, e) => await DropVPDAsync();
            var btnCustom = CreateBtn("➕ Tùy chỉnh", Color.FromArgb(0, 123, 255), Point.Empty);
            btnCustom.Click += (s, e) => ShowCustomVPDDialog();
            panelVPDButtons.Controls.AddRange(new Control[] { btnEnable, btnDisable, btnDrop, btnCustom });
            
            splitContainer.Panel1.Controls.Add(dgvVPD);
            splitContainer.Panel1.Controls.Add(panelVPDButtons);
            splitContainer.Panel1.Controls.Add(lblCurrent);

            // RIGHT: Preset policies
            var lblPreset = new Label { Text = "⚡ Thêm nhanh (click đôi):", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lstPresetVPD = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };
            lstPresetVPD.Items.AddRange(new object[]
            {
                "🔒 TINNHAN - Lọc theo SecurityLabel (MAC)",
                "🔒 TINNHAN - Chỉ xem tin trong cuộc chat của mình",
                "🔒 CUOCTROCHUYEN - Chỉ xem cuộc chat mình tham gia",
                "🔒 THANHVIEN - Chỉ xem thành viên trong chat của mình",
                "🔒 TAIKHOAN - Ẩn thông tin user khác (ngoại trừ Admin)"
            });
            lstPresetVPD.DoubleClick += async (s, e) => await AddPresetVPDAsync();
            
            splitContainer.Panel2.Controls.Add(lstPresetVPD);
            splitContainer.Panel2.Controls.Add(lblPreset);

            panelContent.Controls.Add(splitContainer);
            tabVPD.Controls.Add(panelContent);
            tabVPD.Controls.Add(panelInfo);
            tabControl.TabPages.Add(tabVPD);
        }

        private async Task AddPresetVPDAsync()
        {
            if (lstPresetVPD.SelectedIndex < 0) return;
            
            var presets = new[]
            {
                ("TINNHAN", "VPD_TINNHAN_MAC", "TINNHAN_POLICY_FN_V2", "SELECT"),
                ("TINNHAN", "VPD_TINNHAN_OWNER", "TINNHAN_OWNER_POLICY_FN", "SELECT"),
                ("CUOCTROCHUYEN", "VPD_CUOCTROCHUYEN_MEMBER", "CUOCTROCHUYEN_POLICY_FN", "SELECT"),
                ("THANHVIEN", "VPD_THANHVIEN_MEMBER", "THANHVIEN_POLICY_FN", "SELECT"),
                ("TAIKHOAN", "VPD_TAIKHOAN_PRIVACY", "TAIKHOAN_POLICY_FN", "SELECT")
            };
            
            var preset = presets[lstPresetVPD.SelectedIndex];
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"BEGIN DBMS_RLS.ADD_POLICY(
                    object_schema => USER, object_name => :obj, policy_name => :pol,
                    function_schema => USER, policy_function => :func, statement_types => :stmt, enable => TRUE); END;";
                cmd.Parameters.Add(new OracleParameter("obj", preset.Item1));
                cmd.Parameters.Add(new OracleParameter("pol", preset.Item2));
                cmd.Parameters.Add(new OracleParameter("func", preset.Item3));
                cmd.Parameters.Add(new OracleParameter("stmt", preset.Item4));
                await cmd.ExecuteNonQueryAsync();
                await _dbContext.WriteAuditLogAsync(_adminUsername, "VPD_ADD_PRESET", $"{preset.Item1}.{preset.Item2}", 0);
                MessageBox.Show($"✓ Đã thêm VPD Policy: {preset.Item2}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadVPDAsync();
            }
            catch (OracleException ex) when (ex.Number == 28101) // Policy already exists
            {
                MessageBox.Show($"Policy '{preset.Item2}' đã tồn tại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (OracleException ex) when (ex.Number == 904 || ex.Number == 6550) // Function not found
            {
                MessageBox.Show($"Function '{preset.Item3}' chưa tồn tại!\n\nCần chạy script SQL để tạo function trước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region ===== FGA TAB =====
        private void CreateFGATab()
        {
            var tabFGA = new TabPage("📋 FGA / Audit");
            tabFGA.BackColor = Color.White;

            // Info panel - Dock top
            var panelInfo = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(255, 243, 224) };
            panelInfo.Controls.Add(new Label
            {
                Text = "📋 FGA (Fine-Grained Auditing) - Ghi log truy cập dữ liệu\n" +
                       "• Audit khi user SELECT/UPDATE/DELETE data nhạy cảm  • Xem logs: DBA_FGA_AUDIT_TRAIL  • Package: DBMS_FGA",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 0),
                Font = new Font("Segoe UI", 9F)
            });

            // Main content panel
            var panelContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // Split container for left/right
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 700,
                SplitterWidth = 8
            };

            // LEFT: Policies hiện có
            var lblCurrent = new Label { Text = "📋 FGA Policies hiện có:", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            dgvFGA = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            var panelFGAButtons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 5, 0, 0) };
            var btnEnable = CreateBtn("✓ Enable", Color.FromArgb(40, 167, 69), Point.Empty);
            btnEnable.Click += async (s, e) => await ToggleFGAAsync(true);
            var btnDisable = CreateBtn("✗ Disable", Color.FromArgb(255, 193, 7), Point.Empty, Color.Black);
            btnDisable.Click += async (s, e) => await ToggleFGAAsync(false);
            var btnDrop = CreateBtn("🗑️ Xóa", Color.FromArgb(220, 53, 69), Point.Empty);
            btnDrop.Click += async (s, e) => await DropFGAAsync();
            var btnCustom = CreateBtn("➕ Tùy chỉnh", Color.FromArgb(0, 123, 255), Point.Empty);
            btnCustom.Click += (s, e) => ShowCustomFGADialog();
            var btnViewLogs = CreateBtn("📄 Xem Logs", Color.FromArgb(108, 117, 125), Point.Empty);
            btnViewLogs.Size = new Size(120, 32);
            btnViewLogs.Click += async (s, e) => await ViewFGALogsAsync();
            panelFGAButtons.Controls.AddRange(new Control[] { btnEnable, btnDisable, btnDrop, btnCustom, btnViewLogs });

            splitContainer.Panel1.Controls.Add(dgvFGA);
            splitContainer.Panel1.Controls.Add(panelFGAButtons);
            splitContainer.Panel1.Controls.Add(lblCurrent);

            // RIGHT: Preset policies
            var lblPreset = new Label { Text = "⚡ Thêm nhanh (click đôi):", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lstPresetFGA = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };
            lstPresetFGA.Items.AddRange(new object[]
            {
                "📝 TINNHAN - Audit xem tin CONFIDENTIAL (Level>=3)",
                "📝 TINNHAN - Audit UPDATE/DELETE tin nhắn",
                "📝 TAIKHOAN - Audit xem thông tin mật khẩu",
                "📝 TAIKHOAN - Audit thay đổi quyền user",
                "📝 CUOCTROCHUYEN - Audit tạo/xóa cuộc chat"
            });
            lstPresetFGA.DoubleClick += async (s, e) => await AddPresetFGAAsync();

            splitContainer.Panel2.Controls.Add(lstPresetFGA);
            splitContainer.Panel2.Controls.Add(lblPreset);

            panelContent.Controls.Add(splitContainer);
            tabFGA.Controls.Add(panelContent);
            tabFGA.Controls.Add(panelInfo);
            tabControl.TabPages.Add(tabFGA);
        }

        private async Task AddPresetFGAAsync()
        {
            if (lstPresetFGA.SelectedIndex < 0) return;
            
            var presets = new[]
            {
                ("TINNHAN", "FGA_TINNHAN_CONFIDENTIAL", "NOIDUNG", "NVL(SECURITYLABEL,1) >= 3", "SELECT"),
                ("TINNHAN", "FGA_TINNHAN_MODIFY", "NOIDUNG", "1=1", "UPDATE,DELETE"),
                ("TAIKHOAN", "FGA_TAIKHOAN_PASSWORD", "MATKHAU", "1=1", "SELECT"),
                ("TAIKHOAN", "FGA_TAIKHOAN_ROLE", "MAVAITRO,CLEARANCELEVEL", "1=1", "UPDATE"),
                ("CUOCTROCHUYEN", "FGA_CUOCTROCHUYEN_MANAGE", "TENCTC", "1=1", "INSERT,DELETE")
            };
            
            var preset = presets[lstPresetFGA.SelectedIndex];
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.BindByName = true;
                cmd.CommandText = @"BEGIN DBMS_FGA.ADD_POLICY(
                    object_schema => USER, object_name => :obj, policy_name => :pol,
                    audit_column => :col, audit_condition => :cond, statement_types => :stmt, enable => TRUE); END;";
                cmd.Parameters.Add(new OracleParameter("obj", preset.Item1));
                cmd.Parameters.Add(new OracleParameter("pol", preset.Item2));
                cmd.Parameters.Add(new OracleParameter("col", preset.Item3));
                cmd.Parameters.Add(new OracleParameter("cond", preset.Item4));
                cmd.Parameters.Add(new OracleParameter("stmt", preset.Item5));
                await cmd.ExecuteNonQueryAsync();
                await _dbContext.WriteAuditLogAsync(_adminUsername, "FGA_ADD_PRESET", $"{preset.Item1}.{preset.Item2}", 0);
                MessageBox.Show($"✓ Đã thêm FGA Policy: {preset.Item2}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadFGAAsync();
            }
            catch (OracleException ex) when (ex.Number == 28101) // Policy already exists
            {
                MessageBox.Show($"Policy '{preset.Item2}' đã tồn tại!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region ===== MAC TAB =====
        private void CreateMACTab()
        {
            var tabMAC = new TabPage("🏷️ MAC / Labels");
            tabMAC.BackColor = Color.White;

            var panelInfo = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(232, 234, 246) };
            panelInfo.Controls.Add(new Label
            {
                Text = "🏷️ MAC (Mandatory Access Control) - Bảo mật theo labels/levels\n" +
                       "• Bell-LaPadula: No Read Up, No Write Down  • TAIKHOAN.CLEARANCELEVEL vs TINNHAN.SECURITYLABEL",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 8, 10, 0),
                Font = new Font("Segoe UI", 9F)
            });

            var panelContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 500,
                SplitterWidth = 8
            };

            dgvMAC = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            splitContainer.Panel1.Controls.Add(dgvMAC);

            var txtLevels = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.FromArgb(171, 178, 191),
                Text = @"╔═════════════════════════════════════╗
║     SECURITY LEVELS (Bell-LaPadula)  ║
╠═════════════════════════════════════╣
║  Level 1: UNCLASSIFIED  - Công khai  ║
║  Level 2: INTERNAL      - Nội bộ     ║
║  Level 3: CONFIDENTIAL  - Bảo mật    ║
║  Level 4: SECRET        - Bí mật     ║
║  Level 5: TOP SECRET    - Tối mật    ║
╠═════════════════════════════════════╣
║  User (Clearance=X) chỉ đọc được    ║
║  tin nhắn có SecurityLabel <= X      ║
╚═════════════════════════════════════╝"
            };
            splitContainer.Panel2.Controls.Add(txtLevels);

            panelContent.Controls.Add(splitContainer);
            tabMAC.Controls.Add(panelContent);
            tabMAC.Controls.Add(panelInfo);
            tabControl.TabPages.Add(tabMAC);
        }
        #endregion

        #region ===== HELP TAB =====
        private void CreateHelpTab()
        {
            var tabHelp = new TabPage("❓ Hướng dẫn");
            var txtHelp = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9.5F),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(220, 220, 220),
                Text = GetHelpText()
            };
            tabHelp.Controls.Add(txtHelp);
            tabControl.TabPages.Add(tabHelp);
        }
        #endregion

        #region ===== DATA LOADING =====
        private async Task LoadCurrentTabAsync()
        {
            switch (tabControl.SelectedIndex)
            {
                case 0: await LoadVPDAsync(); break;
                case 1: await LoadFGAAsync(); break;
                case 2: await LoadMACAsync(); break;
            }
        }

        private async Task LoadVPDAsync()
        {
            try
            {
                lblStatus.Text = "Loading VPD...";
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = "SELECT OBJECT_NAME, POLICY_NAME, NVL(FUNCTION,'N/A') AS FUNC, ENABLE FROM USER_POLICIES ORDER BY OBJECT_NAME";
                var list = new List<object>();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new { Table = r.GetString(0), Policy = r.GetString(1), Function = r.GetString(2), Status = r.GetString(3) == "YES" ? "✓ Enabled" : "✗ Disabled" });
                dgvVPD.DataSource = list;
                lblStatus.Text = $"VPD: {list.Count} policies";
            }
            catch (Exception ex) { lblStatus.Text = $"Error: {ex.Message}"; MessageBox.Show($"Lỗi load VPD:\n{ex.Message}", "Error"); }
        }

        private async Task LoadFGAAsync()
        {
            try
            {
                lblStatus.Text = "Loading FGA...";
                using var cmd = _dbContext.Connection.CreateCommand();
                var list = new List<object>();
                
                // Try DBA view first (if user has DBA role)
                try
                {
                    cmd.CommandText = @"
                        SELECT OBJECT_NAME, POLICY_NAME, ENABLED, 
                               NVL(AUDIT_COLUMN,'ALL') AS AUDIT_COL, 
                               NVL(STATEMENT_TYPES,'SELECT') AS STMT_TYPES
                        FROM DBA_AUDIT_POLICIES 
                        WHERE OBJECT_SCHEMA = USER
                        ORDER BY OBJECT_NAME";
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                        list.Add(new { Table = r.GetString(0), Policy = r.GetString(1), Status = r.GetString(2) == "YES" ? "✓ Enabled" : "✗ Disabled", Column = r.GetString(3), Statements = r.GetString(4) });
                }
                catch
                {
                    // Fallback to checking ADMIN_POLICY table for FGA records
                    list.Clear();
                    using var cmd2 = _dbContext.Connection.CreateCommand();
                    cmd2.CommandText = @"
                        SELECT TABLE_NAME, POLICY_NAME, IS_ENABLED, 
                               NVL(STATEMENT_TYPES,'SELECT') AS STMT_TYPES,
                               NVL(DESCRIPTION,'') AS DESCR
                        FROM ADMIN_POLICY 
                        WHERE POLICY_TYPE = 'FGA'
                        ORDER BY TABLE_NAME";
                    using var r2 = await cmd2.ExecuteReaderAsync();
                    while (await r2.ReadAsync())
                        list.Add(new { 
                            Table = r2.GetString(0), 
                            Policy = r2.GetString(1), 
                            Status = r2.GetInt32(2) == 1 ? "✓ Enabled" : "✗ Disabled", 
                            Column = "ALL",
                            Statements = r2.GetString(3) 
                        });
                }
                
                dgvFGA.DataSource = list;
                lblStatus.Text = $"FGA: {list.Count} policies";
            }
            catch (Exception ex) { lblStatus.Text = $"Error: {ex.Message}"; }
        }

        private async Task LoadMACAsync()
        {
            try
            {
                lblStatus.Text = "Loading MAC...";
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT 1 AS LVL, 'UNCLASSIFIED' AS NAME, (SELECT COUNT(*) FROM TAIKHOAN WHERE NVL(CLEARANCELEVEL,1)=1) AS CNT FROM DUAL UNION ALL
                    SELECT 2, 'INTERNAL', (SELECT COUNT(*) FROM TAIKHOAN WHERE CLEARANCELEVEL=2) FROM DUAL UNION ALL
                    SELECT 3, 'CONFIDENTIAL', (SELECT COUNT(*) FROM TAIKHOAN WHERE CLEARANCELEVEL=3) FROM DUAL UNION ALL
                    SELECT 4, 'SECRET', (SELECT COUNT(*) FROM TAIKHOAN WHERE CLEARANCELEVEL=4) FROM DUAL UNION ALL
                    SELECT 5, 'TOP SECRET', (SELECT COUNT(*) FROM TAIKHOAN WHERE CLEARANCELEVEL=5) FROM DUAL";
                using var r = await cmd.ExecuteReaderAsync();
                var list = new List<object>();
                while (await r.ReadAsync())
                    list.Add(new { Level = r.GetInt32(0), Name = r.GetString(1), Users = r.GetInt32(2) });
                dgvMAC.DataSource = list;
                lblStatus.Text = "MAC: Loaded";
            }
            catch (Exception ex) { lblStatus.Text = $"Error: {ex.Message}"; }
        }

        #endregion

        #region ===== POLICY ACTIONS =====
        private async Task ToggleVPDAsync(bool enable)
        {
            if (dgvVPD.SelectedRows.Count == 0) { MessageBox.Show("Chọn một policy"); return; }
            var tbl = dgvVPD.SelectedRows[0].Cells["Table"].Value?.ToString();
            var pol = dgvVPD.SelectedRows[0].Cells["Policy"].Value?.ToString();
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = $"BEGIN DBMS_RLS.{(enable ? "ENABLE" : "DISABLE")}_POLICY(USER, :t, :p); END;";
                cmd.Parameters.Add(new OracleParameter("t", tbl));
                cmd.Parameters.Add(new OracleParameter("p", pol));
                await cmd.ExecuteNonQueryAsync();
                await _dbContext.WriteAuditLogAsync(_adminUsername, enable ? "VPD_ENABLE" : "VPD_DISABLE", $"{tbl}.{pol}", 0);
                MessageBox.Show($"✓ Policy {(enable ? "enabled" : "disabled")}!", "Thành công");
                await LoadVPDAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private async Task DropVPDAsync()
        {
            if (dgvVPD.SelectedRows.Count == 0) { MessageBox.Show("Chọn một policy"); return; }
            if (MessageBox.Show("Xóa VPD policy này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            var tbl = dgvVPD.SelectedRows[0].Cells["Table"].Value?.ToString();
            var pol = dgvVPD.SelectedRows[0].Cells["Policy"].Value?.ToString();
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = "BEGIN DBMS_RLS.DROP_POLICY(USER, :t, :p); END;";
                cmd.Parameters.Add(new OracleParameter("t", tbl));
                cmd.Parameters.Add(new OracleParameter("p", pol));
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("✓ Đã xóa!"); await LoadVPDAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private async Task ToggleFGAAsync(bool enable)
        {
            if (dgvFGA.SelectedRows.Count == 0) { MessageBox.Show("Chọn một FGA policy"); return; }
            var tbl = dgvFGA.SelectedRows[0].Cells["Table"].Value?.ToString();
            var pol = dgvFGA.SelectedRows[0].Cells["Policy"].Value?.ToString();
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = $"BEGIN DBMS_FGA.{(enable ? "ENABLE" : "DISABLE")}_POLICY(USER, :t, :p); END;";
                cmd.Parameters.Add(new OracleParameter("t", tbl));
                cmd.Parameters.Add(new OracleParameter("p", pol));
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show($"✓ FGA Policy {(enable ? "enabled" : "disabled")}!"); await LoadFGAAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private async Task DropFGAAsync()
        {
            if (dgvFGA.SelectedRows.Count == 0) { MessageBox.Show("Chọn một FGA policy"); return; }
            if (MessageBox.Show("Xóa FGA policy này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            var tbl = dgvFGA.SelectedRows[0].Cells["Table"].Value?.ToString();
            var pol = dgvFGA.SelectedRows[0].Cells["Policy"].Value?.ToString();
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = "BEGIN DBMS_FGA.DROP_POLICY(USER, :t, :p); END;";
                cmd.Parameters.Add(new OracleParameter("t", tbl));
                cmd.Parameters.Add(new OracleParameter("p", pol));
                await cmd.ExecuteNonQueryAsync();
                MessageBox.Show("✓ Đã xóa!"); await LoadFGAAsync();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
        }

        private async Task ViewFGALogsAsync()
        {
            try
            {
                using var cmd = _dbContext.Connection.CreateCommand();
                cmd.CommandText = "SELECT TIMESTAMP, DB_USER, OBJECT_NAME, POLICY_NAME, SQL_TEXT FROM DBA_FGA_AUDIT_TRAIL WHERE OBJECT_SCHEMA = USER ORDER BY TIMESTAMP DESC FETCH FIRST 100 ROWS ONLY";
                var list = new List<object>();
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(new { Time = r.GetDateTime(0).ToString("dd/MM HH:mm"), User = r.GetString(1), Table = r.GetString(2), Policy = r.GetString(3), SQL = r.IsDBNull(4) ? "" : r.GetString(4).Substring(0, Math.Min(50, r.GetString(4).Length)) });

                using var dlg = new Form { Text = "FGA Audit Logs", Size = new Size(900, 500), BackColor = Color.White };
                var dgv = new DataGridView { Dock = DockStyle.Fill, DataSource = list, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
                if (list.Count == 0)
                {
                    var lbl = new Label { Text = "📋 Chưa có FGA audit logs.\n\n1. Thêm FGA policy (click đôi vào preset)\n2. Thực hiện query thỏa điều kiện\n3. Logs sẽ hiện ở đây", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12F) };
                    dlg.Controls.Add(lbl);
                }
                else dlg.Controls.Add(dgv);
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}\n\nCần quyền SELECT trên DBA_FGA_AUDIT_TRAIL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowCustomVPDDialog()
        {
            using var dlg = new Form { Text = "Thêm VPD Policy tùy chỉnh", Size = new Size(550, 360), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White, Font = new Font("Segoe UI", 9F) };
            
            var cboTable = new ComboBox { Location = new Point(150, 30), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTable.Items.AddRange(new[] { "TINNHAN", "TAIKHOAN", "CUOCTROCHUYEN", "THANHVIEN", "ATTACHMENT" }); cboTable.SelectedIndex = 0;
            var txtName = new TextBox { Location = new Point(150, 65), Size = new Size(350, 25) };
            var txtFunc = new TextBox { Location = new Point(150, 100), Size = new Size(350, 25), Text = "TINNHAN_POLICY_FN_V2" };
            var cboStmt = new ComboBox { Location = new Point(150, 135), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboStmt.Items.AddRange(new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "SELECT,INSERT,UPDATE,DELETE" }); cboStmt.SelectedIndex = 0;
            var chkEnable = new CheckBox { Text = "Enable ngay", Location = new Point(150, 170), Checked = true };
            var btnOK = new Button { Text = "Thêm", Location = new Point(300, 250), Size = new Size(90, 35), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Nhập tên policy!"); return; }
                try
                {
                    using var cmd = _dbContext.Connection.CreateCommand();
                    cmd.BindByName = true;
                    cmd.CommandText = $"BEGIN DBMS_RLS.ADD_POLICY(USER, :o, :p, USER, :f, :s, {(chkEnable.Checked ? "TRUE" : "FALSE")}); END;";
                    cmd.Parameters.Add(new OracleParameter("o", cboTable.SelectedItem));
                    cmd.Parameters.Add(new OracleParameter("p", txtName.Text.Trim()));
                    cmd.Parameters.Add(new OracleParameter("f", txtFunc.Text.Trim()));
                    cmd.Parameters.Add(new OracleParameter("s", cboStmt.SelectedItem));
                    await cmd.ExecuteNonQueryAsync();
                    MessageBox.Show("✓ Đã thêm!"); dlg.Close(); await LoadVPDAsync();
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            };
            dlg.Controls.AddRange(new Control[] {
                new Label { Text = "Bảng:", Location = new Point(30, 33), AutoSize = true }, cboTable,
                new Label { Text = "Tên Policy:", Location = new Point(30, 68), AutoSize = true }, txtName,
                new Label { Text = "Policy Function:", Location = new Point(30, 103), AutoSize = true }, txtFunc,
                new Label { Text = "Statement Types:", Location = new Point(30, 138), AutoSize = true }, cboStmt, chkEnable, btnOK });
            dlg.ShowDialog(this);
        }

        private void ShowCustomFGADialog()
        {
            using var dlg = new Form { Text = "Thêm FGA Policy tùy chỉnh", Size = new Size(550, 400), StartPosition = FormStartPosition.CenterParent, BackColor = Color.White, Font = new Font("Segoe UI", 9F) };
            
            var cboTable = new ComboBox { Location = new Point(150, 30), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboTable.Items.AddRange(new[] { "TINNHAN", "TAIKHOAN", "CUOCTROCHUYEN", "THANHVIEN" }); cboTable.SelectedIndex = 0;
            var txtName = new TextBox { Location = new Point(150, 65), Size = new Size(350, 25) };
            var txtCol = new TextBox { Location = new Point(150, 100), Size = new Size(350, 25), Text = "NOIDUNG" };
            var txtCond = new TextBox { Location = new Point(150, 135), Size = new Size(350, 25), Text = "1=1" };
            var cboStmt = new ComboBox { Location = new Point(150, 170), Size = new Size(350, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboStmt.Items.AddRange(new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "SELECT,UPDATE,DELETE" }); cboStmt.SelectedIndex = 0;
            var chkEnable = new CheckBox { Text = "Enable ngay", Location = new Point(150, 205), Checked = true };
            var btnOK = new Button { Text = "Thêm", Location = new Point(300, 290), Size = new Size(90, 35), BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Nhập tên policy!"); return; }
                try
                {
                    using var cmd = _dbContext.Connection.CreateCommand();
                    cmd.BindByName = true;
                    cmd.CommandText = $"BEGIN DBMS_FGA.ADD_POLICY(USER, :o, :p, :cond, :col, :s, {(chkEnable.Checked ? "TRUE" : "FALSE")}); END;";
                    cmd.Parameters.Add(new OracleParameter("o", cboTable.SelectedItem));
                    cmd.Parameters.Add(new OracleParameter("p", txtName.Text.Trim()));
                    cmd.Parameters.Add(new OracleParameter("cond", txtCond.Text.Trim()));
                    cmd.Parameters.Add(new OracleParameter("col", txtCol.Text.Trim()));
                    cmd.Parameters.Add(new OracleParameter("s", cboStmt.SelectedItem));
                    await cmd.ExecuteNonQueryAsync();
                    MessageBox.Show("✓ Đã thêm!"); dlg.Close(); await LoadFGAAsync();
                }
                catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
            };
            dlg.Controls.AddRange(new Control[] {
                new Label { Text = "Bảng:", Location = new Point(30, 33), AutoSize = true }, cboTable,
                new Label { Text = "Tên Policy:", Location = new Point(30, 68), AutoSize = true }, txtName,
                new Label { Text = "Audit Column:", Location = new Point(30, 103), AutoSize = true }, txtCol,
                new Label { Text = "Audit Condition:", Location = new Point(30, 138), AutoSize = true }, txtCond,
                new Label { Text = "Statement Types:", Location = new Point(30, 173), AutoSize = true }, cboStmt, chkEnable, btnOK });
            dlg.ShowDialog(this);
        }
        #endregion

        #region ===== HELPERS =====
        private void RepositionBottomControls(Label lblStatus, Button btnRefresh, Button btnClose)
        {
            int bottom = this.ClientSize.Height - 45;
            lblStatus.Location = new Point(20, bottom + 8);
            btnClose.Location = new Point(this.ClientSize.Width - 130, bottom);
            btnRefresh.Location = new Point(this.ClientSize.Width - 260, bottom);
        }

        private Button CreateBtn(string text, Color bg, Point loc, Color? fg = null)
        {
            var btn = new Button { Text = text, Size = new Size(110, 32), Location = loc, BackColor = bg, ForeColor = fg ?? Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private string GetHelpText()
        {
            return @"
╔══════════════════════════════════════════════════════════════════════════════════╗
║                    HƯỚNG DẪN ORACLE SECURITY POLICIES                            ║
╚══════════════════════════════════════════════════════════════════════════════════╝

🛡️ VPD (Virtual Private Database) / RLS (Row Level Security)
════════════════════════════════════════════════════════════════════════════════════
  📌 TỰ ĐỘNG thêm WHERE clause vào query
     SELECT * FROM TINNHAN  →  SELECT * FROM TINNHAN WHERE SECURITYLABEL <= 2
  
  📌 CÁC PRESET:
     • VPD_TINNHAN_MAC: Lọc theo SecurityLabel (Bell-LaPadula)
     • VPD_CUOCTROCHUYEN_MEMBER: Chỉ xem cuộc chat mình tham gia
     • VPD_THANHVIEN_MEMBER: Chỉ xem thành viên trong chat của mình

📋 FGA (Fine-Grained Auditing)
════════════════════════════════════════════════════════════════════════════════════
  📌 GHI LOG khi user truy cập data nhạy cảm
  
  📌 CÁC PRESET:
     • FGA_TINNHAN_CONFIDENTIAL: Audit khi xem tin SecurityLabel >= 3
     • FGA_TINNHAN_MODIFY: Audit khi UPDATE/DELETE tin nhắn
     • FGA_TAIKHOAN_PASSWORD: Audit khi xem thông tin mật khẩu

🔐 ENCRYPTION (AES / RSA / Hybrid)
════════════════════════════════════════════════════════════════════════════════════
  📌 AES-256 (Symmetric): 
     • Socket communication: EncryptionHelper.Encrypt()/Decrypt()
     • Database: DBMS_CRYPTO trong SP_GUI_TINNHAN_MAHOA_AES
  
  📌 RSA-2048 (Asymmetric):
     • Chữ ký số: RsaSign() khi gửi, RsaVerify() khi nhận
     • Key exchange: Mã hóa AES key để gửi qua mạng
     • Mã hóa data nhỏ: RsaEncrypt() cho data < 200 bytes
  
  📌 Hybrid (RSA + AES):
     • HybridEncrypt(): Mã hóa file/attachment lớn
       1. Tạo AES session key ngẫu nhiên
       2. Mã hóa data bằng AES (nhanh)
       3. Mã hóa AES key bằng RSA (bảo mật)

💡 CÁCH SỬ DỤNG:
════════════════════════════════════════════════════════════════════════════════════
  1. Click đôi vào preset policy để thêm nhanh
  2. Hoặc click '➕ Tùy chỉnh' để tạo policy riêng
  3. Xem ENCRYPTION_LOGS để theo dõi mã hóa

⚠️ LƯU Ý:
  • Chạy SQL script trước: Database/Scripts/create_encryption_logs.sql
  • FGA cần quyền DBA để xem logs: DBA_FGA_AUDIT_TRAIL
";
        }
        #endregion
    }
}

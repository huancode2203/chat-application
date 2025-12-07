using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;

namespace ChatClient.Forms
{
    /// <summary>
    /// Dialog tạo nhóm mới với giao diện hiện đại
    /// Hỗ trợ tìm kiếm, chọn tất cả, và giao diện đẹp mắt
    /// </summary>
    public partial class CreateGroupDialog : Form
    {
        private readonly List<string> _allMembers = new List<string>();
        private readonly HashSet<string> _selectedMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly SocketClientService? _socketClient;
        private readonly User? _currentUser;

        public string GroupName => txtGroupName.Text.Trim();
        public string GroupType => GetGroupTypeValue();
        public List<string> Members => _selectedMembers.ToList();

        // Color palette
        private readonly Color _primaryColor = Color.FromArgb(0, 132, 255);
        private readonly Color _successColor = Color.FromArgb(40, 167, 69);
        private readonly Color _dangerColor = Color.FromArgb(220, 53, 69);
        private readonly Color _warningColor = Color.FromArgb(255, 193, 7);
        private readonly Color _bgColor = Color.FromArgb(245, 246, 250);
        private readonly Color _textColor = Color.FromArgb(28, 30, 33);
        private readonly Color _mutedColor = Color.FromArgb(108, 117, 125);

        public CreateGroupDialog() : this(null, null) { }

        public CreateGroupDialog(SocketClientService? socketClient, User? currentUser)
        {
            _socketClient = socketClient;
            _currentUser = currentUser;
            InitializeComponent();
            ApplyModernStyling();
            SetupControls();
            _ = LoadMembersAsync();
        }

        private string GetGroupTypeValue()
        {
            if (cbGroupType.SelectedItem == null) return "GROUP";
            var selected = cbGroupType.SelectedItem.ToString();
            if (selected.Contains("PROJECT")) return "PROJECT";
            if (selected.Contains("DEPARTMENT")) return "DEPARTMENT";
            if (selected.Contains("TEAM")) return "TEAM";
            return "GROUP";
        }

        private void ApplyModernStyling()
        {
            BackColor = _bgColor;
            Font = new Font("Segoe UI", 9F);

            // Style group name textbox
            txtGroupName.Font = new Font("Segoe UI", 10F);
            txtGroupName.BorderStyle = BorderStyle.FixedSingle;

            // Style search textbox
            txtSearch.Font = new Font("Segoe UI", 9F);
            txtSearch.BorderStyle = BorderStyle.FixedSingle;

            // Style combo box
            cbGroupType.Font = new Font("Segoe UI", 9F);
            cbGroupType.FlatStyle = FlatStyle.Flat;

            // Style checked list box
            lstMembers.Font = new Font("Segoe UI", 9F);
            lstMembers.BorderStyle = BorderStyle.FixedSingle;
            lstMembers.BackColor = Color.White;

            // Style buttons
            StyleButton(btnOK, _primaryColor, "✅");
            StyleButton(btnCancel, Color.White, "❌", true);
            StyleButton(btnSelectAll, _successColor, "☑️");
            StyleButton(btnSelectNone, _mutedColor, "☐");

            // Style labels
            lblGroupName.Font = new Font("Segoe UI Semibold", 9F);
            lblGroupName.ForeColor = _textColor;

            lblGroupType.Font = new Font("Segoe UI Semibold", 9F);
            lblGroupType.ForeColor = _textColor;

            lblMembers.Font = new Font("Segoe UI Semibold", 9F);
            lblMembers.ForeColor = _textColor;

            lblSelectedCount.Font = new Font("Segoe UI", 9F);
            lblSelectedCount.ForeColor = _primaryColor;

            // Add placeholder to search
            txtSearch.ForeColor = Color.Gray;
            txtSearch.Text = "🔍 Tìm kiếm thành viên...";
            txtSearch.GotFocus += (s, e) =>
            {
                if (txtSearch.Text == "🔍 Tìm kiếm thành viên...")
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = _textColor;
                }
            };
            txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = "🔍 Tìm kiếm thành viên...";
                    txtSearch.ForeColor = Color.Gray;
                }
            };
        }

        private void StyleButton(Button btn, Color bgColor, string emoji, bool isOutline = false)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9F);
            btn.Cursor = Cursors.Hand;

            if (isOutline)
            {
                btn.BackColor = bgColor;
                btn.ForeColor = _mutedColor;
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btn.FlatAppearance.BorderSize = 1;
            }
            else
            {
                btn.BackColor = bgColor;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
            }

            if (!string.IsNullOrEmpty(emoji) && !btn.Text.Contains(emoji))
            {
                var cleanText = btn.Text.Replace("✅", "").Replace("❌", "")
                    .Replace("☑️", "").Replace("☐", "").Trim();
                btn.Text = $"{emoji} {cleanText}";
            }

            // Hover effect
            btn.MouseEnter += (s, e) =>
            {
                if (!isOutline)
                    btn.BackColor = ControlPaint.Light(bgColor, 0.1f);
            };
            btn.MouseLeave += (s, e) =>
            {
                if (!isOutline)
                    btn.BackColor = bgColor;
            };
        }

        private void SetupControls()
        {
            // Group types with icons
            cbGroupType.Items.Clear();
            cbGroupType.Items.AddRange(new object[]
            {
                "👥 GROUP - Nhóm thường",
                "📋 PROJECT - Dự án",
                "🏢 DEPARTMENT - Phòng ban",
                "👨‍👩‍👧‍👦 TEAM - Nhóm làm việc"
            });
            cbGroupType.SelectedIndex = 0;

            // OK button validation
            btnOK.Click += (s, e) =>
            {
                if (ValidateInput())
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            // Search functionality
            txtSearch.TextChanged += (s, e) =>
            {
                if (txtSearch.Text != "🔍 Tìm kiếm thành viên...")
                {
                    ApplyMemberFilter();
                }
            };

            // Member selection
            lstMembers.ItemCheck += (s, e) =>
            {
                // Use BeginInvoke to get the new check state
                BeginInvoke(new Action(() =>
                {
                    var name = lstMembers.Items[e.Index]?.ToString();
                    if (string.IsNullOrEmpty(name)) return;

                    if (e.NewValue == CheckState.Checked)
                    {
                        _selectedMembers.Add(name);
                    }
                    else
                    {
                        _selectedMembers.Remove(name);
                    }
                    UpdateSelectedCountLabel();
                }));
            };

            btnSelectAll.Click += (s, e) => SetAllMembersChecked(true);
            btnSelectNone.Click += (s, e) => SetAllMembersChecked(false);

            // Enter key handling
            txtGroupName.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    txtSearch.Focus();
                }
            };

            txtSearch.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    lstMembers.Focus();
                }
            };

            // Form key handling
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        private bool ValidateInput()
        {
            // Validate group name
            if (string.IsNullOrWhiteSpace(GroupName))
            {
                ShowValidationError("Vui lòng nhập tên nhóm.", txtGroupName);
                return false;
            }

            if (GroupName.Length < 3)
            {
                ShowValidationError("Tên nhóm phải có ít nhất 3 ký tự.", txtGroupName);
                return false;
            }

            if (GroupName.Length > 50)
            {
                ShowValidationError("Tên nhóm không được quá 50 ký tự.", txtGroupName);
                return false;
            }

            // Check for invalid characters
            var invalidChars = new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
            if (GroupName.IndexOfAny(invalidChars) >= 0)
            {
                ShowValidationError("Tên nhóm chứa ký tự không hợp lệ.", txtGroupName);
                return false;
            }

            // Validate members
            if (_selectedMembers.Count == 0)
            {
                ShowValidationError("Vui lòng chọn ít nhất một thành viên.", lstMembers);
                return false;
            }

            return true;
        }

        private void ShowValidationError(string message, Control control)
        {
            MessageBox.Show(message, "Lỗi xác thực",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            control?.Focus();
        }

        private async Task LoadMembersAsync()
        {
            _allMembers.Clear();
            
            if (_socketClient != null && _currentUser != null)
            {
                try
                {
                    var response = await _socketClient.GetUsersForChatAsync(_currentUser);
                    if (response?.Success == true && response.UserList != null)
                    {
                        _allMembers.AddRange(response.UserList);
                    }
                }
                catch
                {
                    // Fallback to empty list if server unavailable
                }
            }

            if (_allMembers.Count == 0)
            {
                // Fallback sample data
                _allMembers.AddRange(new[] { "nguoidung1", "nguoidung2", "nguoidung3" });
            }

            ApplyMemberFilter();
        }

        private void ApplyMemberFilter()
        {
            lstMembers.BeginUpdate();
            try
            {
                lstMembers.Items.Clear();

                var term = txtSearch.Text.Trim();
                if (term == "🔍 Tìm kiếm thành viên...")
                    term = string.Empty;

                IEnumerable<string> source = _allMembers;

                if (!string.IsNullOrEmpty(term))
                {
                    source = source.Where(m => 
                        m.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var member in source.OrderBy(m => m))
                {
                    var index = lstMembers.Items.Add(member);
                    if (_selectedMembers.Contains(member))
                    {
                        lstMembers.SetItemChecked(index, true);
                    }
                }
            }
            finally
            {
                lstMembers.EndUpdate();
            }

            UpdateSelectedCountLabel();
        }

        private void SetAllMembersChecked(bool isChecked)
        {
            lstMembers.BeginUpdate();
            try
            {
                if (isChecked)
                {
                    // Select all visible members
                    for (int i = 0; i < lstMembers.Items.Count; i++)
                    {
                        lstMembers.SetItemChecked(i, true);
                        var member = lstMembers.Items[i].ToString();
                        if (!string.IsNullOrEmpty(member))
                            _selectedMembers.Add(member);
                    }
                }
                else
                {
                    // Deselect all
                    _selectedMembers.Clear();
                    for (int i = 0; i < lstMembers.Items.Count; i++)
                    {
                        lstMembers.SetItemChecked(i, false);
                    }
                }
            }
            finally
            {
                lstMembers.EndUpdate();
            }

            UpdateSelectedCountLabel();
        }

        private void UpdateSelectedCountLabel()
        {
            if (lblSelectedCount == null) return;

            var total = _allMembers.Count;
            var selected = _selectedMembers.Count;
            
            lblSelectedCount.Text = $"Đã chọn: {selected}/{total}";
            lblSelectedCount.ForeColor = selected > 0 ? _successColor : _mutedColor;
        }

        #region Designer Code
        private System.ComponentModel.IContainer components = null;
        private Label lblGroupName;
        private TextBox txtGroupName;
        private Label lblGroupType;
        private ComboBox cbGroupType;
        private Label lblMembers;
        private CheckedListBox lstMembers;
        private Button btnOK;
        private Button btnCancel;
        private TextBox txtSearch;
        private Button btnSelectAll;
        private Button btnSelectNone;
        private Label lblSelectedCount;
        private Panel pnlHeader;
        private Label lblTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new Panel();
            this.lblTitle = new Label();
            this.lblGroupName = new Label();
            this.txtGroupName = new TextBox();
            this.lblGroupType = new Label();
            this.cbGroupType = new ComboBox();
            this.lblMembers = new Label();
            this.lstMembers = new CheckedListBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.txtSearch = new TextBox();
            this.btnSelectAll = new Button();
            this.btnSelectNone = new Button();
            this.lblSelectedCount = new Label();
            this.SuspendLayout();

            // pnlHeader
            this.pnlHeader.BackColor = Color.FromArgb(0, 132, 255);
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Height = 60;
            this.pnlHeader.Controls.Add(this.lblTitle);

            // lblTitle
            this.lblTitle.Text = "👥 Tạo Nhóm Mới";
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            this.lblTitle.Dock = DockStyle.Fill;
            this.lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            this.lblTitle.Padding = new Padding(20, 0, 0, 0);

            // lblGroupName
            this.lblGroupName.AutoSize = true;
            this.lblGroupName.Location = new Point(20, 80);
            this.lblGroupName.Text = "Tên nhóm *";

            // txtGroupName
            this.txtGroupName.Location = new Point(120, 77);
            this.txtGroupName.Size = new Size(310, 27);
            this.txtGroupName.MaxLength = 50;

            // lblGroupType
            this.lblGroupType.AutoSize = true;
            this.lblGroupType.Location = new Point(20, 115);
            this.lblGroupType.Text = "Loại nhóm";

            // cbGroupType
            this.cbGroupType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbGroupType.Location = new Point(120, 112);
            this.cbGroupType.Size = new Size(310, 27);

            // lblMembers
            this.lblMembers.AutoSize = true;
            this.lblMembers.Location = new Point(20, 150);
            this.lblMembers.Text = "Thành viên *";

            // txtSearch
            this.txtSearch.Location = new Point(120, 147);
            this.txtSearch.Size = new Size(210, 27);

            // btnSelectAll
            this.btnSelectAll.Location = new Point(120, 345);
            this.btnSelectAll.Size = new Size(100, 30);
            this.btnSelectAll.Text = "Chọn tất cả";

            // btnSelectNone
            this.btnSelectNone.Location = new Point(230, 345);
            this.btnSelectNone.Size = new Size(100, 30);
            this.btnSelectNone.Text = "Bỏ chọn";

            // lblSelectedCount
            this.lblSelectedCount.AutoSize = true;
            this.lblSelectedCount.Location = new Point(340, 352);
            this.lblSelectedCount.Text = "Đã chọn: 0/0";

            // lstMembers
            this.lstMembers.CheckOnClick = true;
            this.lstMembers.Location = new Point(120, 180);
            this.lstMembers.Size = new Size(310, 155);
            this.lstMembers.IntegralHeight = false;

            // btnOK
            this.btnOK.Location = new Point(240, 395);
            this.btnOK.Size = new Size(100, 36);
            this.btnOK.Text = "Tạo nhóm";

            // btnCancel
            this.btnCancel.Location = new Point(350, 395);
            this.btnCancel.Size = new Size(80, 36);
            this.btnCancel.Text = "Hủy";

            // CreateGroupDialog
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(460, 450);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.lblSelectedCount);
            this.Controls.Add(this.btnSelectNone);
            this.Controls.Add(this.btnSelectAll);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lstMembers);
            this.Controls.Add(this.lblMembers);
            this.Controls.Add(this.cbGroupType);
            this.Controls.Add(this.lblGroupType);
            this.Controls.Add(this.txtGroupName);
            this.Controls.Add(this.lblGroupName);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreateGroupDialog";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Tạo nhóm mới";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion
    }
}

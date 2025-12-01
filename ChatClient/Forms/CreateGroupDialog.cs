using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ChatClient.Forms
{
    public partial class CreateGroupDialog : Form
    {
        public string GroupName => txtGroupName.Text.Trim();
        public string GroupType => cbGroupType.SelectedItem?.ToString() ?? "GROUP";
        public List<string> Members => lstMembers.CheckedItems.Cast<string>().ToList();

        public CreateGroupDialog()
        {
            InitializeComponent();
            SetupControls();
        }

        private void SetupControls()
        {
            // Group types
            cbGroupType.Items.AddRange(new object[]
            {
                "GROUP - Nhóm thường",
                "PROJECT - Dự án",
                "DEPARTMENT - Phòng ban",
                "TEAM - Nhóm làm việc"
            });
            cbGroupType.SelectedIndex = 0;

            // Sample members (in real app, load from server)
            lstMembers.Items.AddRange(new object[]
            {
                "nguoidung1",
                "nguoidung2",
                "nguoidung3",
                "nguoidung4",
                "quantrivien1"
            });

            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(GroupName))
                {
                    MessageBox.Show("Vui lòng nhập tên nhóm.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (lstMembers.CheckedItems.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một thành viên.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
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
            this.lblGroupName = new Label();
            this.txtGroupName = new TextBox();
            this.lblGroupType = new Label();
            this.cbGroupType = new ComboBox();
            this.lblMembers = new Label();
            this.lstMembers = new CheckedListBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.SuspendLayout();

            // lblGroupName
            this.lblGroupName.AutoSize = true;
            this.lblGroupName.Location = new System.Drawing.Point(20, 20);
            this.lblGroupName.Name = "lblGroupName";
            this.lblGroupName.Size = new System.Drawing.Size(70, 15);
            this.lblGroupName.Text = "Tên nhóm:";

            // txtGroupName
            this.txtGroupName.Location = new System.Drawing.Point(120, 17);
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(300, 23);

            // lblGroupType
            this.lblGroupType.AutoSize = true;
            this.lblGroupType.Location = new System.Drawing.Point(20, 55);
            this.lblGroupType.Name = "lblGroupType";
            this.lblGroupType.Size = new System.Drawing.Size(75, 15);
            this.lblGroupType.Text = "Loại nhóm:";

            // cbGroupType
            this.cbGroupType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbGroupType.Location = new System.Drawing.Point(120, 52);
            this.cbGroupType.Name = "cbGroupType";
            this.cbGroupType.Size = new System.Drawing.Size(300, 23);

            // lblMembers
            this.lblMembers.AutoSize = true;
            this.lblMembers.Location = new System.Drawing.Point(20, 90);
            this.lblMembers.Name = "lblMembers";
            this.lblMembers.Size = new System.Drawing.Size(85, 15);
            this.lblMembers.Text = "Thành viên:";

            // lstMembers
            this.lstMembers.CheckOnClick = true;
            this.lstMembers.Location = new System.Drawing.Point(120, 90);
            this.lstMembers.Name = "lstMembers";
            this.lstMembers.Size = new System.Drawing.Size(300, 200);

            // btnOK
            this.btnOK.Location = new System.Drawing.Point(220, 310);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 35);
            this.btnOK.Text = "Tạo nhóm";
            this.btnOK.UseVisualStyleBackColor = true;

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(330, 310);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;

            // CreateGroupDialog
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 370);
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
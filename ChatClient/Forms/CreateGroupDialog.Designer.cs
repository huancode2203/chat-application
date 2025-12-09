namespace ChatClient.Forms
{
    partial class CreateGroupDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblGroupName = new System.Windows.Forms.Label();
            this.txtGroupName = new System.Windows.Forms.TextBox();
            this.lblGroupType = new System.Windows.Forms.Label();
            this.cbGroupType = new System.Windows.Forms.ComboBox();
            this.lblMembers = new System.Windows.Forms.Label();
            this.lstMembers = new System.Windows.Forms.CheckedListBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.btnSelectAll = new System.Windows.Forms.Button();
            this.btnSelectNone = new System.Windows.Forms.Button();
            this.lblSelectedCount = new System.Windows.Forms.Label();
            this.pnlHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(0, 132, 255);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(460, 60);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.lblTitle.Size = new System.Drawing.Size(460, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "👥 Tạo Nhóm Mới";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblGroupName
            // 
            this.lblGroupName.AutoSize = true;
            this.lblGroupName.Location = new System.Drawing.Point(20, 80);
            this.lblGroupName.Name = "lblGroupName";
            this.lblGroupName.Size = new System.Drawing.Size(77, 20);
            this.lblGroupName.TabIndex = 1;
            this.lblGroupName.Text = "Tên nhóm *";
            // 
            // txtGroupName
            // 
            this.txtGroupName.Location = new System.Drawing.Point(120, 77);
            this.txtGroupName.MaxLength = 50;
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(310, 27);
            this.txtGroupName.TabIndex = 2;
            // 
            // lblGroupType
            // 
            this.lblGroupType.AutoSize = true;
            this.lblGroupType.Location = new System.Drawing.Point(20, 115);
            this.lblGroupType.Name = "lblGroupType";
            this.lblGroupType.Size = new System.Drawing.Size(74, 20);
            this.lblGroupType.TabIndex = 3;
            this.lblGroupType.Text = "Loại nhóm";
            // 
            // cbGroupType
            // 
            this.cbGroupType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbGroupType.FormattingEnabled = true;
            this.cbGroupType.Location = new System.Drawing.Point(120, 112);
            this.cbGroupType.Name = "cbGroupType";
            this.cbGroupType.Size = new System.Drawing.Size(310, 28);
            this.cbGroupType.TabIndex = 4;
            // 
            // lblMembers
            // 
            this.lblMembers.AutoSize = true;
            this.lblMembers.Location = new System.Drawing.Point(20, 150);
            this.lblMembers.Name = "lblMembers";
            this.lblMembers.Size = new System.Drawing.Size(86, 20);
            this.lblMembers.TabIndex = 5;
            this.lblMembers.Text = "Thành viên *";
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(120, 147);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(210, 27);
            this.txtSearch.TabIndex = 6;
            // 
            // lstMembers
            // 
            this.lstMembers.CheckOnClick = true;
            this.lstMembers.FormattingEnabled = true;
            this.lstMembers.IntegralHeight = false;
            this.lstMembers.Location = new System.Drawing.Point(120, 180);
            this.lstMembers.Name = "lstMembers";
            this.lstMembers.Size = new System.Drawing.Size(310, 155);
            this.lstMembers.TabIndex = 7;
            // 
            // btnSelectAll
            // 
            this.btnSelectAll.Location = new System.Drawing.Point(120, 345);
            this.btnSelectAll.Name = "btnSelectAll";
            this.btnSelectAll.Size = new System.Drawing.Size(100, 30);
            this.btnSelectAll.TabIndex = 8;
            this.btnSelectAll.Text = "Chọn tất cả";
            this.btnSelectAll.UseVisualStyleBackColor = true;
            // 
            // btnSelectNone
            // 
            this.btnSelectNone.Location = new System.Drawing.Point(230, 345);
            this.btnSelectNone.Name = "btnSelectNone";
            this.btnSelectNone.Size = new System.Drawing.Size(100, 30);
            this.btnSelectNone.TabIndex = 9;
            this.btnSelectNone.Text = "Bỏ chọn";
            this.btnSelectNone.UseVisualStyleBackColor = true;
            // 
            // lblSelectedCount
            // 
            this.lblSelectedCount.AutoSize = true;
            this.lblSelectedCount.Location = new System.Drawing.Point(340, 352);
            this.lblSelectedCount.Name = "lblSelectedCount";
            this.lblSelectedCount.Size = new System.Drawing.Size(79, 20);
            this.lblSelectedCount.TabIndex = 10;
            this.lblSelectedCount.Text = "Đã chọn: 0/0";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(240, 395);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 36);
            this.btnOK.TabIndex = 11;
            this.btnOK.Text = "Tạo nhóm";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(350, 395);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 36);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Hủy";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // CreateGroupDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 450);
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
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreateGroupDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Tạo nhóm mới";
            this.pnlHeader.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblGroupName;
        private System.Windows.Forms.TextBox txtGroupName;
        private System.Windows.Forms.Label lblGroupType;
        private System.Windows.Forms.ComboBox cbGroupType;
        private System.Windows.Forms.Label lblMembers;
        private System.Windows.Forms.CheckedListBox lstMembers;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnSelectNone;
        private System.Windows.Forms.Label lblSelectedCount;
    }
}

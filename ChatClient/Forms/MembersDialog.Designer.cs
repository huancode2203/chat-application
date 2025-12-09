namespace ChatClient.Forms
{
    partial class MembersDialog
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
            lstMembers = new System.Windows.Forms.ListView();
            colUsername = new System.Windows.Forms.ColumnHeader();
            colEmail = new System.Windows.Forms.ColumnHeader();
            colRole = new System.Windows.Forms.ColumnHeader();
            colBanStatus = new System.Windows.Forms.ColumnHeader();
            colJoinedDate = new System.Windows.Forms.ColumnHeader();
            btnAddMember = new System.Windows.Forms.Button();
            btnRemoveMember = new System.Windows.Forms.Button();
            btnBanMember = new System.Windows.Forms.Button();
            btnUnbanMember = new System.Windows.Forms.Button();
            btnClose = new System.Windows.Forms.Button();
            lblStatus = new System.Windows.Forms.Label();
            lblTitle = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // lstMembers
            // 
            lstMembers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colUsername, colEmail, colRole, colBanStatus, colJoinedDate });
            lstMembers.FullRowSelect = true;
            lstMembers.GridLines = true;
            lstMembers.Location = new System.Drawing.Point(20, 70);
            lstMembers.MultiSelect = false;
            lstMembers.Name = "lstMembers";
            lstMembers.Size = new System.Drawing.Size(760, 450);
            lstMembers.TabIndex = 0;
            lstMembers.UseCompatibleStateImageBehavior = false;
            lstMembers.View = System.Windows.Forms.View.Details;
            // 
            // colUsername
            // 
            colUsername.Text = "Người dùng";
            colUsername.Width = 150;
            // 
            // colEmail
            // 
            colEmail.Text = "Email";
            colEmail.Width = 180;
            // 
            // colRole
            // 
            colRole.Text = "Vai trò";
            colRole.Width = 120;
            // 
            // colBanStatus
            // 
            colBanStatus.Text = "Trạng thái";
            colBanStatus.Width = 120;
            // 
            // colJoinedDate
            // 
            colJoinedDate.Text = "Ngày tham gia";
            colJoinedDate.Width = 140;
            // 
            // btnAddMember
            // 
            btnAddMember.Location = new System.Drawing.Point(20, 540);
            btnAddMember.Name = "btnAddMember";
            btnAddMember.Size = new System.Drawing.Size(130, 40);
            btnAddMember.TabIndex = 1;
            btnAddMember.Text = "➕ Thêm";
            btnAddMember.UseVisualStyleBackColor = true;
            // 
            // btnRemoveMember
            // 
            btnRemoveMember.Enabled = false;
            btnRemoveMember.Location = new System.Drawing.Point(160, 540);
            btnRemoveMember.Name = "btnRemoveMember";
            btnRemoveMember.Size = new System.Drawing.Size(130, 40);
            btnRemoveMember.TabIndex = 2;
            btnRemoveMember.Text = "❌ Xóa";
            btnRemoveMember.UseVisualStyleBackColor = true;
            // 
            // btnBanMember
            // 
            btnBanMember.Enabled = false;
            btnBanMember.Location = new System.Drawing.Point(300, 540);
            btnBanMember.Name = "btnBanMember";
            btnBanMember.Size = new System.Drawing.Size(130, 40);
            btnBanMember.TabIndex = 3;
            btnBanMember.Text = "🔇 Tắt tiếng";
            btnBanMember.UseVisualStyleBackColor = true;
            // 
            // btnUnbanMember
            // 
            btnUnbanMember.Enabled = false;
            btnUnbanMember.Location = new System.Drawing.Point(440, 540);
            btnUnbanMember.Name = "btnUnbanMember";
            btnUnbanMember.Size = new System.Drawing.Size(130, 40);
            btnUnbanMember.TabIndex = 4;
            btnUnbanMember.Text = "🔊 Bỏ tắt tiếng";
            btnUnbanMember.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            btnClose.Location = new System.Drawing.Point(650, 540);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(130, 40);
            btnClose.TabIndex = 5;
            btnClose.Text = "Đóng";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new System.Drawing.Point(20, 595);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(0, 20);
            lblStatus.TabIndex = 6;
            // 
            // lblTitle
            // 
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(28, 30, 33);
            lblTitle.Location = new System.Drawing.Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(760, 40);
            lblTitle.TabIndex = 7;
            lblTitle.Text = "👥 Quản Lý Thành Viên";
            // 
            // MembersDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 630);
            Controls.Add(lblTitle);
            Controls.Add(lblStatus);
            Controls.Add(btnClose);
            Controls.Add(btnUnbanMember);
            Controls.Add(btnBanMember);
            Controls.Add(btnRemoveMember);
            Controls.Add(btnAddMember);
            Controls.Add(lstMembers);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MembersDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Quản lý thành viên";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListView lstMembers;
        private System.Windows.Forms.ColumnHeader colUsername;
        private System.Windows.Forms.ColumnHeader colEmail;
        private System.Windows.Forms.ColumnHeader colRole;
        private System.Windows.Forms.ColumnHeader colBanStatus;
        private System.Windows.Forms.ColumnHeader colJoinedDate;
        private System.Windows.Forms.Button btnAddMember;
        private System.Windows.Forms.Button btnRemoveMember;
        private System.Windows.Forms.Button btnBanMember;
        private System.Windows.Forms.Button btnUnbanMember;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblTitle;
    }
}

namespace ChatServer.Forms
{
    partial class AdminPanelForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabUsers;
        private System.Windows.Forms.TabPage tabConversations;
        private System.Windows.Forms.TabPage tabMessages;
        private System.Windows.Forms.TabPage tabAuditLogs;
        private System.Windows.Forms.DataGridView dgvUsers;
        private System.Windows.Forms.DataGridView dgvConversations;
        private System.Windows.Forms.DataGridView dgvMessages;
        private System.Windows.Forms.DataGridView dgvAuditLogs;
        private System.Windows.Forms.Button btnRefreshUsers;
        private System.Windows.Forms.Button btnCreateUser;
        private System.Windows.Forms.Button btnEditUser;
        private System.Windows.Forms.Button btnDeleteUser;
        private System.Windows.Forms.Button btnBanUser;
        private System.Windows.Forms.Button btnUnbanUser;
        private System.Windows.Forms.Button btnUnlockAccount;
        private System.Windows.Forms.Button btnRefreshConversations;
        private System.Windows.Forms.Button btnDeleteConversation;
        private System.Windows.Forms.Button btnViewMessages;
        private System.Windows.Forms.Button btnRefreshMessages;
        private System.Windows.Forms.Button btnDeleteMessage;
        private System.Windows.Forms.Button btnRefreshLogs;
        private System.Windows.Forms.Label lblCurrentUser;
        private System.Windows.Forms.Button btnPolicyManagement;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            tabControl = new System.Windows.Forms.TabControl();
            tabUsers = new System.Windows.Forms.TabPage();
            dgvUsers = new System.Windows.Forms.DataGridView();
            btnRefreshUsers = new System.Windows.Forms.Button();
            btnCreateUser = new System.Windows.Forms.Button();
            btnEditUser = new System.Windows.Forms.Button();
            btnDeleteUser = new System.Windows.Forms.Button();
            btnBanUser = new System.Windows.Forms.Button();
            btnUnbanUser = new System.Windows.Forms.Button();
            btnUnlockAccount = new System.Windows.Forms.Button();
            tabConversations = new System.Windows.Forms.TabPage();
            dgvConversations = new System.Windows.Forms.DataGridView();
            btnRefreshConversations = new System.Windows.Forms.Button();
            btnDeleteConversation = new System.Windows.Forms.Button();
            btnViewMessages = new System.Windows.Forms.Button();
            tabMessages = new System.Windows.Forms.TabPage();
            dgvMessages = new System.Windows.Forms.DataGridView();
            btnRefreshMessages = new System.Windows.Forms.Button();
            btnDeleteMessage = new System.Windows.Forms.Button();
            tabAuditLogs = new System.Windows.Forms.TabPage();
            dgvAuditLogs = new System.Windows.Forms.DataGridView();
            btnRefreshLogs = new System.Windows.Forms.Button();
            lblCurrentUser = new System.Windows.Forms.Label();
            btnPolicyManagement = new System.Windows.Forms.Button();
            tabControl.SuspendLayout();
            tabUsers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvUsers).BeginInit();
            tabConversations.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvConversations).BeginInit();
            tabMessages.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMessages).BeginInit();
            tabAuditLogs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAuditLogs).BeginInit();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabUsers);
            tabControl.Controls.Add(tabConversations);
            tabControl.Controls.Add(tabMessages);
            tabControl.Controls.Add(tabAuditLogs);
            tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            tabControl.Location = new System.Drawing.Point(0, 41);
            tabControl.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new System.Drawing.Size(1715, 1159);
            tabControl.TabIndex = 0;
            // 
            // tabUsers
            // 
            tabUsers.Controls.Add(dgvUsers);
            tabUsers.Controls.Add(btnRefreshUsers);
            tabUsers.Controls.Add(btnCreateUser);
            tabUsers.Controls.Add(btnEditUser);
            tabUsers.Controls.Add(btnDeleteUser);
            tabUsers.Controls.Add(btnBanUser);
            tabUsers.Controls.Add(btnUnbanUser);
            tabUsers.Controls.Add(btnUnlockAccount);
            tabUsers.Location = new System.Drawing.Point(4, 34);
            tabUsers.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabUsers.Name = "tabUsers";
            tabUsers.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabUsers.Size = new System.Drawing.Size(1707, 1121);
            tabUsers.TabIndex = 0;
            tabUsers.Text = "Quản lý Users";
            tabUsers.UseVisualStyleBackColor = true;
            // 
            // dgvUsers
            // 
            dgvUsers.AllowUserToAddRows = false;
            dgvUsers.AllowUserToDeleteRows = false;
            dgvUsers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvUsers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvUsers.Location = new System.Drawing.Point(17, 100);
            dgvUsers.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dgvUsers.MultiSelect = false;
            dgvUsers.Name = "dgvUsers";
            dgvUsers.ReadOnly = true;
            dgvUsers.RowHeadersWidth = 62;
            dgvUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.Size = new System.Drawing.Size(1663, 960);
            dgvUsers.TabIndex = 0;
            // 
            // btnRefreshUsers
            // 
            btnRefreshUsers.Location = new System.Drawing.Point(17, 20);
            btnRefreshUsers.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnRefreshUsers.Name = "btnRefreshUsers";
            btnRefreshUsers.Size = new System.Drawing.Size(172, 60);
            btnRefreshUsers.TabIndex = 1;
            btnRefreshUsers.Text = "Làm mới";
            btnRefreshUsers.UseVisualStyleBackColor = true;
            // 
            // btnCreateUser
            // 
            btnCreateUser.Location = new System.Drawing.Point(205, 20);
            btnCreateUser.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnCreateUser.Name = "btnCreateUser";
            btnCreateUser.Size = new System.Drawing.Size(172, 60);
            btnCreateUser.TabIndex = 2;
            btnCreateUser.Text = "Tạo User";
            btnCreateUser.UseVisualStyleBackColor = true;
            // 
            // btnEditUser
            // 
            btnEditUser.Location = new System.Drawing.Point(395, 20);
            btnEditUser.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnEditUser.Name = "btnEditUser";
            btnEditUser.Size = new System.Drawing.Size(172, 60);
            btnEditUser.TabIndex = 3;
            btnEditUser.Text = "Sửa User";
            btnEditUser.UseVisualStyleBackColor = true;
            // 
            // btnDeleteUser
            // 
            btnDeleteUser.Location = new System.Drawing.Point(583, 20);
            btnDeleteUser.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnDeleteUser.Name = "btnDeleteUser";
            btnDeleteUser.Size = new System.Drawing.Size(172, 60);
            btnDeleteUser.TabIndex = 4;
            btnDeleteUser.Text = "Xóa User";
            btnDeleteUser.UseVisualStyleBackColor = true;
            // 
            // btnBanUser
            // 
            btnBanUser.Location = new System.Drawing.Point(772, 20);
            btnBanUser.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnBanUser.Name = "btnBanUser";
            btnBanUser.Size = new System.Drawing.Size(172, 60);
            btnBanUser.TabIndex = 5;
            btnBanUser.Text = "Cấm User";
            btnBanUser.UseVisualStyleBackColor = true;
            // 
            // btnUnbanUser
            // 
            btnUnbanUser.Location = new System.Drawing.Point(960, 20);
            btnUnbanUser.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnUnbanUser.Name = "btnUnbanUser";
            btnUnbanUser.Size = new System.Drawing.Size(172, 60);
            btnUnbanUser.TabIndex = 6;
            btnUnbanUser.Text = "Bỏ cấm";
            btnUnbanUser.UseVisualStyleBackColor = true;
            // 
            // btnUnlockAccount
            // 
            btnUnlockAccount.Location = new System.Drawing.Point(1140, 20);
            btnUnlockAccount.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnUnlockAccount.Name = "btnUnlockAccount";
            btnUnlockAccount.Size = new System.Drawing.Size(172, 60);
            btnUnlockAccount.TabIndex = 7;
            btnUnlockAccount.Text = "🔓 Mở khóa";
            btnUnlockAccount.UseVisualStyleBackColor = true;
            // 
            // tabConversations
            // 
            tabConversations.Controls.Add(dgvConversations);
            tabConversations.Controls.Add(btnRefreshConversations);
            tabConversations.Controls.Add(btnDeleteConversation);
            tabConversations.Controls.Add(btnViewMessages);
            tabConversations.Location = new System.Drawing.Point(4, 34);
            tabConversations.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabConversations.Name = "tabConversations";
            tabConversations.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabConversations.Size = new System.Drawing.Size(1707, 1121);
            tabConversations.TabIndex = 1;
            tabConversations.Text = "Quản lý Conversations";
            tabConversations.UseVisualStyleBackColor = true;
            // 
            // dgvConversations
            // 
            dgvConversations.AllowUserToAddRows = false;
            dgvConversations.AllowUserToDeleteRows = false;
            dgvConversations.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvConversations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvConversations.Location = new System.Drawing.Point(17, 100);
            dgvConversations.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dgvConversations.MultiSelect = false;
            dgvConversations.Name = "dgvConversations";
            dgvConversations.ReadOnly = true;
            dgvConversations.RowHeadersWidth = 62;
            dgvConversations.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvConversations.Size = new System.Drawing.Size(1663, 960);
            dgvConversations.TabIndex = 0;
            // 
            // btnRefreshConversations
            // 
            btnRefreshConversations.Location = new System.Drawing.Point(17, 20);
            btnRefreshConversations.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnRefreshConversations.Name = "btnRefreshConversations";
            btnRefreshConversations.Size = new System.Drawing.Size(172, 60);
            btnRefreshConversations.TabIndex = 1;
            btnRefreshConversations.Text = "Làm mới";
            btnRefreshConversations.UseVisualStyleBackColor = true;
            // 
            // btnDeleteConversation
            // 
            btnDeleteConversation.Location = new System.Drawing.Point(205, 20);
            btnDeleteConversation.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnDeleteConversation.Name = "btnDeleteConversation";
            btnDeleteConversation.Size = new System.Drawing.Size(172, 60);
            btnDeleteConversation.TabIndex = 2;
            btnDeleteConversation.Text = "Xóa";
            btnDeleteConversation.UseVisualStyleBackColor = true;
            // 
            // btnViewMessages
            // 
            btnViewMessages.Location = new System.Drawing.Point(395, 20);
            btnViewMessages.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnViewMessages.Name = "btnViewMessages";
            btnViewMessages.Size = new System.Drawing.Size(172, 60);
            btnViewMessages.TabIndex = 3;
            btnViewMessages.Text = "Xem Messages";
            btnViewMessages.UseVisualStyleBackColor = true;
            // 
            // tabMessages
            // 
            tabMessages.Controls.Add(dgvMessages);
            tabMessages.Controls.Add(btnRefreshMessages);
            tabMessages.Controls.Add(btnDeleteMessage);
            tabMessages.Location = new System.Drawing.Point(4, 34);
            tabMessages.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabMessages.Name = "tabMessages";
            tabMessages.Size = new System.Drawing.Size(1707, 1121);
            tabMessages.TabIndex = 2;
            tabMessages.Text = "Quản lý Messages";
            tabMessages.UseVisualStyleBackColor = true;
            // 
            // dgvMessages
            // 
            dgvMessages.AllowUserToAddRows = false;
            dgvMessages.AllowUserToDeleteRows = false;
            dgvMessages.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvMessages.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMessages.Location = new System.Drawing.Point(17, 100);
            dgvMessages.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dgvMessages.MultiSelect = false;
            dgvMessages.Name = "dgvMessages";
            dgvMessages.ReadOnly = true;
            dgvMessages.RowHeadersWidth = 62;
            dgvMessages.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvMessages.Size = new System.Drawing.Size(1663, 960);
            dgvMessages.TabIndex = 0;
            // 
            // btnRefreshMessages
            // 
            btnRefreshMessages.Location = new System.Drawing.Point(17, 20);
            btnRefreshMessages.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnRefreshMessages.Name = "btnRefreshMessages";
            btnRefreshMessages.Size = new System.Drawing.Size(172, 60);
            btnRefreshMessages.TabIndex = 1;
            btnRefreshMessages.Text = "Làm mới";
            btnRefreshMessages.UseVisualStyleBackColor = true;
            // 
            // btnDeleteMessage
            // 
            btnDeleteMessage.Location = new System.Drawing.Point(205, 20);
            btnDeleteMessage.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnDeleteMessage.Name = "btnDeleteMessage";
            btnDeleteMessage.Size = new System.Drawing.Size(172, 60);
            btnDeleteMessage.TabIndex = 2;
            btnDeleteMessage.Text = "Xóa";
            btnDeleteMessage.UseVisualStyleBackColor = true;
            // 
            // tabAuditLogs
            // 
            tabAuditLogs.Controls.Add(dgvAuditLogs);
            tabAuditLogs.Controls.Add(btnRefreshLogs);
            tabAuditLogs.Location = new System.Drawing.Point(4, 34);
            tabAuditLogs.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabAuditLogs.Name = "tabAuditLogs";
            tabAuditLogs.Padding = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tabAuditLogs.Size = new System.Drawing.Size(1707, 1121);
            tabAuditLogs.TabIndex = 3;
            tabAuditLogs.Text = "Audit Logs";
            tabAuditLogs.UseVisualStyleBackColor = true;
            // 
            // dgvAuditLogs
            // 
            dgvAuditLogs.AllowUserToAddRows = false;
            dgvAuditLogs.AllowUserToDeleteRows = false;
            dgvAuditLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvAuditLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAuditLogs.Location = new System.Drawing.Point(17, 100);
            dgvAuditLogs.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            dgvAuditLogs.MultiSelect = false;
            dgvAuditLogs.Name = "dgvAuditLogs";
            dgvAuditLogs.ReadOnly = true;
            dgvAuditLogs.RowHeadersWidth = 62;
            dgvAuditLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvAuditLogs.Size = new System.Drawing.Size(1663, 960);
            dgvAuditLogs.TabIndex = 0;
            // 
            // btnRefreshLogs
            // 
            btnRefreshLogs.Location = new System.Drawing.Point(17, 20);
            btnRefreshLogs.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            btnRefreshLogs.Name = "btnRefreshLogs";
            btnRefreshLogs.Size = new System.Drawing.Size(172, 60);
            btnRefreshLogs.TabIndex = 1;
            btnRefreshLogs.Text = "Làm mới";
            btnRefreshLogs.UseVisualStyleBackColor = true;
            // 
            // lblCurrentUser
            // 
            lblCurrentUser.AutoSize = true;
            lblCurrentUser.Dock = System.Windows.Forms.DockStyle.Top;
            lblCurrentUser.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            lblCurrentUser.Location = new System.Drawing.Point(0, 0);
            lblCurrentUser.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lblCurrentUser.Name = "lblCurrentUser";
            lblCurrentUser.Padding = new System.Windows.Forms.Padding(8, 10, 8, 10);
            lblCurrentUser.Size = new System.Drawing.Size(16, 41);
            lblCurrentUser.TabIndex = 1;
            // 
            // btnPolicyManagement
            // 
            btnPolicyManagement.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            btnPolicyManagement.Cursor = System.Windows.Forms.Cursors.Hand;
            btnPolicyManagement.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPolicyManagement.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnPolicyManagement.ForeColor = System.Drawing.Color.White;
            btnPolicyManagement.Location = new System.Drawing.Point(1440, 6);
            btnPolicyManagement.Margin = new System.Windows.Forms.Padding(4);
            btnPolicyManagement.Name = "btnPolicyManagement";
            btnPolicyManagement.Size = new System.Drawing.Size(240, 36);
            btnPolicyManagement.TabIndex = 2;
            btnPolicyManagement.Text = "📜 Quản lý Policy";
            btnPolicyManagement.UseVisualStyleBackColor = false;
            // 
            // AdminPanelForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(1715, 1200);
            Controls.Add(tabControl);
            Controls.Add(lblCurrentUser);
            Controls.Add(btnPolicyManagement);
            Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            Name = "AdminPanelForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Admin Panel - Chat Server";
            tabControl.ResumeLayout(false);
            tabUsers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvUsers).EndInit();
            tabConversations.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvConversations).EndInit();
            tabMessages.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvMessages).EndInit();
            tabAuditLogs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvAuditLogs).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}


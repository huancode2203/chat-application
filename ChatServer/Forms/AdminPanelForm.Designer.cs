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
        private System.Windows.Forms.Button btnRefreshConversations;
        private System.Windows.Forms.Button btnDeleteConversation;
        private System.Windows.Forms.Button btnViewMessages;
        private System.Windows.Forms.Button btnRefreshMessages;
        private System.Windows.Forms.Button btnDeleteMessage;
        private System.Windows.Forms.Button btnRefreshLogs;
        private System.Windows.Forms.Label lblCurrentUser;

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
            tabControl.Location = new System.Drawing.Point(0, 37);
            tabControl.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new System.Drawing.Size(1429, 963);
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
            tabUsers.Location = new System.Drawing.Point(4, 34);
            tabUsers.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabUsers.Name = "tabUsers";
            tabUsers.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabUsers.Size = new System.Drawing.Size(1421, 925);
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
            dgvUsers.Location = new System.Drawing.Point(14, 83);
            dgvUsers.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dgvUsers.MultiSelect = false;
            dgvUsers.Name = "dgvUsers";
            dgvUsers.ReadOnly = true;
            dgvUsers.RowHeadersWidth = 62;
            dgvUsers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.Size = new System.Drawing.Size(1386, 800);
            dgvUsers.TabIndex = 0;
            // 
            // btnRefreshUsers
            // 
            btnRefreshUsers.Location = new System.Drawing.Point(14, 17);
            btnRefreshUsers.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRefreshUsers.Name = "btnRefreshUsers";
            btnRefreshUsers.Size = new System.Drawing.Size(143, 50);
            btnRefreshUsers.TabIndex = 1;
            btnRefreshUsers.Text = "Làm mới";
            btnRefreshUsers.UseVisualStyleBackColor = true;
            // 
            // btnCreateUser
            // 
            btnCreateUser.Location = new System.Drawing.Point(171, 17);
            btnCreateUser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCreateUser.Name = "btnCreateUser";
            btnCreateUser.Size = new System.Drawing.Size(143, 50);
            btnCreateUser.TabIndex = 2;
            btnCreateUser.Text = "Tạo User";
            btnCreateUser.UseVisualStyleBackColor = true;
            // 
            // btnEditUser
            // 
            btnEditUser.Location = new System.Drawing.Point(329, 17);
            btnEditUser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnEditUser.Name = "btnEditUser";
            btnEditUser.Size = new System.Drawing.Size(143, 50);
            btnEditUser.TabIndex = 3;
            btnEditUser.Text = "Sửa User";
            btnEditUser.UseVisualStyleBackColor = true;
            // 
            // btnDeleteUser
            // 
            btnDeleteUser.Location = new System.Drawing.Point(486, 17);
            btnDeleteUser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnDeleteUser.Name = "btnDeleteUser";
            btnDeleteUser.Size = new System.Drawing.Size(143, 50);
            btnDeleteUser.TabIndex = 4;
            btnDeleteUser.Text = "Xóa User";
            btnDeleteUser.UseVisualStyleBackColor = true;
            // 
            // btnBanUser
            // 
            btnBanUser.Location = new System.Drawing.Point(643, 17);
            btnBanUser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnBanUser.Name = "btnBanUser";
            btnBanUser.Size = new System.Drawing.Size(143, 50);
            btnBanUser.TabIndex = 5;
            btnBanUser.Text = "Cấm User";
            btnBanUser.UseVisualStyleBackColor = true;
            // 
            // btnUnbanUser
            // 
            btnUnbanUser.Location = new System.Drawing.Point(800, 17);
            btnUnbanUser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnUnbanUser.Name = "btnUnbanUser";
            btnUnbanUser.Size = new System.Drawing.Size(143, 50);
            btnUnbanUser.TabIndex = 6;
            btnUnbanUser.Text = "Bỏ cấm";
            btnUnbanUser.UseVisualStyleBackColor = true;
            // 
            // tabConversations
            // 
            tabConversations.Controls.Add(dgvConversations);
            tabConversations.Controls.Add(btnRefreshConversations);
            tabConversations.Controls.Add(btnDeleteConversation);
            tabConversations.Controls.Add(btnViewMessages);
            tabConversations.Location = new System.Drawing.Point(4, 34);
            tabConversations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabConversations.Name = "tabConversations";
            tabConversations.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabConversations.Size = new System.Drawing.Size(1421, 925);
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
            dgvConversations.Location = new System.Drawing.Point(14, 83);
            dgvConversations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dgvConversations.MultiSelect = false;
            dgvConversations.Name = "dgvConversations";
            dgvConversations.ReadOnly = true;
            dgvConversations.RowHeadersWidth = 62;
            dgvConversations.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvConversations.Size = new System.Drawing.Size(1386, 800);
            dgvConversations.TabIndex = 0;
            // 
            // btnRefreshConversations
            // 
            btnRefreshConversations.Location = new System.Drawing.Point(14, 17);
            btnRefreshConversations.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRefreshConversations.Name = "btnRefreshConversations";
            btnRefreshConversations.Size = new System.Drawing.Size(143, 50);
            btnRefreshConversations.TabIndex = 1;
            btnRefreshConversations.Text = "Làm mới";
            btnRefreshConversations.UseVisualStyleBackColor = true;
            // 
            // btnDeleteConversation
            // 
            btnDeleteConversation.Location = new System.Drawing.Point(171, 17);
            btnDeleteConversation.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnDeleteConversation.Name = "btnDeleteConversation";
            btnDeleteConversation.Size = new System.Drawing.Size(143, 50);
            btnDeleteConversation.TabIndex = 2;
            btnDeleteConversation.Text = "Xóa";
            btnDeleteConversation.UseVisualStyleBackColor = true;
            // 
            // btnViewMessages
            // 
            btnViewMessages.Location = new System.Drawing.Point(329, 17);
            btnViewMessages.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnViewMessages.Name = "btnViewMessages";
            btnViewMessages.Size = new System.Drawing.Size(143, 50);
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
            tabMessages.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabMessages.Name = "tabMessages";
            tabMessages.Size = new System.Drawing.Size(1421, 925);
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
            dgvMessages.Location = new System.Drawing.Point(14, 83);
            dgvMessages.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dgvMessages.MultiSelect = false;
            dgvMessages.Name = "dgvMessages";
            dgvMessages.ReadOnly = true;
            dgvMessages.RowHeadersWidth = 62;
            dgvMessages.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvMessages.Size = new System.Drawing.Size(1386, 800);
            dgvMessages.TabIndex = 0;
            // 
            // btnRefreshMessages
            // 
            btnRefreshMessages.Location = new System.Drawing.Point(14, 17);
            btnRefreshMessages.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRefreshMessages.Name = "btnRefreshMessages";
            btnRefreshMessages.Size = new System.Drawing.Size(143, 50);
            btnRefreshMessages.TabIndex = 1;
            btnRefreshMessages.Text = "Làm mới";
            btnRefreshMessages.UseVisualStyleBackColor = true;
            // 
            // btnDeleteMessage
            // 
            btnDeleteMessage.Location = new System.Drawing.Point(171, 17);
            btnDeleteMessage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnDeleteMessage.Name = "btnDeleteMessage";
            btnDeleteMessage.Size = new System.Drawing.Size(143, 50);
            btnDeleteMessage.TabIndex = 2;
            btnDeleteMessage.Text = "Xóa";
            btnDeleteMessage.UseVisualStyleBackColor = true;
            // 
            // tabAuditLogs
            // 
            tabAuditLogs.Controls.Add(dgvAuditLogs);
            tabAuditLogs.Controls.Add(btnRefreshLogs);
            tabAuditLogs.Location = new System.Drawing.Point(4, 34);
            tabAuditLogs.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabAuditLogs.Name = "tabAuditLogs";
            tabAuditLogs.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tabAuditLogs.Size = new System.Drawing.Size(1421, 925);
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
            dgvAuditLogs.Location = new System.Drawing.Point(14, 83);
            dgvAuditLogs.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dgvAuditLogs.MultiSelect = false;
            dgvAuditLogs.Name = "dgvAuditLogs";
            dgvAuditLogs.ReadOnly = true;
            dgvAuditLogs.RowHeadersWidth = 62;
            dgvAuditLogs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgvAuditLogs.Size = new System.Drawing.Size(1386, 800);
            dgvAuditLogs.TabIndex = 0;
            // 
            // btnRefreshLogs
            // 
            btnRefreshLogs.Location = new System.Drawing.Point(14, 17);
            btnRefreshLogs.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRefreshLogs.Name = "btnRefreshLogs";
            btnRefreshLogs.Size = new System.Drawing.Size(143, 50);
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
            lblCurrentUser.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblCurrentUser.Name = "lblCurrentUser";
            lblCurrentUser.Padding = new System.Windows.Forms.Padding(7, 8, 7, 8);
            lblCurrentUser.Size = new System.Drawing.Size(14, 37);
            lblCurrentUser.TabIndex = 1;
            // 
            // AdminPanelForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1429, 1000);
            Controls.Add(tabControl);
            Controls.Add(lblCurrentUser);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
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


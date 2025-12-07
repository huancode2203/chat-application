namespace ChatClient.Forms
{
    partial class ChatFormNew
    {
        private System.ComponentModel.IContainer components = null;

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
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.pnlConvButtons = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.btnProfile = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnViewMembers = new System.Windows.Forms.Button();
            this.btnPrivateChat = new System.Windows.Forms.Button();
            this.btnCreateGroup = new System.Windows.Forms.Button();
            this.lstConversations = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colMembers = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.pnlChatArea = new System.Windows.Forms.Panel();
            this.pnlMessages = new System.Windows.Forms.Panel();
            this.pnlInput = new System.Windows.Forms.Panel();
            this.btnSend = new System.Windows.Forms.Button();
            this.btnAttachment = new System.Windows.Forms.Button();
            this.cbSecurityLabel = new System.Windows.Forms.ComboBox();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.pnlReply = new System.Windows.Forms.Panel();
            this.btnCancelReply = new System.Windows.Forms.Button();
            this.lblReplyTo = new System.Windows.Forms.Label();
            this.lblChatTitle = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.pnlConvButtons.SuspendLayout();
            this.pnlChatArea.SuspendLayout();
            this.pnlInput.SuspendLayout();
            this.pnlReply.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1 - Conversations List
            // 
            this.splitContainer.Panel1.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.splitContainer.Panel1.Controls.Add(this.lstConversations);
            this.splitContainer.Panel1.Controls.Add(this.pnlConvButtons);
            this.splitContainer.Panel1MinSize = 250;
            // 
            // splitContainer.Panel2 - Chat Area
            // 
            this.splitContainer.Panel2.Controls.Add(this.pnlChatArea);
            this.splitContainer.Size = new System.Drawing.Size(1100, 678);
            this.splitContainer.SplitterDistance = 280;
            this.splitContainer.TabIndex = 0;
            // 
            // pnlConvButtons - reverse order for correct Dock.Top stacking
            // 
            this.pnlConvButtons.BackColor = System.Drawing.Color.FromArgb(240, 242, 245);
            this.pnlConvButtons.Controls.Add(this.btnCreateGroup);
            this.pnlConvButtons.Controls.Add(this.btnPrivateChat);
            this.pnlConvButtons.Controls.Add(this.btnViewMembers);
            this.pnlConvButtons.Controls.Add(this.btnRefresh);
            this.pnlConvButtons.Controls.Add(this.btnProfile);
            this.pnlConvButtons.Controls.Add(this.btnLogout);
            this.pnlConvButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlConvButtons.Location = new System.Drawing.Point(0, 448);
            this.pnlConvButtons.Name = "pnlConvButtons";
            this.pnlConvButtons.Padding = new System.Windows.Forms.Padding(8);
            this.pnlConvButtons.Size = new System.Drawing.Size(280, 230);
            this.pnlConvButtons.TabIndex = 1;
            // 
            // btnCreateGroup
            // 
            this.btnCreateGroup.BackColor = System.Drawing.Color.FromArgb(0, 132, 255);
            this.btnCreateGroup.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCreateGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCreateGroup.FlatAppearance.BorderSize = 0;
            this.btnCreateGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreateGroup.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCreateGroup.ForeColor = System.Drawing.Color.White;
            this.btnCreateGroup.Location = new System.Drawing.Point(8, 8);
            this.btnCreateGroup.Name = "btnCreateGroup";
            this.btnCreateGroup.Size = new System.Drawing.Size(264, 34);
            this.btnCreateGroup.TabIndex = 0;
            this.btnCreateGroup.Text = "➕ Tạo nhóm mới";
            this.btnCreateGroup.UseVisualStyleBackColor = false;
            // 
            // btnPrivateChat
            // 
            this.btnPrivateChat.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            this.btnPrivateChat.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrivateChat.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnPrivateChat.FlatAppearance.BorderSize = 0;
            this.btnPrivateChat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrivateChat.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnPrivateChat.ForeColor = System.Drawing.Color.White;
            this.btnPrivateChat.Location = new System.Drawing.Point(8, 42);
            this.btnPrivateChat.Name = "btnPrivateChat";
            this.btnPrivateChat.Size = new System.Drawing.Size(264, 34);
            this.btnPrivateChat.TabIndex = 1;
            this.btnPrivateChat.Text = "💬 Chat riêng";
            this.btnPrivateChat.UseVisualStyleBackColor = false;
            // 
            // btnViewMembers
            // 
            this.btnViewMembers.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.btnViewMembers.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnViewMembers.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnViewMembers.FlatAppearance.BorderSize = 0;
            this.btnViewMembers.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewMembers.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnViewMembers.ForeColor = System.Drawing.Color.White;
            this.btnViewMembers.Location = new System.Drawing.Point(8, 76);
            this.btnViewMembers.Name = "btnViewMembers";
            this.btnViewMembers.Size = new System.Drawing.Size(264, 34);
            this.btnViewMembers.TabIndex = 2;
            this.btnViewMembers.Text = "👥 Xem thành viên";
            this.btnViewMembers.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnRefresh.ForeColor = System.Drawing.Color.Black;
            this.btnRefresh.Location = new System.Drawing.Point(8, 110);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(264, 34);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "🔄 Làm mới";
            this.btnRefresh.UseVisualStyleBackColor = false;
            // 
            // btnProfile
            // 
            this.btnProfile.BackColor = System.Drawing.Color.FromArgb(23, 162, 184);
            this.btnProfile.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnProfile.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnProfile.FlatAppearance.BorderSize = 0;
            this.btnProfile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProfile.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnProfile.ForeColor = System.Drawing.Color.White;
            this.btnProfile.Location = new System.Drawing.Point(8, 144);
            this.btnProfile.Name = "btnProfile";
            this.btnProfile.Size = new System.Drawing.Size(264, 34);
            this.btnProfile.TabIndex = 4;
            this.btnProfile.Text = "👤 Thông tin cá nhân";
            this.btnProfile.UseVisualStyleBackColor = false;
            // 
            // btnLogout
            // 
            this.btnLogout.BackColor = System.Drawing.Color.FromArgb(220, 53, 69);
            this.btnLogout.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogout.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnLogout.FlatAppearance.BorderSize = 0;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(8, 178);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(264, 34);
            this.btnLogout.TabIndex = 5;
            this.btnLogout.Text = "🚪 Đăng xuất";
            this.btnLogout.UseVisualStyleBackColor = false;
            // 
            // lstConversations
            // 
            this.lstConversations.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstConversations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colMembers,
            this.colType});
            this.lstConversations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstConversations.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lstConversations.FullRowSelect = true;
            this.lstConversations.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstConversations.HideSelection = false;
            this.lstConversations.Location = new System.Drawing.Point(0, 0);
            this.lstConversations.MultiSelect = false;
            this.lstConversations.Name = "lstConversations";
            this.lstConversations.Size = new System.Drawing.Size(280, 478);
            this.lstConversations.TabIndex = 0;
            this.lstConversations.UseCompatibleStateImageBehavior = false;
            this.lstConversations.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            this.colName.Text = "Cuộc trò chuyện";
            this.colName.Width = 160;
            // 
            // colMembers
            // 
            this.colMembers.Text = "👥";
            this.colMembers.Width = 45;
            // 
            // colType
            // 
            this.colType.Text = "Loại";
            this.colType.Width = 55;
            // 
            // pnlChatArea - correct order for Dock layout
            // 
            this.pnlChatArea.BackColor = System.Drawing.Color.FromArgb(245, 245, 248);
            this.pnlChatArea.Controls.Add(this.pnlInput);
            this.pnlChatArea.Controls.Add(this.pnlMessages);
            this.pnlChatArea.Controls.Add(this.lblChatTitle);
            this.pnlChatArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChatArea.Location = new System.Drawing.Point(0, 0);
            this.pnlChatArea.Name = "pnlChatArea";
            this.pnlChatArea.Size = new System.Drawing.Size(816, 678);
            this.pnlChatArea.TabIndex = 0;
            // 
            // lblChatTitle
            // 
            this.lblChatTitle.BackColor = System.Drawing.Color.FromArgb(0, 132, 255);
            this.lblChatTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChatTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblChatTitle.ForeColor = System.Drawing.Color.White;
            this.lblChatTitle.Location = new System.Drawing.Point(0, 0);
            this.lblChatTitle.Name = "lblChatTitle";
            this.lblChatTitle.Padding = new System.Windows.Forms.Padding(15, 0, 0, 0);
            this.lblChatTitle.Size = new System.Drawing.Size(816, 45);
            this.lblChatTitle.TabIndex = 0;
            this.lblChatTitle.Text = "💬 Chọn cuộc trò chuyện";
            this.lblChatTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlMessages
            // 
            this.pnlMessages.AutoScroll = true;
            this.pnlMessages.BackColor = System.Drawing.Color.FromArgb(245, 245, 248);
            this.pnlMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMessages.Location = new System.Drawing.Point(0, 45);
            this.pnlMessages.Name = "pnlMessages";
            this.pnlMessages.Padding = new System.Windows.Forms.Padding(10);
            this.pnlMessages.Size = new System.Drawing.Size(816, 483);
            this.pnlMessages.TabIndex = 1;
            // 
            // pnlInput
            // 
            this.pnlInput.BackColor = System.Drawing.Color.White;
            this.pnlInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlInput.Controls.Add(this.pnlReply);
            this.pnlInput.Controls.Add(this.txtMessage);
            this.pnlInput.Controls.Add(this.cbSecurityLabel);
            this.pnlInput.Controls.Add(this.btnAttachment);
            this.pnlInput.Controls.Add(this.btnSend);
            this.pnlInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlInput.Location = new System.Drawing.Point(0, 528);
            this.pnlInput.Name = "pnlInput";
            this.pnlInput.Padding = new System.Windows.Forms.Padding(10);
            this.pnlInput.Size = new System.Drawing.Size(816, 150);
            this.pnlInput.TabIndex = 2;
            // 
            // pnlReply
            // 
            this.pnlReply.BackColor = System.Drawing.Color.FromArgb(230, 243, 255);
            this.pnlReply.Controls.Add(this.lblReplyTo);
            this.pnlReply.Controls.Add(this.btnCancelReply);
            this.pnlReply.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlReply.Location = new System.Drawing.Point(10, 10);
            this.pnlReply.Name = "pnlReply";
            this.pnlReply.Size = new System.Drawing.Size(794, 32);
            this.pnlReply.TabIndex = 0;
            this.pnlReply.Visible = false;
            // 
            // lblReplyTo
            // 
            this.lblReplyTo.AutoSize = true;
            this.lblReplyTo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.lblReplyTo.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.lblReplyTo.Location = new System.Drawing.Point(10, 7);
            this.lblReplyTo.Name = "lblReplyTo";
            this.lblReplyTo.Size = new System.Drawing.Size(100, 20);
            this.lblReplyTo.TabIndex = 0;
            this.lblReplyTo.Text = "↩ Trả lời: ...";
            // 
            // btnCancelReply
            // 
            this.btnCancelReply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancelReply.FlatAppearance.BorderSize = 0;
            this.btnCancelReply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancelReply.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnCancelReply.ForeColor = System.Drawing.Color.FromArgb(150, 150, 150);
            this.btnCancelReply.Location = new System.Drawing.Point(760, 2);
            this.btnCancelReply.Name = "btnCancelReply";
            this.btnCancelReply.Size = new System.Drawing.Size(30, 28);
            this.btnCancelReply.TabIndex = 1;
            this.btnCancelReply.Text = "✕";
            this.btnCancelReply.UseVisualStyleBackColor = true;
            // 
            // txtMessage
            // 
            this.txtMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtMessage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtMessage.Location = new System.Drawing.Point(10, 48);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessage.Size = new System.Drawing.Size(560, 88);
            this.txtMessage.TabIndex = 1;
            // 
            // cbSecurityLabel
            // 
            this.cbSecurityLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSecurityLabel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSecurityLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cbSecurityLabel.FormattingEnabled = true;
            this.cbSecurityLabel.Location = new System.Drawing.Point(580, 48);
            this.cbSecurityLabel.Name = "cbSecurityLabel";
            this.cbSecurityLabel.Size = new System.Drawing.Size(120, 28);
            this.cbSecurityLabel.TabIndex = 2;
            // 
            // btnAttachment
            // 
            this.btnAttachment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAttachment.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.btnAttachment.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAttachment.FlatAppearance.BorderSize = 0;
            this.btnAttachment.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAttachment.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.btnAttachment.ForeColor = System.Drawing.Color.White;
            this.btnAttachment.Location = new System.Drawing.Point(580, 85);
            this.btnAttachment.Name = "btnAttachment";
            this.btnAttachment.Size = new System.Drawing.Size(105, 50);
            this.btnAttachment.TabIndex = 3;
            this.btnAttachment.Text = "📎 Đính kèm";
            this.btnAttachment.UseVisualStyleBackColor = false;
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(0, 132, 255);
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.Location = new System.Drawing.Point(695, 48);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(105, 87);
            this.btnSend.TabIndex = 4;
            this.btnSend.Text = "Gửi ➤";
            this.btnSend.UseVisualStyleBackColor = false;
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 678);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1100, 26);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(40, 167, 69);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(68, 20);
            this.lblStatus.Text = "Sẵn sàng";
            // 
            // ChatFormNew
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 704);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.statusStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "ChatFormNew";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "💬 Chat Application";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.pnlConvButtons.ResumeLayout(false);
            this.pnlChatArea.ResumeLayout(false);
            this.pnlInput.ResumeLayout(false);
            this.pnlInput.PerformLayout();
            this.pnlReply.ResumeLayout(false);
            this.pnlReply.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel pnlConvButtons;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Button btnProfile;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnViewMembers;
        private System.Windows.Forms.Button btnPrivateChat;
        private System.Windows.Forms.Button btnCreateGroup;
        private System.Windows.Forms.ListView lstConversations;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colMembers;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.Panel pnlChatArea;
        private System.Windows.Forms.Label lblChatTitle;
        private System.Windows.Forms.Panel pnlMessages;
        private System.Windows.Forms.Panel pnlInput;
        private System.Windows.Forms.Panel pnlReply;
        private System.Windows.Forms.Label lblReplyTo;
        private System.Windows.Forms.Button btnCancelReply;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.ComboBox cbSecurityLabel;
        private System.Windows.Forms.Button btnAttachment;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}

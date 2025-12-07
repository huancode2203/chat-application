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
            splitContainer = new SplitContainer();
            lstConversations = new ListView();
            colName = new ColumnHeader();
            colMembers = new ColumnHeader();
            colType = new ColumnHeader();
            pnlConvButtons = new Panel();
            btnCreateGroup = new Button();
            btnPrivateChat = new Button();
            btnViewMembers = new Button();
            btnRefresh = new Button();
            btnProfile = new Button();
            btnLogout = new Button();
            pnlChatArea = new Panel();
            pnlInput = new Panel();
            pnlReply = new Panel();
            lblReplyTo = new Label();
            btnCancelReply = new Button();
            txtMessage = new TextBox();
            cbSecurityLabel = new ComboBox();
            btnAttachment = new Button();
            btnSend = new Button();
            pnlMessages = new Panel();
            lblChatTitle = new Label();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            pnlConvButtons.SuspendLayout();
            pnlChatArea.SuspendLayout();
            pnlInput.SuspendLayout();
            pnlReply.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.FixedPanel = FixedPanel.Panel1;
            splitContainer.Location = new Point(0, 0);
            splitContainer.Margin = new Padding(4, 4, 4, 4);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.BackColor = Color.FromArgb(248, 249, 250);
            splitContainer.Panel1.Controls.Add(lstConversations);
            splitContainer.Panel1.Controls.Add(pnlConvButtons);
            splitContainer.Panel1MinSize = 250;
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(pnlChatArea);
            splitContainer.Size = new Size(1644, 848);
            splitContainer.SplitterDistance = 350;
            splitContainer.SplitterWidth = 5;
            splitContainer.TabIndex = 0;
            // 
            // lstConversations
            // 
            lstConversations.BorderStyle = BorderStyle.None;
            lstConversations.Columns.AddRange(new ColumnHeader[] { colName, colMembers, colType });
            lstConversations.Dock = DockStyle.Fill;
            lstConversations.Font = new Font("Segoe UI", 9.5F);
            lstConversations.FullRowSelect = true;
            lstConversations.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lstConversations.Location = new Point(0, 0);
            lstConversations.Margin = new Padding(4, 4, 4, 4);
            lstConversations.MultiSelect = false;
            lstConversations.Name = "lstConversations";
            lstConversations.Size = new Size(350, 560);
            lstConversations.TabIndex = 0;
            lstConversations.UseCompatibleStateImageBehavior = false;
            lstConversations.View = View.Details;
            // 
            // colName
            // 
            colName.Text = "Cuộc trò chuyện";
            colName.Width = 160;
            // 
            // colMembers
            // 
            colMembers.Text = "👥";
            colMembers.Width = 45;
            // 
            // colType
            // 
            colType.Text = "Loại";
            colType.Width = 55;
            // 
            // pnlConvButtons
            // 
            pnlConvButtons.BackColor = Color.FromArgb(240, 242, 245);
            pnlConvButtons.Controls.Add(btnCreateGroup);
            pnlConvButtons.Controls.Add(btnPrivateChat);
            pnlConvButtons.Controls.Add(btnViewMembers);
            pnlConvButtons.Controls.Add(btnRefresh);
            pnlConvButtons.Controls.Add(btnProfile);
            pnlConvButtons.Controls.Add(btnLogout);
            pnlConvButtons.Dock = DockStyle.Bottom;
            pnlConvButtons.Location = new Point(0, 560);
            pnlConvButtons.Margin = new Padding(4, 4, 4, 4);
            pnlConvButtons.Name = "pnlConvButtons";
            pnlConvButtons.Padding = new Padding(10, 10, 10, 10);
            pnlConvButtons.Size = new Size(350, 288);
            pnlConvButtons.TabIndex = 1;
            // 
            // btnCreateGroup
            // 
            btnCreateGroup.BackColor = Color.FromArgb(0, 132, 255);
            btnCreateGroup.Cursor = Cursors.Hand;
            btnCreateGroup.Dock = DockStyle.Top;
            btnCreateGroup.FlatAppearance.BorderSize = 0;
            btnCreateGroup.FlatStyle = FlatStyle.Flat;
            btnCreateGroup.Font = new Font("Segoe UI", 9.5F);
            btnCreateGroup.ForeColor = Color.White;
            btnCreateGroup.Location = new Point(10, 220);
            btnCreateGroup.Margin = new Padding(4, 4, 4, 4);
            btnCreateGroup.Name = "btnCreateGroup";
            btnCreateGroup.Size = new Size(330, 42);
            btnCreateGroup.TabIndex = 0;
            btnCreateGroup.Text = "➕ Tạo nhóm mới";
            btnCreateGroup.UseVisualStyleBackColor = false;
            // 
            // btnPrivateChat
            // 
            btnPrivateChat.BackColor = Color.FromArgb(40, 167, 69);
            btnPrivateChat.Cursor = Cursors.Hand;
            btnPrivateChat.Dock = DockStyle.Top;
            btnPrivateChat.FlatAppearance.BorderSize = 0;
            btnPrivateChat.FlatStyle = FlatStyle.Flat;
            btnPrivateChat.Font = new Font("Segoe UI", 9.5F);
            btnPrivateChat.ForeColor = Color.White;
            btnPrivateChat.Location = new Point(10, 178);
            btnPrivateChat.Margin = new Padding(4, 4, 4, 4);
            btnPrivateChat.Name = "btnPrivateChat";
            btnPrivateChat.Size = new Size(330, 42);
            btnPrivateChat.TabIndex = 1;
            btnPrivateChat.Text = "💬 Chat riêng";
            btnPrivateChat.UseVisualStyleBackColor = false;
            // 
            // btnViewMembers
            // 
            btnViewMembers.BackColor = Color.FromArgb(108, 117, 125);
            btnViewMembers.Cursor = Cursors.Hand;
            btnViewMembers.Dock = DockStyle.Top;
            btnViewMembers.FlatAppearance.BorderSize = 0;
            btnViewMembers.FlatStyle = FlatStyle.Flat;
            btnViewMembers.Font = new Font("Segoe UI", 9.5F);
            btnViewMembers.ForeColor = Color.White;
            btnViewMembers.Location = new Point(10, 136);
            btnViewMembers.Margin = new Padding(4, 4, 4, 4);
            btnViewMembers.Name = "btnViewMembers";
            btnViewMembers.Size = new Size(330, 42);
            btnViewMembers.TabIndex = 2;
            btnViewMembers.Text = "👥 Xem thành viên";
            btnViewMembers.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            btnRefresh.BackColor = Color.FromArgb(255, 193, 7);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Dock = DockStyle.Top;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Font = new Font("Segoe UI", 9.5F);
            btnRefresh.ForeColor = Color.Black;
            btnRefresh.Location = new Point(10, 94);
            btnRefresh.Margin = new Padding(4, 4, 4, 4);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(330, 42);
            btnRefresh.TabIndex = 3;
            btnRefresh.Text = "🔄 Làm mới";
            btnRefresh.UseVisualStyleBackColor = false;
            // 
            // btnProfile
            // 
            btnProfile.BackColor = Color.FromArgb(23, 162, 184);
            btnProfile.Cursor = Cursors.Hand;
            btnProfile.Dock = DockStyle.Top;
            btnProfile.FlatAppearance.BorderSize = 0;
            btnProfile.FlatStyle = FlatStyle.Flat;
            btnProfile.Font = new Font("Segoe UI", 9.5F);
            btnProfile.ForeColor = Color.White;
            btnProfile.Location = new Point(10, 52);
            btnProfile.Margin = new Padding(4, 4, 4, 4);
            btnProfile.Name = "btnProfile";
            btnProfile.Size = new Size(330, 42);
            btnProfile.TabIndex = 4;
            btnProfile.Text = "👤 Thông tin cá nhân";
            btnProfile.UseVisualStyleBackColor = false;
            // 
            // btnLogout
            // 
            btnLogout.BackColor = Color.FromArgb(220, 53, 69);
            btnLogout.Cursor = Cursors.Hand;
            btnLogout.Dock = DockStyle.Top;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 9.5F);
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(10, 10);
            btnLogout.Margin = new Padding(4, 4, 4, 4);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(330, 42);
            btnLogout.TabIndex = 5;
            btnLogout.Text = "🚪 Đăng xuất";
            btnLogout.UseVisualStyleBackColor = false;
            // 
            // pnlChatArea
            // 
            pnlChatArea.BackColor = Color.FromArgb(245, 245, 248);
            pnlChatArea.Controls.Add(pnlInput);
            pnlChatArea.Controls.Add(pnlMessages);
            pnlChatArea.Controls.Add(lblChatTitle);
            pnlChatArea.Dock = DockStyle.Fill;
            pnlChatArea.Location = new Point(0, 0);
            pnlChatArea.Margin = new Padding(4, 4, 4, 4);
            pnlChatArea.Name = "pnlChatArea";
            pnlChatArea.Size = new Size(1289, 848);
            pnlChatArea.TabIndex = 0;
            // 
            // pnlInput
            // 
            pnlInput.BackColor = Color.White;
            pnlInput.BorderStyle = BorderStyle.FixedSingle;
            pnlInput.Controls.Add(pnlReply);
            pnlInput.Controls.Add(txtMessage);
            pnlInput.Controls.Add(cbSecurityLabel);
            pnlInput.Controls.Add(btnAttachment);
            pnlInput.Controls.Add(btnSend);
            pnlInput.Dock = DockStyle.Bottom;
            pnlInput.Location = new Point(0, 661);
            pnlInput.Margin = new Padding(4, 4, 4, 4);
            pnlInput.Name = "pnlInput";
            pnlInput.Padding = new Padding(12, 12, 12, 12);
            pnlInput.Size = new Size(1289, 187);
            pnlInput.TabIndex = 2;
            // 
            // pnlReply
            // 
            pnlReply.BackColor = Color.FromArgb(230, 243, 255);
            pnlReply.Controls.Add(lblReplyTo);
            pnlReply.Controls.Add(btnCancelReply);
            pnlReply.Dock = DockStyle.Top;
            pnlReply.Location = new Point(12, 12);
            pnlReply.Margin = new Padding(4, 4, 4, 4);
            pnlReply.Name = "pnlReply";
            pnlReply.Size = new Size(1263, 40);
            pnlReply.TabIndex = 0;
            pnlReply.Visible = false;
            // 
            // lblReplyTo
            // 
            lblReplyTo.AutoSize = true;
            lblReplyTo.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblReplyTo.ForeColor = Color.FromArgb(80, 80, 80);
            lblReplyTo.Location = new Point(12, 9);
            lblReplyTo.Margin = new Padding(4, 0, 4, 0);
            lblReplyTo.Name = "lblReplyTo";
            lblReplyTo.Size = new Size(104, 25);
            lblReplyTo.TabIndex = 0;
            lblReplyTo.Text = "↩ Trả lời: ...";
            // 
            // btnCancelReply
            // 
            btnCancelReply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancelReply.FlatAppearance.BorderSize = 0;
            btnCancelReply.FlatStyle = FlatStyle.Flat;
            btnCancelReply.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCancelReply.ForeColor = Color.FromArgb(150, 150, 150);
            btnCancelReply.Location = new Point(1221, 2);
            btnCancelReply.Margin = new Padding(4, 4, 4, 4);
            btnCancelReply.Name = "btnCancelReply";
            btnCancelReply.Size = new Size(38, 35);
            btnCancelReply.TabIndex = 1;
            btnCancelReply.Text = "✕";
            btnCancelReply.UseVisualStyleBackColor = true;
            // 
            // txtMessage
            // 
            txtMessage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtMessage.BorderStyle = BorderStyle.FixedSingle;
            txtMessage.Font = new Font("Segoe UI", 10F);
            txtMessage.Location = new Point(12, 60);
            txtMessage.Margin = new Padding(4, 4, 4, 4);
            txtMessage.Multiline = true;
            txtMessage.Name = "txtMessage";
            txtMessage.ScrollBars = ScrollBars.Vertical;
            txtMessage.Size = new Size(904, 110);
            txtMessage.TabIndex = 1;
            // 
            // cbSecurityLabel
            // 
            cbSecurityLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cbSecurityLabel.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSecurityLabel.Font = new Font("Segoe UI", 9F);
            cbSecurityLabel.FormattingEnabled = true;
            cbSecurityLabel.Location = new Point(941, 62);
            cbSecurityLabel.Margin = new Padding(4, 4, 4, 4);
            cbSecurityLabel.Name = "cbSecurityLabel";
            cbSecurityLabel.Size = new Size(172, 33);
            cbSecurityLabel.TabIndex = 2;
            // 
            // btnAttachment
            // 
            btnAttachment.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAttachment.BackColor = Color.FromArgb(108, 117, 125);
            btnAttachment.Cursor = Cursors.Hand;
            btnAttachment.FlatAppearance.BorderSize = 0;
            btnAttachment.FlatStyle = FlatStyle.Flat;
            btnAttachment.Font = new Font("Segoe UI", 12F);
            btnAttachment.ForeColor = Color.White;
            btnAttachment.Location = new Point(941, 115);
            btnAttachment.Margin = new Padding(4, 4, 4, 4);
            btnAttachment.Name = "btnAttachment";
            btnAttachment.Size = new Size(172, 45);
            btnAttachment.TabIndex = 3;
            btnAttachment.Text = "📎 Đính kèm";
            btnAttachment.UseVisualStyleBackColor = false;
            // 
            // btnSend
            // 
            btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSend.BackColor = Color.FromArgb(0, 132, 255);
            btnSend.Cursor = Cursors.Hand;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnSend.ForeColor = Color.White;
            btnSend.Location = new Point(1121, 62);
            btnSend.Margin = new Padding(4, 4, 4, 4);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(141, 98);
            btnSend.TabIndex = 4;
            btnSend.Text = "Gửi ➤";
            btnSend.UseVisualStyleBackColor = false;
            // 
            // pnlMessages
            // 
            pnlMessages.AutoScroll = true;
            pnlMessages.BackColor = Color.FromArgb(245, 245, 248);
            pnlMessages.Dock = DockStyle.Fill;
            pnlMessages.Location = new Point(0, 56);
            pnlMessages.Margin = new Padding(4, 4, 4, 4);
            pnlMessages.Name = "pnlMessages";
            pnlMessages.Padding = new Padding(12, 12, 12, 12);
            pnlMessages.Size = new Size(1289, 792);
            pnlMessages.TabIndex = 1;
            // 
            // lblChatTitle
            // 
            lblChatTitle.BackColor = Color.FromArgb(0, 132, 255);
            lblChatTitle.Dock = DockStyle.Top;
            lblChatTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblChatTitle.ForeColor = Color.White;
            lblChatTitle.Location = new Point(0, 0);
            lblChatTitle.Margin = new Padding(4, 0, 4, 0);
            lblChatTitle.Name = "lblChatTitle";
            lblChatTitle.Padding = new Padding(19, 0, 0, 0);
            lblChatTitle.Size = new Size(1289, 56);
            lblChatTitle.TabIndex = 0;
            lblChatTitle.Text = "💬 Chọn cuộc trò chuyện";
            lblChatTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // statusStrip
            // 
            statusStrip.BackColor = Color.FromArgb(248, 249, 250);
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip.Location = new Point(0, 848);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 18, 0);
            statusStrip.Size = new Size(1644, 32);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.ForeColor = Color.FromArgb(40, 167, 69);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(84, 25);
            lblStatus.Text = "Sẵn sàng";
            // 
            // ChatFormNew
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1644, 880);
            Controls.Add(splitContainer);
            Controls.Add(statusStrip);
            Font = new Font("Segoe UI", 9F);
            Margin = new Padding(4, 4, 4, 4);
            MinimumSize = new Size(1120, 736);
            Name = "ChatFormNew";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "💬 Chat Application";
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            pnlConvButtons.ResumeLayout(false);
            pnlChatArea.ResumeLayout(false);
            pnlInput.ResumeLayout(false);
            pnlInput.PerformLayout();
            pnlReply.ResumeLayout(false);
            pnlReply.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
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

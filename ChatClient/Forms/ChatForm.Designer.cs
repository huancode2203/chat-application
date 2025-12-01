namespace ChatClient.Forms
{
    partial class ChatForm
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.grpConversations = new System.Windows.Forms.GroupBox();
            this.lstConversations = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colMembers = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnPrivateChat = new System.Windows.Forms.Button();
            this.btnCreateGroup = new System.Windows.Forms.Button();
            this.grpChat = new System.Windows.Forms.GroupBox();
            this.lstMessages = new System.Windows.Forms.ListView();
            this.colTime = new System.Windows.Forms.ColumnHeader();
            this.colSender = new System.Windows.Forms.ColumnHeader();
            this.colContent = new System.Windows.Forms.ColumnHeader();
            this.colLabel = new System.Windows.Forms.ColumnHeader();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnAttachment = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.btnViewMembers = new System.Windows.Forms.Button();
            this.cbLabel = new System.Windows.Forms.ComboBox();
            this.lblLabel = new System.Windows.Forms.Label();
            this.txtReceiver = new System.Windows.Forms.TextBox();
            this.lblReceiver = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnLogout = new System.Windows.Forms.ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.grpConversations.SuspendLayout();
            this.panel1.SuspendLayout();
            this.grpChat.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.grpConversations);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grpChat);
            this.splitContainer1.Panel2.Controls.Add(this.panel2);
            this.splitContainer1.Panel2.Controls.Add(this.panel3);
            this.splitContainer1.Size = new System.Drawing.Size(1200, 625);
            this.splitContainer1.SplitterDistance = 300;
            this.splitContainer1.TabIndex = 0;
            // 
            // grpConversations
            // 
            this.grpConversations.Controls.Add(this.lstConversations);
            this.grpConversations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpConversations.Location = new System.Drawing.Point(0, 0);
            this.grpConversations.Name = "grpConversations";
            this.grpConversations.Padding = new System.Windows.Forms.Padding(10);
            this.grpConversations.Size = new System.Drawing.Size(300, 555);
            this.grpConversations.TabIndex = 0;
            this.grpConversations.TabStop = false;
            this.grpConversations.Text = "Cuộc trò chuyện";
            // 
            // lstConversations
            // 
            this.lstConversations.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colMembers,
            this.colType});
            this.lstConversations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstConversations.FullRowSelect = true;
            this.lstConversations.GridLines = true;
            this.lstConversations.HideSelection = false;
            this.lstConversations.Location = new System.Drawing.Point(10, 26);
            this.lstConversations.MultiSelect = false;
            this.lstConversations.Name = "lstConversations";
            this.lstConversations.Size = new System.Drawing.Size(280, 519);
            this.lstConversations.TabIndex = 0;
            this.lstConversations.UseCompatibleStateImageBehavior = false;
            this.lstConversations.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            this.colName.Text = "Tên";
            this.colName.Width = 150;
            // 
            // colMembers
            // 
            this.colMembers.Text = "TV";
            this.colMembers.Width = 50;
            // 
            // colType
            // 
            this.colType.Text = "Loại";
            this.colType.Width = 70;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnPrivateChat);
            this.panel1.Controls.Add(this.btnCreateGroup);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 555);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(300, 70);
            this.panel1.TabIndex = 1;
            // 
            // btnPrivateChat
            // 
            this.btnPrivateChat.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnPrivateChat.Location = new System.Drawing.Point(10, 45);
            this.btnPrivateChat.Name = "btnPrivateChat";
            this.btnPrivateChat.Size = new System.Drawing.Size(280, 35);
            this.btnPrivateChat.TabIndex = 1;
            this.btnPrivateChat.Text = "💬 Chat riêng";
            this.btnPrivateChat.UseVisualStyleBackColor = true;
            // 
            // btnCreateGroup
            // 
            this.btnCreateGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCreateGroup.Location = new System.Drawing.Point(10, 10);
            this.btnCreateGroup.Name = "btnCreateGroup";
            this.btnCreateGroup.Size = new System.Drawing.Size(280, 35);
            this.btnCreateGroup.TabIndex = 0;
            this.btnCreateGroup.Text = "➕ Tạo nhóm";
            this.btnCreateGroup.UseVisualStyleBackColor = true;
            // 
            // grpChat
            // 
            this.grpChat.Controls.Add(this.lstMessages);
            this.grpChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpChat.Enabled = false;
            this.grpChat.Location = new System.Drawing.Point(0, 70);
            this.grpChat.Name = "grpChat";
            this.grpChat.Padding = new System.Windows.Forms.Padding(10);
            this.grpChat.Size = new System.Drawing.Size(896, 435);
            this.grpChat.TabIndex = 0;
            this.grpChat.TabStop = false;
            this.grpChat.Text = "Tin nhắn";
            // 
            // lstMessages
            // 
            this.lstMessages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTime,
            this.colSender,
            this.colContent,
            this.colLabel});
            this.lstMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstMessages.FullRowSelect = true;
            this.lstMessages.GridLines = true;
            this.lstMessages.HideSelection = false;
            this.lstMessages.Location = new System.Drawing.Point(10, 26);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(876, 399);
            this.lstMessages.TabIndex = 0;
            this.lstMessages.UseCompatibleStateImageBehavior = false;
            this.lstMessages.View = System.Windows.Forms.View.Details;
            // 
            // colTime
            // 
            this.colTime.Text = "Thời gian";
            this.colTime.Width = 100;
            // 
            // colSender
            // 
            this.colSender.Text = "Người gửi";
            this.colSender.Width = 120;
            // 
            // colContent
            // 
            this.colContent.Text = "Nội dung";
            this.colContent.Width = 550;
            // 
            // colLabel
            // 
            this.colLabel.Text = "Mức";
            this.colLabel.Width = 50;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnAttachment);
            this.panel2.Controls.Add(this.btnSend);
            this.panel2.Controls.Add(this.txtMessage);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(0, 505);
            this.panel2.Name = "panel2";
            this.panel2.Padding = new System.Windows.Forms.Padding(10);
            this.panel2.Size = new System.Drawing.Size(896, 120);
            this.panel2.TabIndex = 1;
            // 
            // btnAttachment
            // 
            this.btnAttachment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAttachment.Location = new System.Drawing.Point(656, 75);
            this.btnAttachment.Name = "btnAttachment";
            this.btnAttachment.Size = new System.Drawing.Size(110, 35);
            this.btnAttachment.TabIndex = 2;
            this.btnAttachment.Text = "📎 Đính kèm";
            this.btnAttachment.UseVisualStyleBackColor = true;
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.Location = new System.Drawing.Point(776, 75);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(110, 35);
            this.btnSend.TabIndex = 1;
            this.btnSend.Text = "📤 Gửi";
            this.btnSend.UseVisualStyleBackColor = true;
            // 
            // txtMessage
            // 
            this.txtMessage.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtMessage.Location = new System.Drawing.Point(10, 10);
            this.txtMessage.Multiline = true;
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtMessage.Size = new System.Drawing.Size(876, 60);
            this.txtMessage.TabIndex = 0;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.btnViewMembers);
            this.panel3.Controls.Add(this.cbLabel);
            this.panel3.Controls.Add(this.lblLabel);
            this.panel3.Controls.Add(this.txtReceiver);
            this.panel3.Controls.Add(this.lblReceiver);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Padding = new System.Windows.Forms.Padding(10);
            this.panel3.Size = new System.Drawing.Size(896, 70);
            this.panel3.TabIndex = 2;
            // 
            // btnViewMembers
            // 
            this.btnViewMembers.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewMembers.Location = new System.Drawing.Point(746, 25);
            this.btnViewMembers.Name = "btnViewMembers";
            this.btnViewMembers.Size = new System.Drawing.Size(140, 30);
            this.btnViewMembers.TabIndex = 4;
            this.btnViewMembers.Text = "👥 Thành viên";
            this.btnViewMembers.UseVisualStyleBackColor = true;
            // 
            // cbLabel
            // 
            this.cbLabel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLabel.FormattingEnabled = true;
            this.cbLabel.Items.AddRange(new object[] {
            "1 - LOW",
            "2 - MEDIUM",
            "3 - HIGH"});
            this.cbLabel.Location = new System.Drawing.Point(520, 27);
            this.cbLabel.Name = "cbLabel";
            this.cbLabel.Size = new System.Drawing.Size(200, 23);
            this.cbLabel.TabIndex = 3;
            // 
            // lblLabel
            // 
            this.lblLabel.AutoSize = true;
            this.lblLabel.Location = new System.Drawing.Point(420, 30);
            this.lblLabel.Name = "lblLabel";
            this.lblLabel.Size = new System.Drawing.Size(94, 15);
            this.lblLabel.TabIndex = 2;
            this.lblLabel.Text = "Mức bảo mật:";
            // 
            // txtReceiver
            // 
            this.txtReceiver.Location = new System.Drawing.Point(130, 27);
            this.txtReceiver.Name = "txtReceiver";
            this.txtReceiver.ReadOnly = true;
            this.txtReceiver.Size = new System.Drawing.Size(250, 23);
            this.txtReceiver.TabIndex = 1;
            // 
            // lblReceiver
            // 
            this.lblReceiver.AutoSize = true;
            this.lblReceiver.Location = new System.Drawing.Point(13, 30);
            this.lblReceiver.Name = "lblReceiver";
            this.lblReceiver.Size = new System.Drawing.Size(111, 15);
            this.lblReceiver.TabIndex = 0;
            this.lblReceiver.Text = "Cuộc trò chuyện:";
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 650);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(58, 17);
            this.lblStatus.Text = "Sẵn sàng";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnRefresh,
            this.toolStripSeparator1,
            this.btnLogout});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1200, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Image = null;
            this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(69, 22);
            this.btnRefresh.Text = "🔄 Làm mới";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnLogout
            // 
            this.btnLogout.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.btnLogout.Image = null;
            this.btnLogout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(78, 22);
            this.btnLogout.Text = "🚪 Đăng xuất";
            // 
            // ChatForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 672);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "ChatForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chat Nội Bộ";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.grpConversations.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.grpChat.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox grpConversations;
        private System.Windows.Forms.ListView lstConversations;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colMembers;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnPrivateChat;
        private System.Windows.Forms.Button btnCreateGroup;
        private System.Windows.Forms.GroupBox grpChat;
        private System.Windows.Forms.ListView lstMessages;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ColumnHeader colSender;
        private System.Windows.Forms.ColumnHeader colContent;
        private System.Windows.Forms.ColumnHeader colLabel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnAttachment;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button btnViewMembers;
        private System.Windows.Forms.ComboBox cbLabel;
        private System.Windows.Forms.Label lblLabel;
        private System.Windows.Forms.TextBox txtReceiver;
        private System.Windows.Forms.Label lblReceiver;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnLogout;
    }
}
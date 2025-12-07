namespace ChatClient.Forms
{
    partial class ChatForm
    {
        private System.ComponentModel.IContainer components = null;

        // Main controls
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.ListView lstConversations;
        private System.Windows.Forms.ColumnHeader colConversationName;
        private System.Windows.Forms.ColumnHeader colMemberCount;
        private System.Windows.Forms.ColumnHeader colType;
        private ChatClient.Controls.DoubleBufferedListView lstMessages;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ColumnHeader colSender;
        private System.Windows.Forms.ColumnHeader colContent;
        private System.Windows.Forms.ColumnHeader colLabel;
        private System.Windows.Forms.GroupBox grpChat;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.TextBox txtReceiver;
        private System.Windows.Forms.ComboBox cbLabel;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnCreateGroup;
        private System.Windows.Forms.Button btnPrivateChat;
        private System.Windows.Forms.Button btnViewMembers;
        private System.Windows.Forms.Button btnAttachment;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;

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
            splitContainer1 = new SplitContainer();
            panel1 = new Panel();
            lstConversations = new ListView();
            colConversationName = new ColumnHeader();
            colMemberCount = new ColumnHeader();
            colType = new ColumnHeader();
            btnRefresh = new Button();
            btnCreateGroup = new Button();
            btnPrivateChat = new Button();
            btnViewMembers = new Button();
            btnLogout = new Button();
            panel2 = new Panel();
            lstMessages = new ChatClient.Controls.DoubleBufferedListView();
            colTime = new ColumnHeader();
            colSender = new ColumnHeader();
            colContent = new ColumnHeader();
            colLabel = new ColumnHeader();
            grpChat = new GroupBox();
            txtReceiver = new TextBox();
            txtMessage = new TextBox();
            cbLabel = new ComboBox();
            btnAttachment = new Button();
            btnSend = new Button();
            toolStrip1 = new ToolStrip();
            statusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            grpChat.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 25);
            splitContainer1.Margin = new Padding(4);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panel1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panel2);
            splitContainer1.Size = new Size(1626, 895);
            splitContainer1.SplitterDistance = 406;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(lstConversations);
            panel1.Controls.Add(btnRefresh);
            panel1.Controls.Add(btnCreateGroup);
            panel1.Controls.Add(btnPrivateChat);
            panel1.Controls.Add(btnViewMembers);
            panel1.Controls.Add(btnLogout);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(4);
            panel1.Name = "panel1";
            panel1.Size = new Size(406, 895);
            panel1.TabIndex = 0;
            // 
            // lstConversations
            // 
            lstConversations.Columns.AddRange(new ColumnHeader[] { colConversationName, colMemberCount, colType });
            lstConversations.Dock = DockStyle.Fill;
            lstConversations.FullRowSelect = true;
            lstConversations.GridLines = true;
            lstConversations.Location = new Point(0, 0);
            lstConversations.Margin = new Padding(4);
            lstConversations.MultiSelect = false;
            lstConversations.Name = "lstConversations";
            lstConversations.Size = new Size(406, 670);
            lstConversations.TabIndex = 0;
            lstConversations.UseCompatibleStateImageBehavior = false;
            lstConversations.View = View.Details;
            // 
            // colConversationName
            // 
            colConversationName.Text = "Cuộc trò chuyện";
            colConversationName.Width = 150;
            // 
            // colMemberCount
            // 
            colMemberCount.Text = "Thành viên";
            colMemberCount.Width = 70;
            // 
            // colType
            // 
            colType.Text = "Loại";
            colType.Width = 80;
            // 
            // btnRefresh
            // 
            btnRefresh.Dock = DockStyle.Bottom;
            btnRefresh.Location = new Point(0, 670);
            btnRefresh.Margin = new Padding(4);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(406, 45);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "🔄 Làm mới";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnCreateGroup
            // 
            btnCreateGroup.Dock = DockStyle.Bottom;
            btnCreateGroup.Location = new Point(0, 715);
            btnCreateGroup.Margin = new Padding(4);
            btnCreateGroup.Name = "btnCreateGroup";
            btnCreateGroup.Size = new Size(406, 45);
            btnCreateGroup.TabIndex = 2;
            btnCreateGroup.Text = "👥 Tạo nhóm";
            btnCreateGroup.UseVisualStyleBackColor = true;
            // 
            // btnPrivateChat
            // 
            btnPrivateChat.Dock = DockStyle.Bottom;
            btnPrivateChat.Location = new Point(0, 760);
            btnPrivateChat.Margin = new Padding(4);
            btnPrivateChat.Name = "btnPrivateChat";
            btnPrivateChat.Size = new Size(406, 45);
            btnPrivateChat.TabIndex = 3;
            btnPrivateChat.Text = "💬 Chat riêng";
            btnPrivateChat.UseVisualStyleBackColor = true;
            // 
            // btnViewMembers
            // 
            btnViewMembers.Dock = DockStyle.Bottom;
            btnViewMembers.Location = new Point(0, 805);
            btnViewMembers.Margin = new Padding(4);
            btnViewMembers.Name = "btnViewMembers";
            btnViewMembers.Size = new Size(406, 45);
            btnViewMembers.TabIndex = 4;
            btnViewMembers.Text = "👤 Xem thành viên";
            btnViewMembers.UseVisualStyleBackColor = true;
            // 
            // btnLogout
            // 
            btnLogout.Dock = DockStyle.Bottom;
            btnLogout.Location = new Point(0, 850);
            btnLogout.Margin = new Padding(4);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(406, 45);
            btnLogout.TabIndex = 5;
            btnLogout.Text = "🚪 Đăng xuất";
            btnLogout.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            panel2.Controls.Add(lstMessages);
            panel2.Controls.Add(grpChat);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Margin = new Padding(4);
            panel2.Name = "panel2";
            panel2.Size = new Size(1215, 895);
            panel2.TabIndex = 0;
            // 
            // lstMessages
            // 
            lstMessages.Columns.AddRange(new ColumnHeader[] { colTime, colSender, colContent, colLabel });
            lstMessages.Dock = DockStyle.Fill;
            lstMessages.FullRowSelect = true;
            lstMessages.HeaderStyle = ColumnHeaderStyle.None;
            lstMessages.Location = new Point(0, 0);
            lstMessages.Margin = new Padding(4);
            lstMessages.Name = "lstMessages";
            lstMessages.OwnerDraw = true;
            lstMessages.Size = new Size(1215, 707);
            lstMessages.TabIndex = 0;
            lstMessages.UseCompatibleStateImageBehavior = false;
            lstMessages.View = View.Details;
            // 
            // colTime
            // 
            colTime.Text = "Thời gian";
            colTime.Width = 0;
            // 
            // colSender
            // 
            colSender.Text = "Người gửi";
            colSender.Width = 0;
            // 
            // colContent
            // 
            colContent.Text = "Nội dung";
            colContent.Width = 896;
            // 
            // colLabel
            // 
            colLabel.Text = "Mức độ";
            colLabel.Width = 0;
            // 
            // grpChat
            // 
            grpChat.Controls.Add(txtReceiver);
            grpChat.Controls.Add(txtMessage);
            grpChat.Controls.Add(cbLabel);
            grpChat.Controls.Add(btnAttachment);
            grpChat.Controls.Add(btnSend);
            grpChat.Dock = DockStyle.Bottom;
            grpChat.Enabled = false;
            grpChat.Location = new Point(0, 707);
            grpChat.Margin = new Padding(4);
            grpChat.Name = "grpChat";
            grpChat.Padding = new Padding(4);
            grpChat.Size = new Size(1215, 188);
            grpChat.TabIndex = 1;
            grpChat.TabStop = false;
            grpChat.Text = "💬 Trò chuyện";
            // 
            // txtReceiver
            // 
            txtReceiver.Location = new Point(19, 31);
            txtReceiver.Margin = new Padding(4);
            txtReceiver.Name = "txtReceiver";
            txtReceiver.ReadOnly = true;
            txtReceiver.Size = new Size(374, 31);
            txtReceiver.TabIndex = 0;
            // 
            // txtMessage
            // 
            txtMessage.Location = new Point(19, 75);
            txtMessage.Margin = new Padding(4);
            txtMessage.Multiline = true;
            txtMessage.Name = "txtMessage";
            txtMessage.ScrollBars = ScrollBars.Vertical;
            txtMessage.Size = new Size(812, 93);
            txtMessage.TabIndex = 1;
            // 
            // cbLabel
            // 
            cbLabel.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLabel.FormattingEnabled = true;
            cbLabel.Items.AddRange(new object[] { "1 - LOW", "2 - MEDIUM", "3 - HIGH" });
            cbLabel.Location = new Point(850, 75);
            cbLabel.Margin = new Padding(4);
            cbLabel.Name = "cbLabel";
            cbLabel.Size = new Size(249, 33);
            cbLabel.TabIndex = 2;
            // 
            // btnAttachment
            // 
            btnAttachment.Location = new Point(850, 119);
            btnAttachment.Margin = new Padding(4);
            btnAttachment.Name = "btnAttachment";
            btnAttachment.Size = new Size(119, 50);
            btnAttachment.TabIndex = 3;
            btnAttachment.Text = "📎 File";
            btnAttachment.UseVisualStyleBackColor = true;
            // 
            // btnSend
            // 
            btnSend.Location = new Point(981, 119);
            btnSend.Margin = new Padding(4);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(119, 50);
            btnSend.TabIndex = 4;
            btnSend.Text = "📤 Gửi";
            btnSend.UseVisualStyleBackColor = true;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(20, 20);
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1626, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip1.Location = new Point(0, 920);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 18, 0);
            statusStrip1.Size = new Size(1626, 32);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(84, 25);
            lblStatus.Text = "Sẵn sàng";
            // 
            // ChatForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1626, 952);
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Controls.Add(statusStrip1);
            Margin = new Padding(4);
            Name = "ChatForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Chat Application";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            grpChat.ResumeLayout(false);
            grpChat.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
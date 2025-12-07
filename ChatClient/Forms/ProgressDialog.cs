using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace ChatClient.Forms
{
    /// <summary>
    /// Dialog hiển thị tiến trình xử lý với giao diện hiện đại
    /// </summary>
    [DesignTimeVisible(false)]
    public class ProgressDialog : Form
    {
        private readonly Label _lblMessage;
        private readonly ProgressBar _progressBar;
        private readonly Label _lblPercent;
        private readonly Button _btnCancel;
        private readonly Panel _contentPanel;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsCancelled { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public event EventHandler CancelRequested;

        public ProgressDialog(string message, bool showCancelButton = false)
        {
            // Form settings
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(400, 150);
            BackColor = Color.FromArgb(245, 246, 250);
            ShowInTaskbar = false;
            TopMost = true;

            // Add shadow effect with border
            _contentPanel = new Panel
            {
                Location = new Point(2, 2),
                Size = new Size(396, 146),
                BackColor = Color.White
            };
            _contentPanel.Paint += (s, e) =>
            {
                // Draw border
                using var pen = new Pen(Color.FromArgb(220, 220, 220), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _contentPanel.Width - 1, _contentPanel.Height - 1);
            };

            // Title label
            var lblTitle = new Label
            {
                Text = "⏳ Đang xử lý...",
                Location = new Point(20, 15),
                Size = new Size(360, 25),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 30, 33),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Message label
            _lblMessage = new Label
            {
                Text = message,
                Location = new Point(20, 45),
                Size = new Size(360, 24),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(108, 117, 125),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            // Progress bar with modern style
            _progressBar = new ProgressBar
            {
                Location = new Point(20, 75),
                Size = new Size(showCancelButton ? 270 : 360, 24),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            // Percent label
            _lblPercent = new Label
            {
                Text = "",
                Location = new Point(295, 75),
                Size = new Size(55, 24),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(0, 132, 255),
                TextAlign = ContentAlignment.MiddleRight,
                Visible = false
            };

            // Cancel button
            _btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(300, 75),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand,
                Visible = showCancelButton
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) =>
            {
                IsCancelled = true;
                _btnCancel.Enabled = false;
                _btnCancel.Text = "Đang hủy...";
                CancelRequested?.Invoke(this, EventArgs.Empty);
            };

            // Info label
            var lblInfo = new Label
            {
                Text = "Vui lòng đợi...",
                Location = new Point(20, 108),
                Size = new Size(360, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(144, 149, 160),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _contentPanel.Controls.AddRange(new Control[] { 
                lblTitle, _lblMessage, _progressBar, _lblPercent, _btnCancel, lblInfo 
            });
            Controls.Add(_contentPanel);

            // Draw shadow
            Paint += (s, e) =>
            {
                using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
                e.Graphics.FillRectangle(shadowBrush, 4, 4, Width - 4, Height - 4);
            };
        }

        /// <summary>
        /// Cập nhật thông báo hiển thị
        /// </summary>
        public void UpdateMessage(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateMessage(message)));
                return;
            }
            _lblMessage.Text = message;
        }

        /// <summary>
        /// Cập nhật phần trăm tiến trình
        /// </summary>
        public void UpdateProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateProgress(percent)));
                return;
            }

            if (_progressBar.Style == ProgressBarStyle.Marquee)
            {
                _progressBar.Style = ProgressBarStyle.Continuous;
                _progressBar.Minimum = 0;
                _progressBar.Maximum = 100;
                _lblPercent.Visible = true;
            }

            _progressBar.Value = Math.Min(percent, 100);
            _lblPercent.Text = $"{percent}%";
        }

        /// <summary>
        /// Đặt lại về trạng thái không xác định tiến trình
        /// </summary>
        public void SetIndeterminate()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(SetIndeterminate));
                return;
            }

            _progressBar.Style = ProgressBarStyle.Marquee;
            _lblPercent.Visible = false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                // Add drop shadow
                const int CS_DROPSHADOW = 0x00020000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
    }

    /// <summary>
    /// Dialog xác nhận với giao diện hiện đại
    /// </summary>
    public class ConfirmDialog : Form
    {
        public enum ConfirmType
        {
            Question,
            Warning,
            Danger,
            Info
        }

        private DialogResult _result = DialogResult.Cancel;

        public ConfirmDialog(string title, string message, ConfirmType type = ConfirmType.Question)
        {
            // Form settings
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(400, 180);
            BackColor = Color.White;
            ShowInTaskbar = false;

            // Get colors based on type
            var (iconColor, iconText, confirmColor) = type switch
            {
                ConfirmType.Warning => (Color.FromArgb(255, 193, 7), "⚠️", Color.FromArgb(255, 193, 7)),
                ConfirmType.Danger => (Color.FromArgb(220, 53, 69), "⛔", Color.FromArgb(220, 53, 69)),
                ConfirmType.Info => (Color.FromArgb(23, 162, 184), "ℹ️", Color.FromArgb(23, 162, 184)),
                _ => (Color.FromArgb(0, 132, 255), "❓", Color.FromArgb(0, 132, 255))
            };

            // Header panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = iconColor
            };

            var lblTitle = new Label
            {
                Text = $"{iconText} {title}",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };
            headerPanel.Controls.Add(lblTitle);

            // Message label
            var lblMessage = new Label
            {
                Text = message,
                Location = new Point(20, 65),
                Size = new Size(360, 50),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(28, 30, 33),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Buttons panel
            var btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(245, 246, 250)
            };

            var btnConfirm = new Button
            {
                Text = "Xác nhận",
                Location = new Point(180, 10),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = confirmColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, e) =>
            {
                _result = DialogResult.Yes;
                Close();
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(290, 10),
                Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(108, 117, 125),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.Click += (s, e) =>
            {
                _result = DialogResult.No;
                Close();
            };

            btnPanel.Controls.AddRange(new Control[] { btnConfirm, btnCancel });

            Controls.AddRange(new Control[] { headerPanel, lblMessage, btnPanel });

            // Border
            Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        public new DialogResult ShowDialog(IWin32Window owner = null)
        {
            base.ShowDialog(owner);
            return _result;
        }

        /// <summary>
        /// Hiển thị dialog xác nhận nhanh
        /// </summary>
        public static DialogResult Show(string title, string message, ConfirmType type = ConfirmType.Question, IWin32Window owner = null)
        {
            using var dialog = new ConfirmDialog(title, message, type);
            return dialog.ShowDialog(owner);
        }
    }
}

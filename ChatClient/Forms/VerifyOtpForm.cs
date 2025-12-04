using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Services;
using Timer = System.Windows.Forms.Timer;

namespace ChatClient.Forms
{
    /// <summary>
    /// Form xác thực OTP với countdown timer và resend
    /// </summary>
    public partial class VerifyOtpForm : Form
    {
        private readonly string _username;
        private Timer _countdownTimer;
        private int _remainingSeconds = 600; // 10 minutes
        private int _resendCooldown = 0;

        public VerifyOtpForm(string username)
        {
            _username = username;
            InitializeComponent();
            CustomizeForm();
            SetupEventHandlers();
            StartCountdown();
        }

        private void CustomizeForm()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Font = new Font("Segoe UI", 9.5F);

            // Header gradient - Purple theme
            panelHeader.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    panelHeader.ClientRectangle,
                    Color.FromArgb(142, 68, 173),
                    Color.FromArgb(155, 89, 182),
                    45F))
                {
                    e.Graphics.FillRectangle(brush, panelHeader.ClientRectangle);
                }
            };

            // Display username
            lblUserInfo.Text = $"Tài khoản: {_username}";

            // Customize buttons
            CustomizeButton(btnVerify, Color.FromArgb(155, 89, 182), Color.White);
            CustomizeButton(btnResend, Color.FromArgb(52, 152, 219), Color.White);
            CustomizeButton(btnCancel, Color.FromArgb(149, 165, 166), Color.White);

            // OTP textboxes styling
            foreach (Control ctrl in panelOtpInputs.Controls)
            {
                if (ctrl is TextBox tb)
                {
                    CustomizeOtpTextBox(tb);
                }
            }

            // Initially disable resend
            btnResend.Enabled = false;
        }

        private void CustomizeButton(Button button, Color backColor, Color foreColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;

            Color originalColor = backColor;
            button.MouseEnter += (s, e) =>
            {
                if (button.Enabled)
                    button.BackColor = ControlPaint.Light(originalColor, 0.2f);
            };
            button.MouseLeave += (s, e) =>
            {
                if (button.Enabled)
                    button.BackColor = originalColor;
            };
        }

        private void CustomizeOtpTextBox(TextBox textBox)
        {
            textBox.Font = new Font("Consolas", 20F, FontStyle.Bold);
            textBox.TextAlign = HorizontalAlignment.Center;
            textBox.MaxLength = 1;
            textBox.BorderStyle = BorderStyle.FixedSingle;

            // Add focus border effect
            textBox.Enter += (s, e) =>
            {
                textBox.BackColor = Color.FromArgb(240, 248, 255);
            };

            textBox.Leave += (s, e) =>
            {
                textBox.BackColor = Color.White;
            };
        }

        private void SetupEventHandlers()
        {
            btnVerify.Click += async (_, _) => await BtnVerify_Click();
            btnResend.Click += async (_, _) => await BtnResend_Click();
            btnCancel.Click += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            // Setup OTP input auto-focus
            txtOtp1.TextChanged += (s, e) => { if (txtOtp1.Text.Length == 1) txtOtp2.Focus(); };
            txtOtp2.TextChanged += (s, e) => { if (txtOtp2.Text.Length == 1) txtOtp3.Focus(); };
            txtOtp3.TextChanged += (s, e) => { if (txtOtp3.Text.Length == 1) txtOtp4.Focus(); };
            txtOtp4.TextChanged += (s, e) => { if (txtOtp4.Text.Length == 1) txtOtp5.Focus(); };
            txtOtp5.TextChanged += (s, e) => { if (txtOtp5.Text.Length == 1) txtOtp6.Focus(); };
            txtOtp6.TextChanged += (s, e) =>
            {
                if (txtOtp6.Text.Length == 1)
                {
                    btnVerify.Focus();
                    // Auto-verify when all 6 digits entered
                    if (GetOtpCode().Length == 6)
                    {
                        btnVerify.PerformClick();
                    }
                }
            };

            // Setup backspace navigation
            txtOtp2.KeyDown += (s, e) => { if (e.KeyCode == Keys.Back && txtOtp2.Text.Length == 0) txtOtp1.Focus(); };
            txtOtp3.KeyDown += (s, e) => { if (e.KeyCode == Keys.Back && txtOtp3.Text.Length == 0) txtOtp2.Focus(); };
            txtOtp4.KeyDown += (s, e) => { if (e.KeyCode == Keys.Back && txtOtp4.Text.Length == 0) txtOtp3.Focus(); };
            txtOtp5.KeyDown += (s, e) => { if (e.KeyCode == Keys.Back && txtOtp5.Text.Length == 0) txtOtp4.Focus(); };
            txtOtp6.KeyDown += (s, e) => { if (e.KeyCode == Keys.Back && txtOtp6.Text.Length == 0) txtOtp5.Focus(); };

            // Only allow digits
            var otpBoxes = new[] { txtOtp1, txtOtp2, txtOtp3, txtOtp4, txtOtp5, txtOtp6 };
            foreach (var box in otpBoxes)
            {
                box.KeyPress += (s, e) =>
                {
                    if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
                    {
                        e.Handled = true;
                    }
                };

                // Paste support
                box.KeyDown += (s, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.V)
                    {
                        PasteOtp();
                        e.Handled = true;
                    }
                };
            }
        }

        private void StartCountdown()
        {
            _countdownTimer = new Timer { Interval = 1000 };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
            UpdateTimerDisplay();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _remainingSeconds--;

            if (_resendCooldown > 0)
            {
                _resendCooldown--;
                if (_resendCooldown == 0)
                {
                    btnResend.Enabled = true;
                    btnResend.Text = "📧 Gửi lại mã";
                }
                else
                {
                    btnResend.Text = $"Chờ {_resendCooldown}s";
                }
            }

            if (_remainingSeconds <= 0)
            {
                _countdownTimer.Stop();
                lblTimer.Text = "⏰ Hết hạn";
                lblTimer.ForeColor = Color.FromArgb(231, 76, 60);
                ShowStatus("❌ Mã OTP đã hết hạn. Vui lòng gửi lại mã mới.", MessageType.Error);
                btnVerify.Enabled = false;
                btnResend.Enabled = true;
                return;
            }

            UpdateTimerDisplay();
        }

        private void UpdateTimerDisplay()
        {
            int minutes = _remainingSeconds / 60;
            int seconds = _remainingSeconds % 60;
            lblTimer.Text = $"⏱️ Còn lại: {minutes:D2}:{seconds:D2}";

            // Change color based on remaining time
            if (_remainingSeconds > 300) // > 5 minutes
            {
                lblTimer.ForeColor = Color.FromArgb(39, 174, 96);
            }
            else if (_remainingSeconds > 120) // > 2 minutes
            {
                lblTimer.ForeColor = Color.FromArgb(243, 156, 18);
            }
            else
            {
                lblTimer.ForeColor = Color.FromArgb(231, 76, 60);
            }

            // Update progress bar
            progressTimer.Value = (_remainingSeconds * 100) / 600;
        }

        private string GetOtpCode()
        {
            return txtOtp1.Text + txtOtp2.Text + txtOtp3.Text +
                   txtOtp4.Text + txtOtp5.Text + txtOtp6.Text;
        }

        private void ClearOtp()
        {
            txtOtp1.Clear();
            txtOtp2.Clear();
            txtOtp3.Clear();
            txtOtp4.Clear();
            txtOtp5.Clear();
            txtOtp6.Clear();
            txtOtp1.Focus();
        }

        private void PasteOtp()
        {
            try
            {
                string clipboardText = Clipboard.GetText();
                if (clipboardText.Length >= 6 && clipboardText.All(char.IsDigit))
                {
                    txtOtp1.Text = clipboardText[0].ToString();
                    txtOtp2.Text = clipboardText[1].ToString();
                    txtOtp3.Text = clipboardText[2].ToString();
                    txtOtp4.Text = clipboardText[3].ToString();
                    txtOtp5.Text = clipboardText[4].ToString();
                    txtOtp6.Text = clipboardText[5].ToString();
                    txtOtp6.Focus();
                }
            }
            catch
            {
                // Ignore paste errors
            }
        }

        private async Task BtnVerify_Click()
        {
            var otp = GetOtpCode();

            if (otp.Length != 6)
            {
                ShowStatus("⚠️ Vui lòng nhập đầy đủ 6 chữ số", MessageType.Warning);
                txtOtp1.Focus();
                return;
            }

            btnVerify.Enabled = false;
            btnResend.Enabled = false;
            btnVerify.Text = "Đang xác thực...";
            ShowStatus("🔄 Đang xác thực mã OTP...", MessageType.Info);
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.VerifyOtpAsync(_username, otp);

                if (response == null || !response.Success)
                {
                    ShowStatus($"❌ {response?.Message ?? "Mã OTP không đúng"}", MessageType.Error);

                    // Shake animation for wrong OTP
                    ShakeForm();
                    ClearOtp();

                    btnVerify.Enabled = true;
                    btnVerify.Text = "✅ Xác thực";
                    progressBar.Visible = false;

                    if (_remainingSeconds > 0)
                    {
                        btnResend.Enabled = true;
                    }
                    return;
                }

                // Success!
                ShowStatus("✅ Xác thực thành công!", MessageType.Success);
                _countdownTimer?.Stop();
                await Task.Delay(1000);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Lỗi: {ex.Message}", MessageType.Error);
                btnVerify.Enabled = true;
                btnVerify.Text = "✅ Xác thực";
                progressBar.Visible = false;
                btnResend.Enabled = _remainingSeconds > 0;
            }
        }

        private async Task BtnResend_Click()
        {
            btnResend.Enabled = false;
            ShowStatus("🔄 Đang gửi lại mã OTP...", MessageType.Info);

            try
            {
                using var socketClient = new SocketClientService("127.0.0.1", 9000);
                await socketClient.ConnectAsync();

                var response = await socketClient.ResendOtpAsync(_username);

                if (response == null || !response.Success)
                {
                    ShowStatus($"❌ {response?.Message ?? "Lỗi gửi lại mã"}", MessageType.Error);
                    btnResend.Enabled = true;
                    return;
                }

                ShowStatus("✅ Đã gửi lại mã OTP. Vui lòng kiểm tra email!", MessageType.Success);

                // Reset timer
                _remainingSeconds = 600;
                _resendCooldown = 60; // 60 seconds cooldown
                UpdateTimerDisplay();

                btnVerify.Enabled = true;
                ClearOtp();
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Lỗi: {ex.Message}", MessageType.Error);
                btnResend.Enabled = true;
            }
        }

        private void ShakeForm()
        {
            var original = this.Location;
            var rnd = new Random();
            const int shake_amplitude = 10;

            for (int i = 0; i < 10; i++)
            {
                this.Location = new Point(
                    original.X + rnd.Next(-shake_amplitude, shake_amplitude),
                    original.Y + rnd.Next(-shake_amplitude, shake_amplitude));
                System.Threading.Thread.Sleep(20);
                Application.DoEvents();
            }

            this.Location = original;
        }

        private void ShowStatus(string message, MessageType type)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = type switch
            {
                MessageType.Success => Color.FromArgb(39, 174, 96),
                MessageType.Error => Color.FromArgb(231, 76, 60),
                MessageType.Warning => Color.FromArgb(243, 156, 18),
                MessageType.Info => Color.FromArgb(52, 152, 219),
                _ => Color.Black
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
        }

        private enum MessageType
        {
            Success,
            Error,
            Warning,
            Info
        }
    }
}
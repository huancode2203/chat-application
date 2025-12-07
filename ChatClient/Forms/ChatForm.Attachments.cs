using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChatClient.Models;
using ChatClient.Services;
using WinFormsLabel = System.Windows.Forms.Label;

namespace ChatClient.Forms
{
    public partial class ChatForm
    {
        private async Task SendAttachmentAsync()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Tất cả file (*.*)|*.*|Hình ảnh (*.jpg;*.png;*.gif;*.bmp)|*.jpg;*.png;*.gif;*.bmp|" +
                         "Tài liệu (*.pdf;*.doc;*.docx;*.xls;*.xlsx)|*.pdf;*.doc;*.docx;*.xls;*.xlsx|" +
                         "Video (*.mp4;*.avi;*.mkv)|*.mp4;*.avi;*.mkv",
                Title = "Chọn file đính kèm",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            var fileInfo = new FileInfo(openFileDialog.FileName);

            // Check file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (fileInfo.Length > maxFileSize)
            {
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                MessageBox.Show(
                    $"File quá lớn ({fileSizeMB:F2} MB)!\n\nKích thước tối đa cho phép là 10MB.",
                    "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_currentConversationId))
            {
                MessageBox.Show("Vui lòng chọn cuộc trò chuyện trước khi gửi file.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (btnAttachment != null)
                    btnAttachment.Enabled = false;
                if (btnSend != null)
                    btnSend.Enabled = false;

                // Show progress dialog
                var progressForm = new ProgressDialog($"Đang tải {fileInfo.Name} lên server...");
                progressForm.Show(this);
                Application.DoEvents();

                UpdateStatus($"Đang tải {fileInfo.Name} lên server...", false);

                var response = await _socketClient.UploadAttachmentAsync(
                    _currentUser, openFileDialog.FileName);

                progressForm.Close();

                if (response == null || !response.Success)
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tải file lên server.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Send message with attachment
                var label = (cbLabel?.SelectedIndex ?? 0) + 1;
                var fileIcon = GetFileIcon(fileInfo.Extension);
                var displayName = $"{fileIcon} {fileInfo.Name}";

                var msgResponse = await _socketClient.SendMessageWithAttachmentAsync(
                    _currentUser, _currentConversationId,
                    $"[File: {fileInfo.Name}]",
                    label, response.AttachmentId);

                if (msgResponse == null || !msgResponse.Success)
                {
                    MessageBox.Show(msgResponse?.Message ?? "Lỗi gửi tin nhắn kèm file.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Append tin nhắn đính kèm ngay, không reload toàn bộ
                var newMsg = new ChatMessageDto
                {
                    MessageId = msgResponse.MessageId,
                    ConversationId = _currentConversationId,
                    Sender = _currentUser.Matk,
                    Content = $"[File: {fileInfo.Name}]",
                    SecurityLabel = label,
                    Timestamp = DateTime.Now
                };

                if (lstMessages != null)
                {
                    lstMessages.BeginUpdate();
                    AddMessageToList(lstMessages, newMsg, _currentUser.Matk, _messageHeights);
                    lstMessages.EndUpdate();

                    if (lstMessages.Items.Count > 0)
                        lstMessages.Items[^1].EnsureVisible();
                }

                _currentMessages = [.. _currentMessages.Append(newMsg).OrderBy(m => m.Timestamp)];

                // Nếu là ảnh, preload để hiện inline ngay
                if (HasImageExtension(fileInfo.Name))
                {
                    _ = PreloadInlineImageAsync(newMsg.MessageId);
                }

                UpdateStatus($"Đã gửi {fileInfo.Name} thành công.", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi file: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btnAttachment != null)
                    btnAttachment.Enabled = true;
                if (btnSend != null)
                    btnSend.Enabled = true;
            }
        }

        private async Task DownloadAttachmentAsync()
        {
            if (lstMessages == null || lstMessages.SelectedItems.Count == 0) return;

            var item = lstMessages.SelectedItems[0];
            if (item.Tag == null) return;

            var messageId = Convert.ToInt32(item.Tag);
            var content = item.SubItems.Count > 2 ? item.SubItems[2].Text : string.Empty;

            if (!TryParseAttachmentFileName(content, out var fileName))
            {
                MessageBox.Show("Không tìm thấy tên file đính kèm.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Show save file dialog
                using var saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = "Tất cả file (*.*)|*.*",
                    Title = "Lưu file đính kèm"
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

                // Show progress
                var progressForm = new ProgressDialog($"Đang tải {fileName} xuống...");
                progressForm.Show(this);
                Application.DoEvents();

                UpdateStatus($"Đang tải {fileName}...", false);

                var response = await _socketClient.DownloadAttachmentAsync(_currentUser, messageId);

                progressForm.Close();

                if (response == null || !response.Success || string.IsNullOrEmpty(response.AttachmentContentBase64))
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tải file.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Save file
                var bytes = Convert.FromBase64String(response.AttachmentContentBase64);
                await File.WriteAllBytesAsync(saveFileDialog.FileName, bytes);

                UpdateStatus($"Đã lưu {fileName} thành công.", false);

                var result = MessageBox.Show(
                    $"Đã tải file thành công!\n\nBạn có muốn mở file ngay không?",
                    "Thành công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải file: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PreviewAttachmentAsync()
        {
            if (lstMessages == null || lstMessages.SelectedItems.Count == 0) return;

            var item = lstMessages.SelectedItems[0];
            if (item.Tag == null) return;

            var messageId = Convert.ToInt32(item.Tag);
            var content = item.SubItems.Count > 2 ? item.SubItems[2].Text : string.Empty;

            if (!TryParseAttachmentFileName(content, out var fileName))
            {
                MessageBox.Show("Không tìm thấy tên file đính kèm.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!HasImageExtension(fileName))
            {
                MessageBox.Show("Chỉ có thể xem trước các file ảnh (jpg, png, gif, bmp, webp).", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // Show loading dialog
                var progressForm = new ProgressDialog($"Đang tải {fileName}...");
                progressForm.Show(this);
                Application.DoEvents();

                UpdateStatus($"Đang tải {fileName}...", false);

                var response = await _socketClient.DownloadAttachmentAsync(_currentUser, messageId);

                progressForm.Close();

                if (response == null || !response.Success || string.IsNullOrEmpty(response.AttachmentContentBase64))
                {
                    MessageBox.Show(response?.Message ?? "Lỗi tải ảnh.", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Convert to image
                var bytes = Convert.FromBase64String(response.AttachmentContentBase64);
                using var ms = new MemoryStream(bytes);
                var image = System.Drawing.Image.FromStream(ms);

                // Show preview form
                ShowImagePreview(image, fileName);

                UpdateStatus("Đã tải ảnh thành công.", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xem trước ảnh: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowImagePreview(System.Drawing.Image image, string fileName)
        {
            var previewForm = new Form
            {
                Text = $"Xem trước - {fileName}",
                Size = new System.Drawing.Size(800, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                MinimizeBox = false,
                MaximizeBox = true,
                BackColor = System.Drawing.Color.Black
            };

            var pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = image,
                BackColor = System.Drawing.Color.Black
            };

            var statusLabel = new WinFormsLabel
            {
                Text = $"{fileName} - {image.Width} x {image.Height} pixels",
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9F)
            };

            var btnClose = new Button
            {
                Text = "Đóng (ESC)",
                Size = new System.Drawing.Size(100, 30),
                Location = new System.Drawing.Point(10, 10),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => previewForm.Close();

            var btnSave = new Button
            {
                Text = "💾 Lưu",
                Size = new System.Drawing.Size(100, 30),
                Location = new System.Drawing.Point(120, 10),
                BackColor = System.Drawing.Color.FromArgb(40, 167, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) =>
            {
                await DownloadAttachmentAsync();
            };

            pictureBox.Controls.Add(btnClose);
            pictureBox.Controls.Add(btnSave);
            btnClose.BringToFront();
            btnSave.BringToFront();

            previewForm.Controls.Add(pictureBox);
            previewForm.Controls.Add(statusLabel);

            // ESC to close
            previewForm.KeyPreview = true;
            previewForm.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    previewForm.Close();
            };

            // Clean up
            previewForm.FormClosed += (s, e) =>
            {
                image.Dispose();
            };

            previewForm.ShowDialog(this);
        }
    }
}
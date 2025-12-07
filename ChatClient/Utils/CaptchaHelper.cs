using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ChatClient.Utils
{
    public static class CaptchaHelper
    {
        private static readonly Random _random = new();
        private static string _currentCaptcha = string.Empty;

        public static string GenerateCaptcha()
        {
            // Tạo mã captcha ngẫu nhiên 4-5 ký tự
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            _currentCaptcha = new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
            return _currentCaptcha;
        }

        public static bool ValidateCaptcha(string input)
        {
            return !string.IsNullOrEmpty(_currentCaptcha) && 
                   _currentCaptcha.Equals(input, StringComparison.OrdinalIgnoreCase);
        }

        public static Bitmap GenerateCaptchaImage(string captchaText)
        {
            var bitmap = new Bitmap(120, 40);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Background màu trắng
            graphics.Clear(Color.White);
            
            // Vẽ text với font lớn và màu xanh
            var font = new Font("Arial", 18, FontStyle.Bold);
            var brush = new SolidBrush(Color.FromArgb(0, 102, 204)); // Màu xanh
            
            // Tính toán vị trí để căn giữa
            var textSize = graphics.MeasureString(captchaText, font);
            var x = (bitmap.Width - textSize.Width) / 2;
            var y = (bitmap.Height - textSize.Height) / 2;
            
            // Vẽ text
            graphics.DrawString(captchaText, font, brush, x, y);
            
            // Vẽ đường chéo để làm khó đọc
            var pen = new Pen(Color.FromArgb(200, 200, 200), 2);
            graphics.DrawLine(pen, 0, bitmap.Height / 2, bitmap.Width, bitmap.Height / 2);
            
            // Thêm noise
            for (int i = 0; i < 20; i++)
            {
                var noiseX = _random.Next(bitmap.Width);
                var noiseY = _random.Next(bitmap.Height);
                bitmap.SetPixel(noiseX, noiseY, Color.FromArgb(_random.Next(200, 256), 
                    _random.Next(200, 256), _random.Next(200, 256)));
            }
            
            return bitmap;
        }
    }
}


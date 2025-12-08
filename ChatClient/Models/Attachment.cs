using System;

namespace ChatClient.Models
{
    /// <summary>
    /// Model tệp đính kèm trên client.
    /// Đồng bộ với bảng ATTACHMENT trong database.
    /// </summary>
    public class Attachment
    {
        // ========== THÔNG TIN CƠ BẢN ==========
        public int AttachId { get; set; }                           // ATTACH_ID
        public string Matk { get; set; } = string.Empty;            // MATK - Người upload
        public string Filename { get; set; } = string.Empty;        // FILENAME
        public string Mimetype { get; set; } = string.Empty;        // MIMETYPE
        public long Filesize { get; set; }                          // FILESIZE
        public byte[]? Filedata { get; set; }                       // FILEDATA
        public string? StorageUrl { get; set; }                     // STORAGE_URL
        public DateTime UploadedAt { get; set; }                    // UPLOADED_AT
        
        // ========== MÃ HÓA ==========
        public bool IsEncrypted { get; set; }                       // IS_ENCRYPTED
        public byte[]? EncryptedContent { get; set; }               // ENCRYPTED_CONTENT
        public byte[]? EncryptionKey { get; set; }                  // ENCRYPTION_KEY (AES key)
        public byte[]? EncryptionIv { get; set; }                   // ENCRYPTION_IV (AES IV)
        public string EncryptionType { get; set; } = "NONE";        // ENCRYPTION_TYPE
        
        // ========== HELPER ==========
        public string FilesizeFormatted
        {
            get
            {
                if (Filesize < 1024) return $"{Filesize} B";
                if (Filesize < 1024 * 1024) return $"{Filesize / 1024.0:F1} KB";
                if (Filesize < 1024 * 1024 * 1024) return $"{Filesize / (1024.0 * 1024):F1} MB";
                return $"{Filesize / (1024.0 * 1024 * 1024):F1} GB";
            }
        }
        
        public bool IsImage => Mimetype?.StartsWith("image/") ?? false;
        public bool IsVideo => Mimetype?.StartsWith("video/") ?? false;
        public bool IsAudio => Mimetype?.StartsWith("audio/") ?? false;
        public bool IsDocument => !IsImage && !IsVideo && !IsAudio;
        
        public string FileExtension
        {
            get
            {
                var idx = Filename?.LastIndexOf('.') ?? -1;
                return idx >= 0 ? Filename!.Substring(idx) : string.Empty;
            }
        }
    }
}

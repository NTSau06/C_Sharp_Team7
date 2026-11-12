using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SportShopAdmin.Utils
{
    /// <summary>
    /// Quản lý ảnh sản phẩm. Cột HinhAnh chỉ lưu tên file; file ảnh được chép vào thư mục cấu hình
    /// "ImageFolder" trong App.config (mặc định: thư mục Images cạnh file .exe).
    /// Có thể trỏ ImageFolder tới wwwroot/images của project Web để hai ứng dụng dùng chung ảnh.
    /// </summary>
    public static class ImageHelper
    {
        public static string Folder
        {
            get
            {
                string cfg = ConfigurationManager.AppSettings["ImageFolder"];
                string folder = string.IsNullOrWhiteSpace(cfg) ? "Images" : cfg.Trim();
                if (!Path.IsPathRooted(folder)) folder = Path.Combine(Application.StartupPath, folder);
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        /// <summary>Chép ảnh vào thư mục ảnh, trả về tên file mới</summary>
        public static string Import(string sourcePath)
        {
            string name = "sp_" + Guid.NewGuid().ToString("N").Substring(0, 12) + Path.GetExtension(sourcePath).ToLowerInvariant();
            File.Copy(sourcePath, Path.Combine(Folder, name));
            return name;
        }

        /// <summary>Đọc ảnh (không khóa file). Trả về null nếu không có.</summary>
        public static Image Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            try
            {
                string path = Path.Combine(Folder, fileName);
                if (!File.Exists(path)) return null;
                byte[] bytes = File.ReadAllBytes(path);
                using (var ms = new MemoryStream(bytes))
                using (Image img = Image.FromStream(ms))
                {
                    return new Bitmap(img);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}

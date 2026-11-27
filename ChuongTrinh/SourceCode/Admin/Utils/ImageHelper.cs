using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SportZoneAdmin.Utils
{
    /// <summary>
    /// Quản lý ảnh sản phẩm. Cột HinhAnh chỉ lưu tên file; file ảnh được chép vào thư mục cấu hình
    /// "ImageFolder" trong App.config, đang trỏ tới ChuongTrinh\SourceCode\Clients\wwwroot\images
    /// để Admin và Web dùng chung ảnh (Web: WebHelper.AnhSanPham => "/images/" + HinhAnh).
    /// </summary>
    public static class ImageHelper
    {
        public static string Folder
        {
            get
            {
                string cfg = ConfigurationManager.AppSettings["ImageFolder"];
                string folder = string.IsNullOrWhiteSpace(cfg) ? "Images" : cfg.Trim();
                if (!Path.IsPathRooted(folder))
                    folder = Path.GetFullPath(Path.Combine(Application.StartupPath, folder));

                // Nếu chạy từ thư mục khác chuẩn bin\Debug, tự dò Clients\wwwroot\images ở các thư mục cha
                if (!Directory.Exists(folder))
                {
                    string dir = Application.StartupPath;
                    for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                    {
                        string probe = Path.Combine(dir, "Clients", "wwwroot", "images");
                        if (Directory.Exists(probe))
                        {
                            folder = probe;
                            break;
                        }
                        DirectoryInfo parent = Directory.GetParent(dir.TrimEnd('\\', '/'));
                        dir = parent == null ? null : parent.FullName;
                    }
                }

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

        /// <summary>
        /// Đọc ảnh (không khóa file). Trả về null nếu không có.
        /// Nếu CSDL cũ còn lưu tên ảnh mẫu .svg/.png đã xóa thì tự chuyển sang bản .jpg cùng tên.
        /// </summary>
        public static Image Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            try
            {
                string path = Path.Combine(Folder, fileName.Trim());
                string ext = Path.GetExtension(path);
                if (string.Equals(ext, ".svg", StringComparison.OrdinalIgnoreCase)
                    || (!File.Exists(path) && string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase)))
                    path = Path.ChangeExtension(path, ".jpg");
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

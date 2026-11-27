using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SportZoneAdmin.DAL
{
    /// <summary>
    /// Tự động chuẩn bị CSDL khi chạy chương trình từ Visual Studio:
    ///  1. Dò máy chủ SQL Server có trên máy ( . / .\SQLEXPRESS / (localdb)\MSSQLLocalDB ...).
    ///  2. Nếu chưa có CSDL SportZoneDB (hoặc thiếu bảng) thì chạy script Database\SportZoneDB.sql
    ///     (được nhúng sẵn vào file .exe) để tạo 11 bảng + dữ liệu mẫu.
    /// Nhờ vậy không cần mở SSMS chạy script bằng tay.
    /// </summary>
    public static class DatabaseInitializer
    {
        public const string DatabaseName = "SportZoneDB";
        private const string ScriptResourceName = "SportZoneAdmin.SportZoneDB.sql";

        /// <summary>Các tên máy chủ thường gặp, thử lần lượt.</summary>
        public static readonly string[] CandidateServers =
        {
            ".",
            @".\SQLEXPRESS",
            @".\SQLEXPRESS01",
            @".\SQLEXPRESS02",
            @"(localdb)\MSSQLLocalDB",
            "localhost",
            @"localhost\SQLEXPRESS"
        };

        // ------------------------------------------------------------------ dò máy chủ
        private static string MasterConnectionString(string server)
        {
            bool isLocalDb = server.IndexOf("localdb", StringComparison.OrdinalIgnoreCase) >= 0;
            var b = new SqlConnectionStringBuilder(DBConnect.ConnectionString)
            {
                DataSource = server,
                InitialCatalog = "master",
                Pooling = false,
                // LocalDB khởi động lần đầu có thể mất 10-20 giây
                ConnectTimeout = isLocalDb ? 40 : 5
            };
            return b.ConnectionString;
        }

        /// <summary>Kiểm tra máy chủ SQL có kết nối được không (vào master).</summary>
        public static bool CanConnectServer(string server, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(server)) { error = "Chưa nhập tên máy chủ."; return false; }
            try
            {
                using (var conn = new SqlConnection(MasterConnectionString(server)))
                {
                    conn.Open();
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Thử lần lượt các máy chủ; trả về tên máy chủ đầu tiên kết nối được, hoặc null.</summary>
        public static string FindServer(string preferred)
        {
            var tried = new List<string>();
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(preferred)) list.Add(preferred.Trim());
            list.AddRange(CandidateServers);

            foreach (string s in list)
            {
                if (tried.Exists(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase))) continue;
                tried.Add(s);
                string err;
                if (CanConnectServer(s, out err)) return s;
            }
            return null;
        }

        // ------------------------------------------------------------------ kiểm tra / tạo CSDL
        /// <summary>CSDL SportZoneDB đã tồn tại và có đủ bảng chưa?</summary>
        public static bool DatabaseReady(string server)
        {
            using (var conn = new SqlConnection(MasterConnectionString(server)))
            {
                conn.Open();
                const string sql =
                    "SELECT CASE WHEN DB_ID(@db) IS NULL THEN 0 " +
                    "WHEN OBJECT_ID(QUOTENAME(@db) + '.dbo.ChiTietPhieuNhap') IS NULL THEN 0 " +
                    "WHEN OBJECT_ID(QUOTENAME(@db) + '.dbo.QuanTriVien') IS NULL THEN 0 ELSE 1 END";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@db", DatabaseName);
                    return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            }
        }

        /// <summary>Tạo CSDL nếu chưa có. Trả về true nếu vừa tạo mới.</summary>
        public static bool EnsureDatabase(string server)
        {
            if (DatabaseReady(server)) return false;
            CreateDatabase(server);
            return true;
        }

        /// <summary>Xóa (nếu có) và tạo lại toàn bộ CSDL + dữ liệu mẫu từ script.</summary>
        public static void CreateDatabase(string server)
        {
            string script = LoadScript();

            // Đóng mọi kết nối đang giữ trong pool để DROP DATABASE không bị chặn
            SqlConnection.ClearAllPools();

            using (var conn = new SqlConnection(MasterConnectionString(server)))
            {
                conn.Open();
                foreach (string batch in SplitBatches(script))
                {
                    using (var cmd = new SqlCommand(batch, conn))
                    {
                        cmd.CommandTimeout = 180;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            SqlConnection.ClearAllPools();
        }

        // ------------------------------------------------------------------ đọc script
        /// <summary>
        /// Đọc script SportZoneDB.sql: ưu tiên bản nhúng trong .exe,
        /// nếu không có thì tìm file Database\SportZoneDB.sql cạnh .exe hoặc ở thư mục cha.
        /// </summary>
        public static string LoadScript()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(ScriptResourceName))
            {
                if (s != null)
                    using (var r = new StreamReader(s, Encoding.UTF8, true))
                        return r.ReadToEnd();
            }

            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string path = Path.Combine(dir, "Database", "SportZoneDB.sql");
                if (File.Exists(path)) return File.ReadAllText(path, Encoding.UTF8);
                DirectoryInfo parent = Directory.GetParent(dir.TrimEnd('\\', '/'));
                dir = parent == null ? null : parent.FullName;
            }
            throw new FileNotFoundException("Không tìm thấy script Database\\SportZoneDB.sql để tạo CSDL.");
        }

        /// <summary>Tách script theo dòng "GO" (giống SSMS).</summary>
        public static List<string> SplitBatches(string script)
        {
            var result = new List<string>();
            string[] parts = Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (string p in parts)
            {
                string batch = p.Trim().TrimStart('﻿');
                if (batch.Length > 0) result.Add(batch);
            }
            return result;
        }
    }
}

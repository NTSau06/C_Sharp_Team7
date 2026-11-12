using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace SportShopWeb.DAL
{
    /// <summary>
    /// Chuẩn bị CSDL khi Web khởi động (giống phân hệ Admin):
    ///  1. Dò máy chủ SQL Server có trên máy ( . / .\SQLEXPRESS / (localdb)\MSSQLLocalDB ...).
    ///  2. Nếu chưa có CSDL SportShopDB thì chạy script Database\SportShopDB.sql (nhúng trong project)
    ///     để tạo 11 bảng + dữ liệu mẫu.
    /// </summary>
    public static class DatabaseInitializer
    {
        public const string DatabaseName = "SportShopDB";
        private const string ScriptResourceName = "SportShopWeb.SportShopDB.sql";

        public static readonly string[] CandidateServers =
        {
            ".",
            @".\SQLEXPRESS",
            @"(localdb)\MSSQLLocalDB",
            "localhost",
            @"localhost\SQLEXPRESS"
        };

        private static string MasterConnectionString(string baseConnectionString, string server)
        {
            bool isLocalDb = server.IndexOf("localdb", StringComparison.OrdinalIgnoreCase) >= 0;
            var b = new SqlConnectionStringBuilder(baseConnectionString)
            {
                DataSource = server,
                InitialCatalog = "master",
                Pooling = false,
                ConnectTimeout = isLocalDb ? 40 : 5   // LocalDB khởi động lần đầu có thể mất 10-20 giây
            };
            return b.ConnectionString;
        }

        public static bool CanConnectServer(string baseConnectionString, string server, out string error)
        {
            error = null;
            try
            {
                using (var conn = new SqlConnection(MasterConnectionString(baseConnectionString, server)))
                    conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Trả về chuỗi kết nối dùng được (đã thay Data Source nếu phải tự dò) và tạo CSDL nếu cần.
        /// Ném InvalidOperationException kèm hướng dẫn nếu không tìm thấy SQL Server.
        /// </summary>
        public static string Prepare(string connectionString, bool autoDetect, bool autoCreate, Action<string> log)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string configured = builder.DataSource;
            string server = null;
            string firstError;

            if (CanConnectServer(connectionString, configured, out firstError))
                server = configured;
            else if (autoDetect)
            {
                log("Không kết nối được \"" + configured + "\" (" + firstError + "). Đang tự dò máy chủ SQL Server...");
                foreach (string s in CandidateServers)
                {
                    if (string.Equals(s, configured, StringComparison.OrdinalIgnoreCase)) continue;
                    string err;
                    if (CanConnectServer(connectionString, s, out err)) { server = s; break; }
                }
            }

            if (server == null)
                throw new InvalidOperationException(
                    "Không tìm thấy SQL Server. Cài \"SQL Server Express LocalDB\" bằng Visual Studio Installer " +
                    "(Modify > Individual components) hoặc sửa Data Source trong appsettings.json. Chi tiết: " + firstError);

            log("Dùng máy chủ SQL: " + server);
            builder.DataSource = server;
            string cs = builder.ConnectionString;

            if (autoCreate && !DatabaseReady(cs, server))
            {
                log("Chưa có CSDL SportShopDB - đang tạo từ script Database/SportShopDB.sql ...");
                CreateDatabase(cs, server);
                log("Đã tạo CSDL SportShopDB (11 bảng + dữ liệu mẫu).");
            }
            return cs;
        }

        public static bool DatabaseReady(string baseConnectionString, string server)
        {
            using (var conn = new SqlConnection(MasterConnectionString(baseConnectionString, server)))
            {
                conn.Open();
                const string sql =
                    "SELECT CASE WHEN DB_ID(@db) IS NULL THEN 0 " +
                    "WHEN OBJECT_ID(QUOTENAME(@db) + '.dbo.ChiTietPhieuNhap') IS NULL THEN 0 " +
                    "WHEN OBJECT_ID(QUOTENAME(@db) + '.dbo.GioHang') IS NULL THEN 0 ELSE 1 END";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@db", DatabaseName);
                    return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
                }
            }
        }

        public static void CreateDatabase(string baseConnectionString, string server)
        {
            string script = LoadScript();
            SqlConnection.ClearAllPools();
            using (var conn = new SqlConnection(MasterConnectionString(baseConnectionString, server)))
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

        private static string LoadScript()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(ScriptResourceName))
            {
                if (s != null)
                    using (var r = new StreamReader(s, Encoding.UTF8, true))
                        return r.ReadToEnd();
            }
            throw new FileNotFoundException("Không tìm thấy script Database/SportShopDB.sql trong project.");
        }

        private static List<string> SplitBatches(string script)
        {
            var result = new List<string>();
            foreach (string p in Regex.Split(script, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                string batch = p.Trim().TrimStart('﻿');
                if (batch.Length > 0) result.Add(batch);
            }
            return result;
        }
    }
}

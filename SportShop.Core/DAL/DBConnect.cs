using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SportShopAdmin.DAL
{
    /// <summary>
    /// Lớp dùng chung để mở SqlConnection và thực thi câu lệnh có tham số (chống SQL Injection).
    /// Các phương thức nhận SqlTransaction t dùng kết nối t.Connection để tham gia giao dịch.
    /// </summary>
    public static class DBConnect
    {
        private const string ConnectionName = "SportShopDB";
        private static string _overrideConnectionString;

        /// <summary>
        /// Gọi MỘT LẦN lúc khởi động ứng dụng Web (Program.cs) để cung cấp connection string
        /// đọc từ appsettings.json, vì ASP.NET Core không dùng App.config/ConfigurationManager.
        /// Ứng dụng Desktop (WinForms) KHÔNG gọi hàm này - vẫn đọc từ App.config như trước giờ.
        /// </summary>
        public static void UseConnectionString(string connectionString)
        {
            _overrideConnectionString = connectionString;
        }

        public static string ConnectionString
        {
            get
            {
                if (_overrideConnectionString != null) return _overrideConnectionString;
                ConnectionStringSettings cs = ConfigurationManager.ConnectionStrings[ConnectionName];
                if (cs == null)
                    throw new ConfigurationErrorsException("Thiếu chuỗi kết nối 'SportShopDB' trong App.config.");
                return cs.ConnectionString;
            }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>Tên máy chủ SQL đang cấu hình (Data Source)</summary>
        public static string GetServerName()
        {
            return new SqlConnectionStringBuilder(ConnectionString).DataSource;
        }

        /// <summary>Lưu lại tên máy chủ SQL vào file cấu hình của chương trình (chỉ áp dụng cho app .exe có App.config)</summary>
        public static void SaveServerName(string server)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringSettings setting = config.ConnectionStrings.ConnectionStrings[ConnectionName];
            if (setting == null) return;
            var builder = new SqlConnectionStringBuilder(setting.ConnectionString) { DataSource = server };
            setting.ConnectionString = builder.ConnectionString;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("connectionStrings");
        }

        public static void TestConnection()
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
            }
        }

        // ------------------------------------------------------------------ tham số
        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static SqlParameter PInt(string name, int? value)
        {
            return new SqlParameter(name, SqlDbType.Int) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        public static SqlParameter PDate(string name, DateTime? value)
        {
            return new SqlParameter(name, SqlDbType.DateTime) { Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        public static SqlParameter PText(string name, string value)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, 500) { Value = value ?? "" };
        }

        // ------------------------------------------------------------------ thực thi
        private static SqlCommand CreateCommand(SqlConnection conn, SqlTransaction t, string sql, SqlParameter[] ps)
        {
            var cmd = new SqlCommand(sql, conn, t);
            if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);
            return cmd;
        }

        public static DataTable ExecuteQuery(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = CreateCommand(conn, null, sql, ps))
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable ExecuteQuery(SqlTransaction t, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = CreateCommand(t.Connection, t, sql, ps))
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = CreateCommand(conn, null, sql, ps))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static int ExecuteNonQuery(SqlTransaction t, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = CreateCommand(t.Connection, t, sql, ps))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = CreateCommand(conn, null, sql, ps))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        public static object ExecuteScalar(SqlTransaction t, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = CreateCommand(t.Connection, t, sql, ps))
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>Chạy một khối công việc trong SqlTransaction: lỗi bất kỳ => Rollback.</summary>
        public static void RunInTransaction(Action<SqlTransaction> work)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        work(t);
                        t.Commit();
                    }
                    catch
                    {
                        try { t.Rollback(); } catch { /* bỏ qua lỗi rollback */ }
                        throw;
                    }
                }
            }
        }

        public static T RunInTransaction<T>(Func<SqlTransaction, T> work)
        {
            T result = default(T);
            RunInTransaction(t => { result = work(t); });
            return result;
        }
    }

    /// <summary>Hàm tiện ích đọc giá trị từ DataRow an toàn với DBNull</summary>
    public static class DataRowExt
    {
        public static int Int(this DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
        }

        public static int? IntN(this DataRow r, string col)
        {
            return r[col] == DBNull.Value ? (int?)null : Convert.ToInt32(r[col]);
        }

        public static decimal Dec(this DataRow r, string col)
        {
            return r[col] == DBNull.Value ? 0m : Convert.ToDecimal(r[col]);
        }

        public static string Str(this DataRow r, string col)
        {
            return r[col] == DBNull.Value ? "" : Convert.ToString(r[col]);
        }

        public static DateTime Date(this DataRow r, string col)
        {
            return r[col] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[col]);
        }

        public static bool Bool(this DataRow r, string col)
        {
            return r[col] != DBNull.Value && Convert.ToBoolean(r[col]);
        }
    }
}

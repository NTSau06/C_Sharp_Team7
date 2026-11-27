using System.Data;
using Microsoft.Data.SqlClient;

namespace SportZoneWeb.DAL
{
    /// <summary>
    /// Lớp dùng chung để mở SqlConnection và thực thi câu lệnh có tham số (SqlParameter - chống SQL Injection).
    /// Các phương thức nhận SqlTransaction t dùng kết nối t.Connection để tham gia giao dịch.
    /// Chuỗi kết nối được gán một lần khi ứng dụng khởi động (Program.cs).
    /// </summary>
    public static class DBConnect
    {
        public static string ConnectionString { get; set; }

        public static SqlConnection GetConnection()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối 'SportZoneDB' trong appsettings.json.");
            return new SqlConnection(ConnectionString);
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

        public static SqlParameter PDec(string name, decimal? value)
        {
            return new SqlParameter(name, SqlDbType.Decimal) { Precision = 18, Scale = 0, Value = value.HasValue ? (object)value.Value : DBNull.Value };
        }

        public static SqlParameter PText(string name, string value, int size = 500)
        {
            return new SqlParameter(name, SqlDbType.NVarChar, size) { Value = value ?? "" };
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
                return ExecuteQuery(conn, null, sql, ps);
            }
        }

        public static DataTable ExecuteQuery(SqlTransaction t, string sql, params SqlParameter[] ps)
        {
            return ExecuteQuery(t.Connection, t, sql, ps);
        }

        private static DataTable ExecuteQuery(SqlConnection conn, SqlTransaction t, string sql, SqlParameter[] ps)
        {
            using (SqlCommand cmd = CreateCommand(conn, t, sql, ps))
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
                    return cmd.ExecuteNonQuery();
            }
        }

        public static int ExecuteNonQuery(SqlTransaction t, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = CreateCommand(t.Connection, t, sql, ps))
                return cmd.ExecuteNonQuery();
        }

        public static object ExecuteScalar(string sql, params SqlParameter[] ps)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlCommand cmd = CreateCommand(conn, null, sql, ps))
                    return cmd.ExecuteScalar();
            }
        }

        public static object ExecuteScalar(SqlTransaction t, string sql, params SqlParameter[] ps)
        {
            using (SqlCommand cmd = CreateCommand(t.Connection, t, sql, ps))
                return cmd.ExecuteScalar();
        }

        /// <summary>Chạy một khối công việc trong SqlTransaction: lỗi bất kỳ => Rollback.</summary>
        public static T RunInTransaction<T>(Func<SqlTransaction, T> work)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                using (SqlTransaction t = conn.BeginTransaction())
                {
                    try
                    {
                        T result = work(t);
                        t.Commit();
                        return result;
                    }
                    catch
                    {
                        try { t.Rollback(); } catch { /* bỏ qua lỗi rollback */ }
                        throw;
                    }
                }
            }
        }
    }

    /// <summary>Hàm tiện ích đọc giá trị từ DataRow an toàn với DBNull</summary>
    public static class DataRowExt
    {
        public static int Int(this DataRow r, string col) => r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);
        public static int? IntN(this DataRow r, string col) => r[col] == DBNull.Value ? (int?)null : Convert.ToInt32(r[col]);
        public static decimal Dec(this DataRow r, string col) => r[col] == DBNull.Value ? 0m : Convert.ToDecimal(r[col]);
        public static string Str(this DataRow r, string col) => r[col] == DBNull.Value ? "" : Convert.ToString(r[col]);
        public static DateTime Date(this DataRow r, string col) => r[col] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r[col]);
        public static bool Bool(this DataRow r, string col) => r[col] != DBNull.Value && Convert.ToBoolean(r[col]);
    }
}

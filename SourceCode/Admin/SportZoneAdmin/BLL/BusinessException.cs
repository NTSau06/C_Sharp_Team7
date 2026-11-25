using System;

namespace SportZoneAdmin.BLL
{
    /// <summary>Lỗi nghiệp vụ / dữ liệu không hợp lệ - thông báo thân thiện cho người dùng</summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }

        /// <param name="field">Tên thuộc tính DTO bị lỗi (vd "GiaBan") để giao diện đánh dấu đúng ô nhập (ErrorProvider / ModelState).</param>
        public BusinessException(string message, string field) : base(message)
        {
            Field = field;
        }

        /// <summary>Tên thuộc tính bị lỗi; null nếu lỗi không gắn với một ô nhập cụ thể.</summary>
        public string Field { get; private set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SportZoneAdmin.DAL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.BLL
{
    /// <summary>Lỗi nghiệp vụ / dữ liệu không hợp lệ - thông báo thân thiện cho người dùng</summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }
}

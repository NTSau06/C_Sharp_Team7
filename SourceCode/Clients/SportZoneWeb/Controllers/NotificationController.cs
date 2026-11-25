using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportZoneWeb.BLL;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    /// <summary>Thông báo cho khách (ví dụ: đơn hàng đổi trạng thái)</summary>
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ThongBaoBLL _bll = new ThongBaoBLL();

        public IActionResult Index() => View(_bll.LayDanhSach(User.MaKH().Value));

        /// <summary>Mở thông báo: đánh dấu đã đọc rồi chuyển tới đơn hàng liên quan</summary>
        public IActionResult Open(int id, int? maDH)
        {
            _bll.DocMot(User.MaKH().Value, id);
            return maDH.HasValue ? RedirectToAction("OrderDetail", "Account", new { id = maDH.Value }) : RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReadAll()
        {
            _bll.DocTatCa(User.MaKH().Value);
            return RedirectToAction(nameof(Index));
        }
    }
}

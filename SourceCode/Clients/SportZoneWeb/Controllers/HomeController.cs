using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SportZoneWeb.BLL;
using SportZoneWeb.Models;
using SportZoneWeb.Utils;

namespace SportZoneWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly DanhMucBLL _danhMucBLL = new DanhMucBLL();
        private readonly SanPhamBLL _sanPhamBLL = new SanPhamBLL();
        private readonly MaGiamGiaBLL _maGiamGiaBLL = new MaGiamGiaBLL();
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            var vm = new HomeVM();
            try
            {
                vm.DanhMucs = _danhMucBLL.GetAll();
                vm.MoiNhat = _sanPhamBLL.MoiNhat(8);
                vm.BanChay = _sanPhamBLL.BanChay(4);
                vm.KhuyenMai = _maGiamGiaBLL.DangHieuLuc();
                vm.DaXem = _sanPhamBLL.TheoDanhSachMa(HttpContext.Session.DaXem().Take(4));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tải trang chủ");
                vm.ThongBao = "Không thể tải dữ liệu cửa hàng. Vui lòng thử lại sau.";
            }
            return View(vm);
        }

        public IActionResult About() => View();

        [Route("/Home/Error")]
        public IActionResult Error()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            Exception ex = feature?.Error;
            var vm = new ErrorVM { TieuDe = "Đã xảy ra lỗi", ThongBao = "Hệ thống đang gặp sự cố. Vui lòng thử lại sau." };
            if (ex is SqlException || ex?.InnerException is SqlException)
            {
                vm.TieuDe = "Không kết nối được cơ sở dữ liệu";
                vm.ThongBao = "Không thể kết nối CSDL SportZoneDB. Kiểm tra SQL Server đã chạy và chuỗi kết nối trong appsettings.json." +
                              (_env.IsDevelopment() ? " Chi tiết: " + ex.Message : "");
            }
            if (ex != null) _logger.LogError(ex, "Lỗi không mong muốn tại {Path}", feature?.Path);
            Response.StatusCode = 500;
            return View("Error", vm);
        }

        [Route("/Home/StatusCode/{code:int}")]
        public IActionResult MaLoi(int code)
        {
            var vm = code == 404
                ? new ErrorVM { TieuDe = "Không tìm thấy trang", ThongBao = "Trang bạn tìm không tồn tại hoặc đã bị xóa." }
                : new ErrorVM { TieuDe = "Lỗi " + code, ThongBao = "Yêu cầu không thể xử lý." };
            return View("Error", vm);
        }
    }
}

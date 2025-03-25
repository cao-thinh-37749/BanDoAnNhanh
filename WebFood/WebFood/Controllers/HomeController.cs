using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood.Data;
using WebFood.Models;
using System.Linq;
using System.Diagnostics;

namespace WebFood.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly ConnectStr _context;
		private readonly GetCount _getCount;
		public HomeController(ILogger<HomeController> logger, ConnectStr context)
		{
			_logger = logger;
			_context = context;
			_getCount = new GetCount(context);
		}
        #region Index
        public IActionResult Index(int? categoryId, decimal? minPrice = null, decimal? maxPrice = null, int page = 1, int pageSize = 8)
		{
			// Lấy danh sách sản phẩm đã lọc
			var products = GetFilteredProducts(categoryId, minPrice, maxPrice);

			// Lưu các tham số vào ViewBag để sử dụng trong View
			ViewBag.TotalProducts = products.Count();
			ViewBag.CurrentCategoryId = categoryId;
			ViewBag.CurrentMinPrice = minPrice;
			ViewBag.CurrentMaxPrice = maxPrice;
			ViewBag.CurrentPage = page;

			// Lưu email người dùng vào ViewBag
			var userEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
            ViewBag.UserEmail = userEmail;
			if(userEmail != null)
			{
				// Lấy số lượng sản phẩm trong giỏ hàng và lưu vào ViewBag
				ViewBag.SoluongHang = _getCount.GetCartCount(userEmail); 
				ViewBag.SoLuongYeuThich = _getCount.GetFavoriteProductCount(userEmail);
			}

			// Phân trang sản phẩm
			var pagedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return View(pagedProducts);
		}
        #endregion

        #region Details
        public IActionResult Details(int MaSanPham)
		{
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var product = _context.SanPham.FirstOrDefault(p => p.MaSanPham == MaSanPham);
			if (product == null)
			{
				return NotFound();
			}

			// Lưu email người dùng vào ViewBag
			ViewBag.UserEmail = userEmail;
            ViewBag.UserImage = HttpContext.Session.GetString("UserImage");
            ViewBag.SoluongHang = _getCount.GetCartCount(userEmail!);
            ViewBag.SoLuongYeuThich = _getCount.GetFavoriteProductCount(userEmail!);

            return View(product);
		}
        #endregion
        
        public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}

        #region Lọc
        // Phương thức lấy danh sách sản phẩm đã lọc
        private IQueryable<SanPham> GetFilteredProducts(int? categoryId, decimal? minPrice, decimal? maxPrice)
		{
			var products = _context.SanPham.AsQueryable();

			if (categoryId.HasValue)
			{
				products = products.Where(p => p.DanhMuc == categoryId.Value);
			}

			if (minPrice.HasValue && maxPrice.HasValue)
			{
				products = products.Where(p => p.Gia >= minPrice.Value && p.Gia <= maxPrice.Value);
			}

			return products;
		}

        #endregion

    }
}

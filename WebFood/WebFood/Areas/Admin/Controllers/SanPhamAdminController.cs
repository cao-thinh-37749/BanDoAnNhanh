using Microsoft.AspNetCore.Mvc;
using WebFood.Data;
using System.Linq;
using WebFood.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebFood.Areas.Admin.fliter;
using WebFood.Service;
using System.Net.Http;

namespace WebFood.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminSession] // kiểm tra session
    public class SanPhamAdminController : BaseController
    {
        private readonly SanPhamService _sanPhamService; 
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SanPhamAdminController(SanPhamService sanPhamService, IWebHostEnvironment webHostEnvironment)
        {
            _sanPhamService = sanPhamService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string searchTerm , int? CategoryId , bool showOutOfStock, string sort)
        {
            // Lưu thông tin tìm kiếm vào ViewData
            ViewData["searchTerm"] = searchTerm;
            ViewData["categoryId"] = CategoryId;
            ViewData["ShowOutOfStock"] = showOutOfStock;
            ViewData["SortList"] = sort;
             var products = await _sanPhamService.GetSanPhamsAsync(searchTerm, CategoryId, showOutOfStock, sort);

            ViewBag.DanhMucList = await _sanPhamService.GetDanhMucsAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _sanPhamService.GetSanPhamByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.DanhMucList =  await _sanPhamService.GetDanhMucsAsync();
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> Create(SanPham sanPham, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DanhMucList = await _sanPhamService.GetDanhMucsAsync();
                return View(sanPham);
            }

            string imageUrl = null;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", "Định dạng ảnh không hợp lệ!");
                    ViewBag.DanhMucList = await _sanPhamService.GetDanhMucsAsync();
                    return View(sanPham);
                }

                // Lưu hình ảnh vào thư mục và lấy đường dẫn ảnh
                imageUrl = await SaveUserImage(ImageFile);
            }

            // Gửi thông tin sản phẩm và đường dẫn ảnh vào service để lưu vào cơ sở dữ liệu
            bool result = await _sanPhamService.CreateSanPhamAsync(sanPham, imageUrl);

            if (!result)
            {
                ModelState.AddModelError("", "Lỗi khi tạo sản phẩm!");
                ViewBag.DanhMucList = await _sanPhamService.GetDanhMucsAsync();
                return View(sanPham);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveUserImage(IFormFile imageFile)
        {
            try
            {
                // Đường dẫn đến thư mục lưu trữ hình ảnh
                // Kiểm tra và tạo thư mục nếu chưa tồn tại
                var directoryPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "newfood");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath); // Tạo thư mục nếu chưa tồn tại
                }

                // Tạo tên file duy nhất để tránh trùng lặp
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var imagePath = Path.Combine(directoryPath, uniqueFileName);

                // Lưu hình ảnh vào thư mục 'ImagesUser'
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Trả về đường dẫn hình ảnh lưu trữ
                return "/images/newfood/" + uniqueFileName;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }





        public async Task<IActionResult> Edit(int MaSanPham)
        {
            var product = await _sanPhamService.GetSanPhamByIdAsync(MaSanPham);
            if (product == null) return NotFound();
            ViewBag.DanhMucList = await _sanPhamService.GetDanhMucsAsync();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int MaSanPham, SanPham sanPham)
        {
            if (!ModelState.IsValid) return View(sanPham);

            await _sanPhamService.UpdateSanPhamAsync(MaSanPham, sanPham);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int MaSanPham)
        {
            var product = await _sanPhamService.GetSanPhamByIdAsync(MaSanPham);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int MaSanPham)
        {
            await _sanPhamService.DeleteSanPhamAsync(MaSanPham);
            return RedirectToAction(nameof(Index));
        }
    }
}
    
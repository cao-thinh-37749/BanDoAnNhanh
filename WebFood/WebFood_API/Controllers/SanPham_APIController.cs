using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood_API.Data;
using WebFood_API.Models;

namespace WebFood_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPham_APIController : ControllerBase
    {
        private readonly ConnectStr _connectStr;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SanPham_APIController(ConnectStr connectStr, IWebHostEnvironment webHostEnvironment)
        {
            _connectStr = connectStr;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("danhmuc")]
        public async Task<IActionResult> GetDanhMucList()
        {
            var danhMucList = await _connectStr.DanhMucSanPham
                .Select(dm => new { dm.MaDanhMuc, dm.TenDanhMuc })
                .ToListAsync();

            return Ok(danhMucList);
        }


        [HttpGet]
        public async Task<IActionResult> GetSanPhams(string? searchTerm, int? CategoryId, bool showOutOfStock, string? sort)
        {
            // Truy vấn dữ liệu dựa trên tham số nhận được từ URL
            var products = await _connectStr.SanPham
                .Include(sp => sp.DanhMucSanPham)
                .Where(sp =>
                    (!CategoryId.HasValue || sp.DanhMuc == CategoryId) &&
                    (showOutOfStock ? sp.SoLuong == 0 : sp.SoLuong > 0) &&
                    (string.IsNullOrEmpty(searchTerm) || sp.TenSanPham.Contains(searchTerm))
                ).ToListAsync();

            // Sắp xếp theo giá
            if (sort == "asc")
                products = products.OrderBy(sp => sp.Gia).ToList();
            else if (sort == "desc")
                products = products.OrderByDescending(sp => sp.Gia).ToList();
            else
                products = products.ToList();

            return Ok(products);
        }


        [HttpGet("{MaSanPham}")]
        public async Task<IActionResult> GetSanPham(int MaSanPham)
        {
            var product = await _connectStr.SanPham.FindAsync(MaSanPham);
            if (product == null) return NotFound();
            return Ok(product);
        }


        [HttpPost]
        public async Task<IActionResult> CreateSanPham([FromForm] SanPham sanPham)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState không hợp lệ!");
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(sanPham.HinhAnh))
                return BadRequest("Không có tệp ảnh.");

            var existingProduct = await _connectStr.SanPham
                .FirstOrDefaultAsync(sp => sp.TenSanPham.ToLower() == sanPham.TenSanPham.ToLower() && sp.DanhMuc == sanPham.DanhMuc);

            if (existingProduct != null)
            {
                existingProduct.SoLuong += sanPham.SoLuong;
                existingProduct.Gia = sanPham.Gia;
                existingProduct.MoTa = sanPham.MoTa;
                existingProduct.HinhAnh = sanPham.HinhAnh; // Lưu đường dẫn ảnh từ client
                existingProduct.NgayCapNhat = DateTime.Now;
                _connectStr.SanPham.Update(existingProduct);
            }
            else
            {
                // Nếu không có ảnh, dùng ảnh mặc định
                sanPham.HinhAnh = string.IsNullOrEmpty(sanPham.HinhAnh) ? "/images/default.jpg" : sanPham.HinhAnh;
                await _connectStr.SanPham.AddAsync(sanPham);
            }

            await _connectStr.SaveChangesAsync();
            // Trả về danh sách sản phẩm trực tiếp mà không cần gọi GetSanPhams
            return Ok(await _connectStr.SanPham.ToListAsync());
        }



        [HttpPut("{MaSanPham}")]
        public async Task<IActionResult> EditSanPham(int MaSanPham, [FromBody] SanPham sanPham)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (MaSanPham != sanPham.MaSanPham) return BadRequest("Mã sản phẩm không khớp");

            var product = await _connectStr.SanPham.FindAsync(MaSanPham);
            if (product == null) return NotFound();

            product.TenSanPham = sanPham.TenSanPham;
            product.Gia = sanPham.Gia;
            product.SoLuong = sanPham.SoLuong;
            product.DanhMuc = sanPham.DanhMuc;
            product.MoTa = sanPham.MoTa;
            product.HinhAnh = sanPham.HinhAnh;
            product.NgayCapNhat = DateTime.Now;

            _connectStr.SanPham.Update(product);
            await _connectStr.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{MaSanPham}")]
        public async Task<IActionResult> DeleteSanPham(int MaSanPham)
        {
            var product = await _connectStr.SanPham.FindAsync(MaSanPham);
            if (product == null) return NotFound();

            product.SoLuong = 0;
            product.NgayCapNhat = DateTime.Now;
            await _connectStr.SaveChangesAsync();
            return NoContent();
        }

    }
}

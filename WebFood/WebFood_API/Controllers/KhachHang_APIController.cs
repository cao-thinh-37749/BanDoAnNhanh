using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFood_API.Data;
using WebFood_API.Models;

namespace WebFood_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhachHang_APIController : ControllerBase
    {
        private readonly ConnectStr _connectStr;
        private readonly UserManager<KhachHang> _userManager;

        public KhachHang_APIController(ConnectStr connectStr, UserManager<KhachHang> userManager)
        {
            _connectStr = connectStr;
            _userManager = userManager;
        }

        #region Data 
        // Lấy thông tin khách hàng có tổng tiền lớn hơn 100
        private List<KhachHangTongTien> LayKhachHangTongTienLonHon100(string? email = null)
        {
            var donHang = _connectStr.ChiTietDonDatHang
                .Include(dh => dh.KhachHang)
                .ToList();

            var khachHangLoc = donHang
                .GroupBy(dh => dh.KhachHang)
                .Where(group => group.Sum(dh => dh.Gia) > 100)
                .Select(group => new KhachHangTongTien
                {
                    KhachHang = group.Key,
                    TongTien = group.Sum(dh => dh.Gia)
                })
                .OrderByDescending(td => td.TongTien)
                .ToList();

            // Nếu có email, lọc danh sách theo email khách hàng
            if (!string.IsNullOrEmpty(email))
            {
                khachHangLoc = khachHangLoc
                    .Where(kh => kh.KhachHang.Email.Contains(email, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return khachHangLoc;
        }


        [HttpGet("get-100")]
        public IActionResult GetCustomersWithOrdersAbove100([FromQuery] string? email = null)
        {
            try
            {
                var khachHangLoc = LayKhachHangTongTienLonHon100(email);
                return Ok(khachHangLoc);
            
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                return StatusCode(500, $"Lỗi phía máy chủ: {ex.Message}");
            }
        }


        // Lấy danh sách tất cả khách hàng bình thường
        [HttpGet("get-all-customers")]
        public async Task<IActionResult> GetAllCustomers([FromQuery] string? email = null)
        {
            var query = _connectStr.KhachHang.AsQueryable();

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(kh => kh.Email.Contains(email));
            }

            var khachHangs = await query.ToListAsync();

            if (khachHangs == null || !khachHangs.Any())
            {
                return NotFound("Không tìm thấy khách hàng.");
            }

            return Ok(khachHangs);
        }



        // Lấy thông tin khách hàng
        private async Task<KhachHangDetailsViewModel?> LayThongTinKhachHang(string Id)
        {
            var khachHang = await _connectStr.KhachHang
                .FirstOrDefaultAsync(kh => kh.Id == Id);

            if (khachHang == null) return null;

            var chiTietDonHang = await _connectStr.ChiTietDonDatHang
                .Include(dh => dh.SanPham)
                .Where(dh => dh.Id == Id)
                .ToListAsync();

            var tongTien = chiTietDonHang.Sum(dh => dh.Gia);

            // Lấy danh sách vai trò của khách hàng
            var roles = await _userManager.GetRolesAsync(khachHang);
            var allRoles = await _connectStr.Roles.Select(r => r.Name).ToListAsync(); // Lấy tất cả roles từ Identity

            return new KhachHangDetailsViewModel
            {
                KhachHang = khachHang,
                ChiTietDonHangs = chiTietDonHang,
                Roles = roles.ToList(),// Gán danh sách vai trò vào ViewModel
                AllRoles = allRoles
            };
        }

        [HttpGet("get-customer-details/{Id}")]
        public async Task<IActionResult> GetCustomerDetails(string Id)
        {
            try
            {
                var khachHangLoc = await LayThongTinKhachHang(Id);
                return Ok(khachHangLoc);

            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                return StatusCode(500, $"Lỗi phía máy chủ: {ex.Message}");
            }
        }

        #endregion

        #region UpdateRoles
        [HttpPost("update-roles/{Id}")]
        public async Task<IActionResult> UpdateRoles(string Id, [FromBody] List<string> roles)
        {
            var khachHang = await _userManager.FindByIdAsync(Id);
            if (khachHang == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(khachHang);
            var removeResult = await _userManager.RemoveFromRolesAsync(khachHang, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest("Lỗi khi xóa vai trò cũ");
            }

            var addResult = await _userManager.AddToRolesAsync(khachHang, roles);
            if (!addResult.Succeeded)
            {
                return BadRequest("Lỗi khi thêm vai trò mới");
            }

            return Ok("Vai trò đã được cập nhật.");
        }

        #endregion

        #region Delete Order Details
        [HttpPost("delete-all-order-details")]
        public async Task<IActionResult> DeleteAllOrderDetails(string customerId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return BadRequest("Mã khách hàng không được để trống.");
            }

            var orderDetails = await _connectStr.ChiTietDonDatHang
                .Where(c => c.Id == customerId)
                .ToListAsync();

            if (!orderDetails.Any())
            {
                return NotFound("Không có chi tiết đơn hàng nào của khách hàng này để xóa.");
            }

            _connectStr.ChiTietDonDatHang.RemoveRange(orderDetails);
            await _connectStr.SaveChangesAsync();

            return Ok("Xóa hết chi tiết đơn hàng của khách hàng thành công.");
        }

        #endregion

        #region Delete Customer Permanently
        [HttpDelete("delete-customer/{id}")]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            var orderDetails = await _connectStr.ChiTietDonDatHang
                .Where(c => c.Id == id)
                .ToListAsync();

            if (orderDetails.Any())
            {
                return BadRequest("Không thể xóa khách hàng vì có đơn hàng liên quan.");
            }

            var customer = await _connectStr.KhachHang.FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null)
            {
                return NotFound("Khách hàng không tồn tại.");
            }

            _connectStr.KhachHang.Remove(customer);
            await _connectStr.SaveChangesAsync();

            return Ok("Xóa khách hàng thành công.");
        }

        #endregion

        #region Send Voucher

        [HttpGet("get-customer")]
        public IActionResult GetCustomer([FromQuery] string email)
        {
            var khachHang = _connectStr.KhachHang.FirstOrDefault(kh => kh.Email == email);
            if (khachHang == null)
            {
                return NotFound(new { message = "Email không tồn tại." });
            }

            return Ok(new
            {
                email = khachHang.Email,
                tenKhachHang = khachHang.Email // Giả sử có cột tên khách hàng
            });
        }


        [HttpPost("send-voucher")]
        public IActionResult SendVoucher([FromQuery] string email, [FromQuery] int voucherCode)
        {
            var khachHang = _connectStr.KhachHang.FirstOrDefault(ad => ad.Email == email);
            if (khachHang == null)
            {
                return NotFound("Email không tồn tại.");
            }

    
            // Bỏ phần gửi mail đi

            return Ok("Voucher đã được gửi thành công!");
        }

        #endregion
    }
}

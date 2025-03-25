using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebFood.Areas.Admin.fliter;
using WebFood.Models;

namespace WebFood.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminSession]
    public class RolesController : BaseController
    {
        private readonly HttpClient _httpClient;

        public RolesController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Hiển thị danh sách vai trò
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("https://localhost:7023/api/roles");
            if (!response.IsSuccessStatusCode)
            {
                return View(new List<RoleViewModel>());
            }

            var jsonData = await response.Content.ReadAsStringAsync();
            var roles = JsonSerializer.Deserialize<List<RoleViewModel>>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(roles);
        }

        // GET: Tạo vai trò mới
        public IActionResult Create()
        {
            return View();
        }

        // POST: Xử lý tạo vai trò mới
        [HttpPost]
        public async Task<IActionResult> Create(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var content = new StringContent(JsonSerializer.Serialize(roleName), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://localhost:7023/api/roles", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "Không thể tạo vai trò.");
            }
            return View();
        }

        // GET: Sửa vai trò
        public async Task<IActionResult> Edit(string id)
        {
            var response = await _httpClient.GetAsync($"https://localhost:7023/api/roles/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var jsonData = await response.Content.ReadAsStringAsync();
            var role = JsonSerializer.Deserialize<RoleViewModel>(jsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(role);
        }

        // POST: Xử lý cập nhật vai trò
        [HttpPost]
        public async Task<IActionResult> Edit(string id, string roleName)
        {
            var content = new StringContent(JsonSerializer.Serialize(new { roleName }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"https://localhost:7023/api/roles/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, "Cập nhật vai trò thất bại.");
            return View();
        }

        // Xóa vai trò
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await _httpClient.DeleteAsync($"https://localhost:7023/api/roles/{id}");

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }
            // Nếu API trả về lỗi 400 (BadRequest), lấy thông báo lỗi từ API
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa role.";
            }
            ModelState.AddModelError(string.Empty, "Không thể xóa vai trò.");
            return RedirectToAction(nameof(Index));
        }

    }

}

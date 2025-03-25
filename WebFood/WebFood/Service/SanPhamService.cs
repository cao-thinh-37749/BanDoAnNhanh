using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Collections.Generic;
using WebFood.Models;

namespace WebFood.Service
{
    public class SanPhamService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://localhost:7023/api/SanPham_API"; // Cập nhật URL đúng của API

        public SanPhamService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<List<SelectListItem>> GetDanhMucsAsync()
        {
            // Gọi API để lấy danh sách danh mục sản phẩm
            var danhMucList = await _httpClient.GetFromJsonAsync<List<DanhMucSanPham>>($"{_apiBaseUrl}/danhmuc");

            // Chuyển đổi danh sách `DanhMucSanPham` thành `SelectListItem`
            var selectList = danhMucList?.Select(dm => new SelectListItem
            {
                Value = dm.MaDanhMuc.ToString(), // Giá trị danh mục
                Text = dm.TenDanhMuc // Tên danh mục hiển thị
            }).ToList();

            return selectList ?? new List<SelectListItem>(); // Trả về danh sách rỗng nếu null
        }


        public async Task<List<SanPham>> GetSanPhamsAsync(string searchTerm, int? CategoryId, bool showOutOfStock, string sort)
        {
            string apiUrl;

            // Kiểm tra nếu không có tham số, sử dụng URL mặc định
            if (string.IsNullOrEmpty(searchTerm) && !CategoryId.HasValue && !showOutOfStock && string.IsNullOrEmpty(sort))
            {
                apiUrl = "https://localhost:7023/api/SanPham_API"; // URL mặc định khi không có tham số
            }
            else
            {
                // Tạo URL API với các tham số nếu có
                apiUrl = $"{_apiBaseUrl}?searchTerm={searchTerm}&CategoryId={CategoryId}&showOutOfStock={showOutOfStock}&sort={sort}";
            }

            return await _httpClient.GetFromJsonAsync<List<SanPham>>(apiUrl);
        }

        public async Task<SanPham> GetSanPhamByIdAsync(int MaSanPham)
        {
            return await _httpClient.GetFromJsonAsync<SanPham>($"{_apiBaseUrl}/{MaSanPham}");
        }

        public async Task<bool> CreateSanPhamAsync(SanPham sanPham, string imageUrl)
        {
            if (sanPham == null || string.IsNullOrEmpty(imageUrl))
            {
                Console.WriteLine("❌ Dữ liệu không hợp lệ.");
                return false;
            }

            // Kiểm tra đường dẫn ảnh (imageUrl) có hợp lệ không nếu cần
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!allowedExtensions.Contains(Path.GetExtension(imageUrl).ToLower()))
            {
                Console.WriteLine(" Ảnh không đúng định dạng!");
                return false;
            }

            // Xây dựng nội dung gửi lên API
            var content = new MultipartFormDataContent
            {
                { new StringContent(sanPham.TenSanPham ?? ""), "TenSanPham" },
                { new StringContent(sanPham.DanhMuc.ToString()), "DanhMuc" },
                { new StringContent(imageUrl ?? ""), "HinhAnh" },
                { new StringContent(sanPham.SoLuong.ToString()), "SoLuong" },
                { new StringContent(sanPham.Gia.ToString()), "Gia" },
                { new StringContent(sanPham.MoTa ?? ""), "MoTa" }
            };

            // Gửi yêu cầu POST tới API
            var response = await _httpClient.PostAsync(_apiBaseUrl, content);
            Console.WriteLine($"🔍 Response: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateSanPhamAsync(int MaSanPham, SanPham sanPham)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/{MaSanPham}", sanPham);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteSanPhamAsync(int MaSanPham)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/{MaSanPham}");
            return response.IsSuccessStatusCode;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.Collections.Generic;
using WebFood.Models;

namespace WebFood.Service
{
    public class KhachHangService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://localhost:7023/api/KhachHang_API"; // Cập nhật URL đúng của API

        public KhachHangService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Lấy danh sách tất cả khách hàng
        public async Task<List<KhachHang>> GetAllKhachHangsAsync(string? email = null)
        {
            string url = $"{_apiBaseUrl}/get-all-customers";
            if (!string.IsNullOrEmpty(email))
            {
                url += $"?email={email}";
            }
            return await _httpClient.GetFromJsonAsync<List<KhachHang>>(url);
        }

        // Lấy danh sách khách hàng có tổng tiền lớn hơn 100
        public async Task<List<KhachHangTongTien>> GetCustomersWithOrdersAbove100Async(string? email = null)
        {
            string url = $"{_apiBaseUrl}/get-100";
            if (!string.IsNullOrEmpty(email))
            {
                url += $"?email={email}";
            }
            return await _httpClient.GetFromJsonAsync<List<KhachHangTongTien>>(url);
        }


        // Lấy thông tin chi tiết khách hàng
        public async Task<KhachHangDetailsViewModel?> GetKhachHangByIdAsync(string Id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<KhachHangDetailsViewModel>($"{_apiBaseUrl}/get-customer-details/{Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi API: {ex.Message}");
                return null;
            }
        }

    
        // Cập nhật vai trò cho khách hàng
        public async Task<bool> UpdateRolesAsync(string id, List<string> roles)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/update-roles/{id}", roles);
            return response.IsSuccessStatusCode;
        }

        // Xóa khách hàng
        public async Task<bool> DeleteKhachHangAsync(string id)
        {
            var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/delete-customer/{id}");
            return response.IsSuccessStatusCode;
        }

        // Gửi Voucher cho khách hàng
        public async Task<bool> SendVoucherAsync(string email, int selectedVoucher)
        {
            var url = $"{_apiBaseUrl}/send-voucher?email={Uri.EscapeDataString(email)}&voucherCode={selectedVoucher}";

            var response = await _httpClient.PostAsync(url, null); // Không cần body

            return response.IsSuccessStatusCode;
        }

        public async Task<KhachHang?> GetCustomerByEmailAsync(string email)
        {
            var url = $"{_apiBaseUrl}/get-customer?email={Uri.EscapeDataString(email)}";
            return await _httpClient.GetFromJsonAsync<KhachHang>(url);
        }


        // Xóa tất cả chi tiết đơn hàng của khách hàng
        public async Task<bool> DeleteAllOrderDetailsAsync(string customerId)
        {
            var url = $"{_apiBaseUrl}/delete-all-order-details?customerId={customerId}";

            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }
    }
}

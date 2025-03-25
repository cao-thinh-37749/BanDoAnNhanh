using Microsoft.EntityFrameworkCore;
using WebFood.Models;
using WebFood.Data;
using System.Linq;
	
namespace WebFood.Data
{
	public class GetCount
	{
		private readonly ConnectStr _context;
		public GetCount(ConnectStr context)
		{
			_context = context;
		}
		public int GetCartCount(string userEmail)
		{
			if (string.IsNullOrEmpty(userEmail)) return 0;

			var customer = _context.KhachHang.FirstOrDefault(k => k.Email == userEmail);
			if (customer == null) return 0;

			return _context.GioHang
				.Where(g => g.Id == customer.Id)
				.Sum(g => (int?)g.SoLuong) ?? 0;
		}

		public int GetFavoriteProductCount(string userEmail)
		{
			if (string.IsNullOrEmpty(userEmail)) return 0;

			var customer = _context.KhachHang.FirstOrDefault(k => k.Email == userEmail);
			if (customer == null) return 0;

			return _context.SanPhamYeuThich
				.Where(g => g.Id == customer.Id)
				.Count();
		}
	}
}

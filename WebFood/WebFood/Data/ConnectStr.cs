using WebFood.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WebFood.Models;

namespace WebFood.Data
{
    public class ConnectStr : IdentityDbContext<KhachHang>
    {
        public ConnectStr(DbContextOptions<ConnectStr> options) : base(options) { }

        public DbSet<KhachHang> KhachHang { get; set; }
        public DbSet<DanhMucSanPham> DanhMucSanPham { get; set; }
        public DbSet<SanPham> SanPham { get; set; }
        public DbSet<SanPhamYeuThich> SanPhamYeuThich { get; set; }
        public DbSet<GioHang> GioHang { get; set; }
        public DbSet<ChiTietDonDatHang> ChiTietDonDatHang { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Định danh bảng KhachHang
            modelBuilder.Entity<KhachHang>()
                .ToTable("KhachHang")
                .HasKey(k => k.Id); // Thay Id bằng MaKhachHang làm khóa chính

            modelBuilder.Entity<KhachHang>()
                .HasKey(kh => kh.Id);

            modelBuilder.Entity<DanhMucSanPham>()
                .HasKey(dsp => dsp.MaDanhMuc);

            modelBuilder.Entity<SanPham>()
                .HasKey(sp => sp.MaSanPham);

            modelBuilder.Entity<SanPhamYeuThich>()
                .HasKey(sp => sp.MaYeuThich);

            modelBuilder.Entity<GioHang>()
                .HasKey(gh => gh.MaGioHang);

            modelBuilder.Entity<ChiTietDonDatHang>()
                .HasKey(ct => ct.MaChiTiet);
        }


    }
}

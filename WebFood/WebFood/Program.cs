using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebFood.Data;
using WebFood.Models;
using WebFood.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<SendMail>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddHttpClient<SanPhamService>();
builder.Services.AddHttpClient<KhachHangService>();



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn phiên
    options.Cookie.HttpOnly = true; // Bảo mật cookie
    options.Cookie.IsEssential = true; // Cookie cần thiết cho phiên
});

//api
builder.Services.AddHttpClient();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Cấu hình khóa tài khoản
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15); // Khóa trong 15 phút
    options.Lockout.MaxFailedAccessAttempts = 3; // Số lần đăng nhập thất bại trước khi khóa
    options.Lockout.AllowedForNewUsers = true;
});

//Đăng ký Identity 
builder.Services.AddIdentity<KhachHang, IdentityRole>()
    .AddEntityFrameworkStores<ConnectStr>()
    .AddDefaultTokenProviders();


// Cấu hình dịch vụ Google Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie() // Sử dụng Cookie Authentication
.AddGoogle(options =>
{
    options.ClientId = "1026167841819-0lupg5ftub8iu2797ait562sg9f0fcj8.apps.googleusercontent.com"; // Thay thế bằng Client ID của bạn
    options.ClientSecret = "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"; // Thay thế bằng Client Secret của bạn
    options.CallbackPath = "/signin-google";
});



// Đăng ký DbContext với DI container, cấu hình sử dụng SQL Server với chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<ConnectStr>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectStr")));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Khởi tạo vai trò và tài khoản admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await RoleInitializer.CreateRoles(services);
}

// Cấu hình middleware authentication
app.UseAuthentication();
app.UseAuthorization();


//đăng ký areas 
app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

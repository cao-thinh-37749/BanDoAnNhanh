using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebFood_API.Data;
using WebFood_API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Đăng ký DbContext với DI container, cấu hình sử dụng SQL Server với chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<ConnectStr>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectStr")));

builder.Services.AddIdentity<KhachHang, IdentityRole>()
    .AddEntityFrameworkStores<ConnectStr>()
    .AddDefaultTokenProviders();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

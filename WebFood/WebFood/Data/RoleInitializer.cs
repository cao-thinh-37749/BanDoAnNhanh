using Microsoft.AspNetCore.Identity;

using WebFood.Models;

namespace WebFood.Data
{
    public class RoleInitializer
    {
   
        public static async Task CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<KhachHang>>();

            string[] roleNames = { "admin", "customer" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Tạo tài khoản admin mặc định
            var adminEmail = "caothinh467@gmail.com";
            var adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new KhachHang
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    PhoneNumber = "0398180979",
                    EmailConfirmed = true
                };

                var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
                if (!createAdmin.Succeeded)
                {
                    foreach (var error in createAdmin.Errors)
                    {
                        Console.WriteLine(error.Description);
                    }
                }

                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "admin");
                }
            }
        }
    }
}

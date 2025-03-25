using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebFood_API.Data;

namespace WebFood_API.Controllers
{
    [Route("api/roles")]
    [ApiController]
    public class Roles_APIController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ConnectStr _connectStr;

        public Roles_APIController(RoleManager<IdentityRole> roleManager, ConnectStr connectStr)
        {
            _roleManager = roleManager;
            _connectStr = connectStr;
        }

        // GET: api/roles
        [HttpGet]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => new { r.Id, r.Name });
            return Ok(roles);
        }

        // GET: api/roles/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound("Role not found.");
            }

            return Ok(role);
        }

        // POST: api/roles
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return BadRequest("Role name is required.");
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                return Ok(new { Message = "Role created successfully." });
            }
            return BadRequest(result.Errors);
        }

        // PUT: api/roles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return BadRequest("Role name cannot be empty.");
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound("Role not found.");
            }

            role.Name = roleName;
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Role updated successfully." });
            }
            return BadRequest(result.Errors);
        }


        // DELETE: api/roles/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteRole(string id)
        {
            // Kiểm tra xem có user nào thuộc role này không
            bool hasUsersWithRole = _connectStr.UserRoles.Any(ur => ur.RoleId == id);
            if (hasUsersWithRole)
            {
                return BadRequest("Không thể xóa, có người dùng đang sử dụng role này.");
            }

            // Nếu không có user nào, thực hiện xóa
            var role = _connectStr.Roles.FirstOrDefault(r => r.Id == id);
            if (role == null)
            {
                return NotFound("Role không tồn tại.");
            }

            _connectStr.Roles.Remove(role);
            _connectStr.SaveChanges();

            return Ok("Role đã được xóa thành công.");
        }
    }
}

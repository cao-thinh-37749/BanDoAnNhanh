using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace WebFood.fliter
{
	public class UserSessionFilter : ActionFilterAttribute
	{
		public override void OnActionExecuted(ActionExecutedContext context)
		{
			var session = context.HttpContext.Session.GetString("UserEmail");
			if (string.IsNullOrEmpty(session))
			{
				context.Result = new RedirectToActionResult("Login", "Login", null);
			}
		}
	}

	// Lấy session ra
	public class UserBaseController : Controller
	{
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			// Lấy thông tin từ Session
			ViewBag.UserMaKhachHang = HttpContext.Session.GetString("UserMaKhachHang");
			ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
			ViewBag.UserImage = HttpContext.Session.GetString("UserImage");

			base.OnActionExecuting(context);
		}
	}
}

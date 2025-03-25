using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebFood.Areas.Admin.fliter
{
    public class AdminSession : ActionFilterAttribute 
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var session = context.HttpContext.Session.GetString("adminEmail"); context.HttpContext.Session.GetString("adminImage");
            if (string.IsNullOrEmpty(session))
            {
                context.Result = new RedirectToActionResult("LoginAd", "LoginAdmin",new {area = "Admin"});
            }
        }
    }

    //lấy session ra 
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Lấy email từ Session
            ViewBag.Email = HttpContext.Session.GetString("adminEmail");
            ViewBag.Hinh = HttpContext.Session.GetString("adminImage");

            base.OnActionExecuting(context);
        }
    }
}

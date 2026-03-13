using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace proyect.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            ViewBag.IsElectron = userAgent.Contains("electron-launcher");

            base.OnActionExecuting(context);
        }
    }
}
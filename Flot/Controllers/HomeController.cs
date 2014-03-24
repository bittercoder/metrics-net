using System.Web.Mvc;
using metrics;
using metrics.Util;

namespace Flot.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult GetSample()
        {
            var content = Serializer.Serialize(MvcApplication.Metrics.AllSorted);
            
            return Content(content,"application/json");
        }
    }
}

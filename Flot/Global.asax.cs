using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using metrics;

namespace Flot
{
    public class MvcApplication : HttpApplication
    {
        public static readonly Metrics Metrics = new Metrics();

        static MvcApplication()
        {
            var machineMetrics = new MachineMetrics(Metrics);
            machineMetrics.InstallPhysicalDisk();
            machineMetrics.InstallLogicalDisk();
            machineMetrics.InstallCLRLocksAndThreads();

            Metrics.Gauge(typeof(MvcApplication), "hey_you_guys", () =>
            {
                return 12;
            });
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}
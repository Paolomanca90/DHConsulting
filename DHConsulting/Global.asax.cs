using DHConsulting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace DHConsulting
{
    public static class AppData
    {
        public static List<Prodotto> Prodotti { get; set; }
    }

    public class WebApiApplication : System.Web.HttpApplication
    {
        private ModelDb db = new ModelDb();
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AppData.Prodotti = db.Prodotto.ToList();
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.IO;
using NHibernate.Cfg;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Caches.Redis.Sample.Mapping;
using ServiceStack.Redis;

namespace NHibernate.Caches.Redis.Sample
{
    public class MvcApplication : System.Web.HttpApplication
    {
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

        public static ISessionFactory SessionFactory { get; private set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            var clientManager = new BasicRedisClientManager("localhost:6379");
            
            using (var client = clientManager.GetClient())
            {
                client.FlushDb();
            }

            RedisCacheProvider.SetClientManager(clientManager);

            var dbFile = HttpContext.Current.Server.MapPath("~/App_Data/sample.db");

            if (File.Exists(dbFile)) { File.Delete(dbFile); }

            var configuration = Fluently.Configure()
                .Database(
                    SQLiteConfiguration.Standard.UsingFile(dbFile)
                )
                .Mappings(m =>
                {
                    m.FluentMappings.Add(typeof(BlogPostMapping));
                })
                .ExposeConfiguration(cfg =>
                {
                    cfg.SetProperty(NHibernate.Cfg.Environment.GenerateStatistics, "true");
                })
                .Cache(c =>
                {
                    c.UseQueryCache().UseSecondLevelCache().ProviderClass<RedisCacheProvider>();
                })
                .BuildConfiguration();

            new SchemaExport(configuration).Create(false, true);

            SessionFactory = configuration.BuildSessionFactory();
        }
    }
}
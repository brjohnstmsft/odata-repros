using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using System.ComponentModel.DataAnnotations;

namespace ODataAndCors
{
    public class Field
    {
        public string Name { get; set; }

        public string Type { get; set; }
    }

    public class Index
    {
        [Key]
        public string Name { get; set; }

        public IList<Field> Fields { get; set; }
    }

    public class IndexesController : ControllerBase
    {
        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public IList<Index> Get()
        {
            return new[] {
            new Index()
            {
                Name = "myindex",
                Fields = new[]
                {
                    new Field() { Name = "a", Type = "int" },
                    new Field() { Name = "b", Type = "string" }
                }
            }
        };
        }

        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public int DoSomething() => 0;
    }

    public class PingController : ControllerBase
    {
        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public IActionResult Get() => Ok();

        [HttpGet]
        [EnableCors("AllowAllOrigins")]
        public int DoSomething() => 0;
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddOData();
            services.AddCors(options =>
                options.AddPolicy(
                    "AllowAllOrigins",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            IEdmModel model = BuildModel();
            app.UseMvc(routeBuilder =>
            {
                routeBuilder.MapRoute("ping", "{controller=Ping}", new { action = "Get" });
                routeBuilder.MapRoute("dosomething", "{controller=Ping}/do", new { action = "DoSomething" });
                routeBuilder.MapODataServiceRoute("odata", "odata", model);
                routeBuilder.EnableDependencyInjection();
            });
        }

        private static IEdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder
                .EntitySet<Index>("Indexes")
                .EntityType
                .Function("DoSomething")
                .Returns<int>();

            return builder.GetEdmModel();
        }
    }
}

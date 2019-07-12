using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Repro
{
    public static class Startup
    {
        public static void Configure(HttpConfiguration httpConfig)
        {
            IEdmModel model = StaticModel.BuildModel();
            DynamicModel.AddToModel(model);
            httpConfig.MapODataServiceRoute("odata", "odata", builder =>
            {
                var odataMessageReaderSettings = new ODataMessageReaderSettings { ReadUntypedAsString = false };

                builder
                    .AddServicePrototype(odataMessageReaderSettings)
                    .AddService(ServiceLifetime.Singleton, sp => model)
                    .AddService<IODataPathHandler>(ServiceLifetime.Singleton, sp => new DefaultODataPathHandler())
                    .AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => ODataRoutingConventions.CreateDefault());
            });

            httpConfig.Filters.Add(new ActionArgumentValidationAttribute());
        }

        private sealed class ActionArgumentValidationAttribute : ActionFilterAttribute
        {
            public override void OnActionExecuting(HttpActionContext actionContext)
            {
                HttpRequestMessage request = actionContext.Request;

                if (!actionContext.ModelState.IsValid)
                {
                    HttpRequestContext requestContext = request.GetRequestContext();
                    bool oldIncludeErrorDetail = requestContext.IncludeErrorDetail;

                    try
                    {
                        requestContext.IncludeErrorDetail = true;
                        actionContext.Response = request.CreateErrorResponse(HttpStatusCode.BadRequest, actionContext.ModelState);
                    }
                    finally
                    {
                        requestContext.IncludeErrorDetail = oldIncludeErrorDetail;
                    }

                    return;
                }
            }
        }
    }
}

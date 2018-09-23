using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.OData.Extensions;
using Microsoft.OData.Edm;

namespace Repro
{
    public static class Startup
    {
        public static void Configure(HttpConfiguration httpConfig)
        {
            IEdmModel model = Model.BuildModel();
            httpConfig.MapODataServiceRoute("odata", "odata", model);
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

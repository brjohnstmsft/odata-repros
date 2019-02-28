using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Repro
{
    public static class Startup
    {
        public static void Configure(HttpConfiguration httpConfig)
        {
            IEdmModel model = Model.BuildModel();
            httpConfig.MapODataServiceRoute("odata", "odata", builder =>
            {
                var odataMessageReaderSettings =
                    new ODataMessageReaderSettings
                    {
                        ReadUntypedAsString = false,     // Comment out this line and the next to see EscapedTabShouldRoundTripCorrectly fail.
                        PrimitiveTypeResolver = Resolve  // Comment out this line to see IntDynamicPropertyDeserializesCorrectly fail.
                    };

                builder
                    .AddServicePrototype(odataMessageReaderSettings)
                    .AddService(ServiceLifetime.Singleton, sp => model)
                    .AddService<IODataPathHandler>(ServiceLifetime.Singleton, sp => new DefaultODataPathHandler())
                    .AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => ODataRoutingConventions.CreateDefault());
            });
            httpConfig.Filters.Add(new ActionArgumentValidationAttribute());
        }

        private static IEdmTypeReference Resolve(object value, string payloadTypeName)
        {
            // The ODataLib JSON parser only supports three kinds of numeric .NET types: int, double, and decimal.
            switch (value)
            {
                case int _:
                    return EdmCoreModel.Instance.GetInt32(isNullable: true);

                default:
                    return null;
            }
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Repro
{
    public static class Startup
    {
        public static void Configure(HttpConfiguration httpConfig)
        {
            const string RoutePrefix = "odata";
            const string RouteName = "odata";

            IEdmModel model = Model.Build();
            httpConfig.MapODataServiceRoute(
                RoutePrefix,
                RouteName,
                builder =>
                {
                    builder
                        .AddService(ServiceLifetime.Singleton, sp => model)

                        // Uncomment the line below to test the workaround.
                        ////.AddService<ODataResourceSetDeserializer, CustomResourceSetDeserializer>(ServiceLifetime.Singleton)
                        .AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => ODataRoutingConventions.CreateDefault());
                });

            httpConfig.Filters.Add(new ActionArgumentValidationAttribute());
        }

        private sealed class CustomResourceSetDeserializer : ODataResourceSetDeserializer
        {
            public CustomResourceSetDeserializer(ODataDeserializerProvider deserializerProvider) : base(deserializerProvider)
            {
            }

            public override IEnumerable ReadResourceSet(
                ODataResourceSetWrapper resourceSet,
                IEdmStructuredTypeReference elementType,
                ODataDeserializerContext readContext)
            {
                if (elementType.IsComplex() && !elementType.IsNullable &&
                    resourceSet.Resources.Any(r => r == null))
                {
                    string typeRefString =
                        $"{elementType.FullName()}[Nullable={elementType.IsNullable}]";

                    string message =
                        $"A null value was found with the expected type '{typeRefString}'. The expected type " +
                        $"'{typeRefString}' does not allow null values.";

                    throw new ODataException(message);
                }

                return base.ReadResourceSet(resourceSet, elementType, readContext);
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

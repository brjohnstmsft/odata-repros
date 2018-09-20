using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;

namespace ODataAndCorsClassic
{
    /// <summary>
    /// This class has been copied from 'ODataPathRouteConstraint' from ODataLib.
    /// The 'Match' api has been modified to use the existing RequestContainer, if it already exists
    /// instead of creating a new one.
    /// The CreateRequestContainer() API throws an error if the request container already exists.
    /// Since, in our implementation we expect the Match API to be called multiple times, it has been slightly
    /// modifed to comsme the existing RequestContainer.
    /// </summary>
    public class CustomODataPathRouteConstraint : ODataPathRouteConstraint
    {
        private const string RequestContainerKey = "Microsoft.AspNet.OData.RequestContainer";

        // "%2F"
        private static readonly string _escapedSlash = Uri.EscapeDataString("/");

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataPathRouteConstraint" /> class.
        /// </summary>
        /// <param name="routeName">The name of the route this constraint is associated with.</param>
        public CustomODataPathRouteConstraint(string routeName) : base(routeName)
        {
        }

        /// <summary>
        /// Get the OData path from the url and query string.
        /// </summary>
        /// <param name="oDataPathString">The ODataPath from the route values.</param>
        /// <param name="uriPathString">The Uri from start to end of path, i.e. the left portion.</param>
        /// <param name="queryString">The Uri from the query string to the end, i.e. the right portion.</param>
        /// <param name="requestContainerFactory">The request container factory.</param>
        /// <returns>The OData path.</returns>
        private static ODataPath GetODataPath(string oDataPathString, string uriPathString, string queryString, Func<IServiceProvider> requestContainerFactory)
        {
            ODataPath path = null;

            try
            {
                // Service root is the current RequestUri, less the query string and the ODataPath (always the
                // last portion of the absolute path).  ODL expects an escaped service root and other service
                // root calculations are calculated using AbsoluteUri (also escaped).  But routing exclusively
                // uses unescaped strings, determined using
                //    address.GetComponents(UriComponents.Path, UriFormat.Unescaped)
                //
                // For example if the AbsoluteUri is
                // <http://localhost/odata/FunctionCall(p0='Chinese%E8%A5%BF%E9%9B%85%E5%9B%BEChars')>, the
                // oDataPathString will contain "FunctionCall(p0='Chinese西雅图Chars')".
                //
                // Due to this decoding and the possibility of unnecessarily-escaped characters, there's no
                // reliable way to determine the original string from which oDataPathString was derived.
                // Therefore a straightforward string comparison won't always work.  See RemoveODataPath() for
                // details of chosen approach.
                string serviceRoot = uriPathString;

                if (!String.IsNullOrEmpty(oDataPathString))
                {
                    serviceRoot = RemoveODataPath(serviceRoot, oDataPathString);
                }

                // As mentioned above, we also need escaped ODataPath.
                // The requestLeftPart and request.QueryString are both escaped.
                // The ODataPath for service documents is empty.
                string oDataPathAndQuery = uriPathString.Substring(serviceRoot.Length);

                if (!String.IsNullOrEmpty(queryString))
                {
                    // Ensure path handler receives the query string as well as the path.
                    oDataPathAndQuery += queryString;
                }

                // Leave an escaped '/' out of the service route because DefaultODataPathHandler will add a
                // literal '/' to the end of this string if not already present. That would double the slash
                // in response links and potentially lead to later 404s.
                if (serviceRoot.EndsWith(_escapedSlash, StringComparison.OrdinalIgnoreCase))
                {
                    serviceRoot = serviceRoot.Substring(0, serviceRoot.Length - _escapedSlash.Length);
                }

                IServiceProvider requestContainer = requestContainerFactory();
                IODataPathHandler pathHandler = requestContainer.GetRequiredService<IODataPathHandler>();
                path = pathHandler.Parse(serviceRoot, oDataPathAndQuery, requestContainer);
            }
            catch (ODataException)
            {
                path = null;
            }

            return path;
        }

        // Find the substring of the given URI string before the given ODataPath.  Tests rely on the following:
        // 1. ODataPath comes at the end of the processed Path
        // 2. Virtual path root, if any, comes at the beginning of the Path and a '/' separates it from the rest
        // 3. OData prefix, if any, comes between the virtual path root and the ODataPath and '/' characters separate
        //    it from the rest
        // 4. Even in the case of Unicode character corrections, the only differences between the escaped Path and the
        //    unescaped string used for routing are %-escape sequences which may be present in the Path
        //
        // Therefore, look for the '/' character at which to lop off the ODataPath.  Can't just unescape the given
        // uriString because subsequent comparisons would only help to check whether a match is _possible_, not where
        // to do the lopping.
        private static string RemoveODataPath(string uriString, string oDataPathString)
        {
            // Potential index of oDataPathString within uriString.
            int endIndex = uriString.Length - oDataPathString.Length - 1;
            if (endIndex <= 0)
            {
                // Bizarre: oDataPathString is longer than uriString.  Likely the values collection passed to Match()
                // is corrupt.
                throw new InvalidOperationException("RequestUri is too short for ODataPath.");
                //throw Error.InvalidOperation(SRResources.RequestUriTooShortForODataPath, uriString, oDataPathString);
            }

            string startString = uriString.Substring(0, endIndex + 1);  // Potential return value.
            string endString = uriString.Substring(endIndex + 1);       // Potential oDataPathString match.
            if (String.Equals(endString, oDataPathString, StringComparison.Ordinal))
            {
                // Simple case, no escaping in the ODataPathString portion of the Path.  In this case, don't do extra
                // work to look for trailing '/' in startString.
                return startString;
            }

            while (true)
            {
                // Escaped '/' is a derivative case but certainly possible.
                int slashIndex = startString.LastIndexOf('/', endIndex - 1);
                int escapedSlashIndex =
                    startString.LastIndexOf(_escapedSlash, endIndex - 1, StringComparison.OrdinalIgnoreCase);
                if (slashIndex > escapedSlashIndex)
                {
                    endIndex = slashIndex;
                }
                else if (escapedSlashIndex >= 0)
                {
                    // Include the escaped '/' (three characters) in the potential return value.
                    endIndex = escapedSlashIndex + 2;
                }
                else
                {
                    // Failure, unable to find the expected '/' or escaped '/' separator.
                    throw new InvalidOperationException(String.Format("Request URI '{0}' does not contain OData path '{1}'.", uriString, oDataPathString));
                }

                startString = uriString.Substring(0, endIndex + 1);
                endString = uriString.Substring(endIndex + 1);

                // Compare unescaped strings to avoid both arbitrary escaping and use of lowercase 'a' through 'f' in
                // %-escape sequences.
                endString = Uri.UnescapeDataString(endString);
                if (String.Equals(endString, oDataPathString, StringComparison.Ordinal))
                {
                    return startString;
                }

                if (endIndex == 0)
                {
                    // Failure, could not match oDataPathString after an initial '/' or escaped '/'.
                    throw new InvalidOperationException(String.Format("OData path not found. Could not match {0} after an initial '/' or escaped '/'", oDataPathString));
                }
            }
        }

        /// <summary>
        /// Determines whether this instance equals a specified route.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="route">The route to compare.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="values">A list of parameter values.</param>
        /// <param name="routeDirection">The route direction.</param>
        /// <returns>
        /// True if this instance equals a specified route; otherwise, false.
        /// </returns>
        /// <remarks>This signature uses types that are AspNet-specific.</remarks>
        public override bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            if (routeDirection == HttpRouteDirection.UriResolution)
            {
                ODataPath path = null;

                object oDataPathValue;
                if (values.TryGetValue(ODataRouteConstants.ODataPath, out oDataPathValue))
                {
                    string requestLeftPart = request.RequestUri.GetLeftPart(UriPartial.Path);
                    string queryString = request.RequestUri.Query;

                    path = GetODataPath(oDataPathValue as string, requestLeftPart, queryString, () => GetRequestContainer(request));
                }

                if (path != null)
                {
                    // Set all the properties we need for routing, querying, formatting
                    HttpRequestMessageProperties properties = request.ODataProperties();
                    properties.Path = path;
                    properties.RouteName = RouteName;

                    if (!values.ContainsKey(ODataRouteConstants.Controller))
                    {
                        // Select controller name using the routing conventions
                        string controllerName = SelectControllerName(path, request);
                        if (controllerName != null)
                        {
                            values[ODataRouteConstants.Controller] = controllerName;
                        }
                    }

                    return true;
                }

                // The request doesn't match this route so dispose the request container.
                request.DeleteRequestContainer(true);
                return false;
            }
            else
            {
                // This constraint only applies to URI resolution
                return true;
            }
        }

        /// <summary>
        /// Selects the name of the controller to dispatch the request to.
        /// </summary>
        /// <param name="path">The OData path of the request.</param>
        /// <param name="request">The request.</param>
        /// <returns>The name of the controller to dispatch to, or <c>null</c> if one cannot be resolved.</returns>
        /// <remarks>This signature uses types that are AspNet-specific.</remarks>
        protected override string SelectControllerName(ODataPath path, HttpRequestMessage request)
        {
            foreach (IODataRoutingConvention routingConvention in request.GetRoutingConventions())
            {
                string controllerName = routingConvention.SelectController(path, request);
                if (controllerName != null)
                {
                    return controllerName;
                }
            }

            return null;
        }

        private IServiceProvider GetRequestContainer(HttpRequestMessage request)
        {
            object value;
            if (request.Properties.TryGetValue(RequestContainerKey, out value))
            {
                return (IServiceProvider)value;
            }
            return request.CreateRequestContainer(RouteName);
        }
    }
}

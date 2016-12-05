using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.OData.Routing;
using Flurl;
using Microsoft.Web.Http;
using Microsoft.Web.OData.Routing;
using Swashbuckle.Swagger;

namespace Swashbuckle.OData.Descriptions
{
    internal static class OperationExtensions
    {
        /// <summary>
        /// The name of the apiVersion parameter for Microsoft.AspNet.OData.Versioning
        /// </summary>
        private const string ApiVersionKeyName = "apiVersion";

        public static IDictionary<string, string> GenerateSamplePathParameterValues(this Operation operation)
        {
            Contract.Requires(operation != null);

            return operation.parameters?.Where(parameter => parameter.@in == "path")
                .ToDictionary(queryParameter => queryParameter.name, queryParameter => queryParameter.GenerateSamplePathParameterValue());
        }

        public static string GenerateSampleODataUri(this Operation operation, string serviceRoot, string pathTemplate)
        {
            Contract.Requires(operation != null);
            Contract.Requires(serviceRoot != null);

            var parameters = operation.GenerateSamplePathParameterValues();

            if (parameters != null && parameters.Any())
            {
                var prefix = new Uri(serviceRoot);

                return new UriTemplate(pathTemplate).BindByName(prefix, parameters).ToString();
            }
            return serviceRoot.AppendPathSegment(pathTemplate);
        }

        public static IList<Parameter> Parameters(this Operation operation)
        {
            Contract.Requires(operation != null);

            return operation.parameters ?? (operation.parameters = new List<Parameter>());
        }

        /// <summary>
        /// Generate the Sample OData Uri with support for the library:
        /// Microsoft.AspNet.OData.Versioning
        /// </summary>
        /// <param name="operation">this operation</param>
        /// <param name="serviceRoot">service root value</param>
        /// <param name="pathTemplate">path template value</param>
        /// <param name="odataRoute">odata route value</param>
        /// <returns></returns>
        public static string GenerateSampleODataUri
        (
            this Operation operation,
            string serviceRoot,
            string pathTemplate,
            ODataRoute odataRoute
        )
        {
            Contract.Requires(operation != null);
            Contract.Requires(serviceRoot != null);
            Contract.Requires(odataRoute != null);

            var parameters = operation.GenerateSamplePathParameterValues();

            if (parameters == null || !parameters.Any())
                return serviceRoot.AppendPathSegment(pathTemplate);

            // Check for ApiVersion contraint and Uri parameter value defined
            // in the Microsoft.AspNet.OData.Versioning library
            var version = default(ApiVersion);
            if (odataRoute.RouteConstraint.GetType()
                    == typeof(VersionedODataPathRouteConstraint)
               )
            {
                version = (odataRoute.RouteConstraint 
                    as VersionedODataPathRouteConstraint)?.ApiVersion;
            }
            if (null != version && parameters.ContainsKey(ApiVersionKeyName))
            {
                parameters[ApiVersionKeyName] = version.ToString();
            }

            var prefix = new Uri(serviceRoot);

            return new UriTemplate(pathTemplate)
                        .BindByName(prefix, parameters)
                        .ToString();
        }
    }
}
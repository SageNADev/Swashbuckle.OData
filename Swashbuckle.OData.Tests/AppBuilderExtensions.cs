using System;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;
using Microsoft.Web.Http;
using Microsoft.Web.Http.Versioning;
using Microsoft.Web.OData.Routing;
using Owin;
using Swashbuckle.Application;
using SwashbuckleODataSample;

namespace Swashbuckle.OData.Tests
{
    public static class AppBuilderExtensions
    {
        public static HttpConfiguration GetStandardHttpConfig(this IAppBuilder appBuilder, params Type[] targetControllers)
        {
            return GetStandardHttpConfig(appBuilder, null, null, targetControllers);
        }

        public static HttpConfiguration GetStandardHttpConfig(this IAppBuilder appBuilder, Action<SwaggerDocsConfig> swaggerDocsConfig, Action<ODataSwaggerDocsConfig> odataSwaggerDocsConfig, params Type[] targetControllers)
        {
            var config = new HttpConfiguration
            {
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            };
            return ConfigureHttpConfig(appBuilder, config, swaggerDocsConfig, odataSwaggerDocsConfig, targetControllers);
        }

        /// <summary>
        /// Gets the Http Configuration implementing Microsoft AspNet Versioning library.
        /// </summary>
        /// <param name="appBuilder">this app builder</param>
        /// <param name="versionOption">ApiVersioning options</param>
        /// <param name="targetControllers">controller array of their types</param>
        /// <returns>HttpConfiguation supporting Microsoft AspNet Versioning</returns>
        public static HttpConfiguration GetVersionedHttpConfig
        (
            this IAppBuilder appBuilder,
            ApiVersioningOptions versionOption,
            params Type[] targetControllers
        )
        {
            var config = new HttpConfiguration
            {
                IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always
            };
            return ConfigureVersionedHttpConfig(
                appBuilder, 
                config, 
                versionOption, 
                null, 
                null, 
                targetControllers);
        }

        /// <summary>
        /// Configure the Http configuration for unit testing.
        /// </summary>
        /// <param name="appBuilder">this app builder</param>
        /// <param name="config">the http configuration</param>
        /// <param name="swaggerDocsConfig">swagger doc configuration</param>
        /// <param name="odataSwaggerDocsConfig">odata swagger doc config</param>
        /// <param name="targetControllers">controller array of their types</param>
        /// <returns>unit test configured http config</returns>
        public static HttpConfiguration ConfigureHttpConfig
        (
            this IAppBuilder appBuilder, 
            HttpConfiguration config, 
            Action<SwaggerDocsConfig> swaggerDocsConfig, 
            Action<ODataSwaggerDocsConfig> odataSwaggerDocsConfig, 
            params Type[] targetControllers
        )
        {
            InitHttpConfig(appBuilder,config);
            ConfigureSwagger(config, swaggerDocsConfig, odataSwaggerDocsConfig);

            config.Services.Replace(typeof (IHttpControllerSelector), new UnitTestControllerSelector(config, targetControllers));

            return config;
        }

        /// <summary>
        /// Configure the Http configuration for unit testing with ApiVersioning support.
        /// </summary>
        /// <param name="appBuilder">this app builder</param>
        /// <param name="config">the http configuration</param>
        /// <param name="versionOption">ApiVersioning options</param>
        /// <param name="swaggerDocsConfig">swagger doc configuration</param>
        /// <param name="odataSwaggerDocsConfig">odata swagger doc config</param>
        /// <param name="targetControllers">controller array of their types</param>
        /// <returns>unit test configured http config with ApiVersion support</returns>
        public static HttpConfiguration ConfigureVersionedHttpConfig
        (
            this IAppBuilder appBuilder,
            HttpConfiguration config,
            ApiVersioningOptions versionOption,
            Action<SwaggerDocsConfig> swaggerDocsConfig,
            Action<ODataSwaggerDocsConfig> odataSwaggerDocsConfig,
            params Type[] targetControllers
        )
        {
            InitHttpConfig(appBuilder, config);
            ConfigureMultiVersionSwagger(config, swaggerDocsConfig, odataSwaggerDocsConfig);
            config.AddApiVersioning();

            config.Services.Replace(
                typeof(IHttpControllerSelector),
                new VersionedUnitTestControllerSelector(
                    config, 
                    versionOption, 
                    targetControllers)
            );

            return config;
        }

        /// <summary>
        /// Initialize the HttpServer
        /// </summary>
        /// <param name="appBuilder">this app builder</param>
        /// <param name="config">the http config</param>
        private static void InitHttpConfig
        (
            IAppBuilder appBuilder,
            HttpConfiguration config
        )
        {
            var server = new HttpServer(config);
            appBuilder.UseWebApi(server);

            FormatterConfig.Register(config);
        }

        private static void ConfigureSwagger
        (
            HttpConfiguration config, 
            Action<SwaggerDocsConfig> swaggerDocsConfig, 
            Action<ODataSwaggerDocsConfig> odataSwaggerDocsConfig
        )
        {
            config.EnableSwagger(c =>
            {
                // Use "SingleApiVersion" to describe a single version API. Swagger 2.0 includes an "Info" object to
                // hold additional metadata for an API. Version and title are required but you can also provide
                // additional fields by chaining methods off SingleApiVersion.
                //
                c.SingleApiVersion("v1", "A title for your API");

                // Wrap the default SwaggerGenerator with additional behavior (e.g. caching) or provide an
                // alternative implementation for ISwaggerProvider with the CustomProvider option.
                //
                c.CustomProvider(defaultProvider => new ODataSwaggerProvider(defaultProvider, c, config).Configure(odataSwaggerDocsConfig));

                // Apply test-specific configs
                swaggerDocsConfig?.Invoke(c);
            }).EnableSwaggerUi();
        }

        /// <summary>
        /// Configure Swagger with multi-version for unit testing.
        /// </summary>
        /// <param name="config">the http config</param>
        /// <param name="swaggerDocsConfig">swagger doc configuration</param>
        /// <param name="odataSwaggerDocsConfig">odata swagger doc config</param>
        private static void ConfigureMultiVersionSwagger
        (
            HttpConfiguration config,
            Action<SwaggerDocsConfig> swaggerDocsConfig,
            Action<ODataSwaggerDocsConfig> odataSwaggerDocsConfig
        )
        {
            config.EnableSwagger(c =>
            {
                // If your API has multiple versions, use "MultipleApiVersions" instead of "SingleApiVersion".
                // In this case, you must provide a lambda that tells Swashbuckle which actions should be
                // included in the docs for a given API version. Like "SingleApiVersion", each call to "Version"
                // returns an "Info" builder so you can provide additional metadata per API version.
                //
                c.MultipleApiVersions(
                            ResolveVersionSupportByRouteConstraint,
                            (vc) =>
                            {                              
                                vc.Version("2.0", "A title for your API 2.0");
                                vc.Version("1.0", "A title for your API 1.0");
                            });

                // Wrap the default SwaggerGenerator with additional behavior (e.g. caching) or provide an
                // alternative implementation for ISwaggerProvider with the CustomProvider option.
                //
                c.CustomProvider(defaultProvider => new ODataSwaggerProvider(defaultProvider, c, config).Configure(odataSwaggerDocsConfig));

                // Apply test-specific configs
                swaggerDocsConfig?.Invoke(c);
            }).EnableSwaggerUi();
        }

        /// <summary>
        /// Microsoft AspNet Versioning based swagger version resolver.
        /// </summary>
        /// <param name="apiDesc">the api description</param>
        /// <param name="targetApiVersion">target version of API</param>
        /// <returns>true if the version matches from the api description</returns>
        private static bool ResolveVersionSupportByRouteConstraint
        (
            ApiDescription apiDesc, string targetApiVersion
        )
        {
            var routeConstraints = apiDesc.Route.Constraints;
            // Specific to the use of the MS ASPNet OData Versioning library
            var versionConstraint = (routeConstraints.ContainsKey("apiVersion"))
                        && (routeConstraints.ContainsKey("ODataConstraint"))
                ? routeConstraints["ODataConstraint"]
                    as VersionedODataPathRouteConstraint
                : null;

            var version = default(ApiVersion);
            ApiVersion.TryParse(targetApiVersion, out version);
            var isVersionSupported = (versionConstraint != null)
                                     && (versionConstraint.ApiVersion == version);

            return isVersionSupported;
        }
    }
}
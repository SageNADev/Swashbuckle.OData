using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Owin.Hosting;
using Microsoft.Web.Http;
using Microsoft.Web.Http.Versioning;
using Microsoft.Web.OData.Builder;
using NUnit.Framework;
using Owin;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using SwashbuckleODataSample;

namespace Swashbuckle.OData.Tests
{
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class VersionedODataTests
    {
        [Test]
        public async Task It_supports_versioned_odata_lib()
        {
            using (WebApp.Start(
                    HttpClientUtils.BaseAddress,
                    appBuilder => UrlPathVersionConfiguration(
                        appBuilder,
                        typeof(VersionedProductsController)))
            )
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                var productToUpdate = new ProductVersioned
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Product 1",
                    Price = 2.30
                };
                // Verify that the OData route in the test controller is valid
                var response = await httpClient.PutAsJsonAsync(
                    $"/v1/VersionedProducts('{productToUpdate.Id}')",
                    productToUpdate);
                await response.ValidateSuccessAsync();

                //// Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/1.0");

                //// Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/v'{apiVersion}'/VersionedProducts('{Id}')", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem?.put.Should().NotBeNull();

                await ValidationUtils.ValidateSwaggerJson("swagger/docs/1.0");
            }
        }

        [Test]
        public async Task It_supports_other_versions()
        {
            using (WebApp.Start(
                    HttpClientUtils.BaseAddress,
                    appBuilder => UrlPathVersionConfiguration(
                        appBuilder,
                        typeof(VersionedProductsController)))
            )
            {
                // Arrange
                var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
                var productToUpdate = new ProductVersionedv2()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Product 1 v2",
                    Price = 2.30,
                    Description = "This P 1 product v2."
                };
                // Verify that the OData route in the test controller is valid
                var response = await httpClient.PutAsJsonAsync(
                    $"/v2/VersionedProducts('{productToUpdate.Id}')",
                    productToUpdate);
                await response.ValidateSuccessAsync();

                response = await httpClient.GetAsync("/v2/VersionedProducts");

                await response.ValidateSuccessAsync();

                //// Act
                var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/2.0");

                //// Assert
                PathItem pathItem;
                swaggerDocument.paths.TryGetValue("/v'{apiVersion}'/VersionedProducts('{Id}')", out pathItem);
                pathItem.Should().NotBeNull();
                pathItem?.get.Should().NotBeNull();

                await ValidationUtils.ValidateSwaggerJson("swagger/docs/2.0");
            }
        }

        // TODO: Feature not yet supported.  Requires adding ?api-version=<version> parameter to all http verbs if versioning library in use.
        //[Test]
        //public async Task It_supports_other_versions_using_query_string_param()
        //{
        //    using (WebApp.Start(
        //            HttpClientUtils.BaseAddress,
        //            appBuilder => QueryStringParamVersionConfiguration(
        //                appBuilder,
        //                typeof(VersionedProductsController)))
        //    )
        //    {
        //        // Arrange
        //        var httpClient = HttpClientUtils.GetHttpClient(HttpClientUtils.BaseAddress);
        //        var productToUpdate = new ProductVersionedv2()
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            Name = "Product 1 v2",
        //            Price = 2.30,
        //            Description = "This P 1 product v2."
        //        };
        //        // Verify that the OData route in the test controller is valid
        //        var response = await httpClient.PutAsJsonAsync(
        //            $"/VersionedProducts('{productToUpdate.Id}')?api-version=2.0",
        //            productToUpdate);
        //        await response.ValidateSuccessAsync();

        //        //// Act
        //        var swaggerDocument = await httpClient.GetJsonAsync<SwaggerDocument>("swagger/docs/2.0");

        //        //// Assert
        //        //PathItem pathItem;
        //        //swaggerDocument.paths.TryGetValue("/v'{apiVersion}'/VersionedProducts('{Id}')", out pathItem);
        //        //pathItem.Should().NotBeNull();
        //        //pathItem?.put.Should().NotBeNull();

        //        await ValidationUtils.ValidateSwaggerJson("swagger/docs/2.0");
        //    }
        //}

        private static void UrlPathVersionConfiguration(IAppBuilder appBuilder, Type targetController)
        {
            ApiVersioningOptions versionOptions = new ApiVersioningOptions();
            versionOptions.AssumeDefaultVersionWhenUnspecified = true;
            var config = appBuilder.GetVersionedHttpConfig(
                versionOptions,
                targetController);

            // Define a route to a controller class that contains functions
            var models = GetVersionedModels(config);

            config.MapVersionedODataRoutes("odata-bypath", "v{apiVersion}", models);

            config.EnsureInitialized();
        }

        private static void QueryStringParamVersionConfiguration(IAppBuilder appBuilder, Type targetController)
        {
            var config = InitConfiguration(appBuilder, targetController);

            // Define a route to a controller class that contains functions
            var models = GetVersionedModels(config);

            config.MapVersionedODataRoutes("odata-bypath", null, models);

            config.EnsureInitialized();
        }

        private static HttpConfiguration InitConfiguration(IAppBuilder appBuilder, Type targetController)
        {
            var versionOptions = new ApiVersioningOptions
            {
                AssumeDefaultVersionWhenUnspecified = true
            };
            return appBuilder.GetVersionedHttpConfig(
                versionOptions,
                targetController);
        }

        private static IEnumerable<IEdmModel> GetVersionedModels(HttpConfiguration config)
        {
            var modelBuilder = new VersionedODataModelBuilder(config)
            {
                DefaultModelConfiguration = (builder, apiVersion) =>
                {
                    builder.EntitySet<ProductVersioned>("VersionedProducts");
                },
                ModelConfigurations =
                {
                    new VersionedProductModelConfig()
                }
            };
            return modelBuilder.GetEdmModels();
        }
    }

    #region Models
    public class ProductVersioned
    {
        [Key]
        public string Id { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }
    }

    public class ProductVersionedv2
    {
        [Key]
        public string Id { get; set; }

        public string Name { get; set; }

        public double Price { get; set; }

        public string Description { get; set; }
    }

    public class VersionedProductModelConfig : IModelConfiguration
    {
        public void Apply(ODataModelBuilder builder, ApiVersion apiVersion)
        {
            if (apiVersion.MajorVersion == 2)
            {
                builder.EntitySet<ProductVersionedv2>("VersionedProductsv2");
            }
        }
    }
    #endregion

    #region Controllers
    [ApiVersion("1.0")]
    [ControllerName("VersionedProducts")]
    public class VersionedProductsController : ODataController
    {
        private static readonly ConcurrentDictionary<string, ProductVersioned> Data;

        static VersionedProductsController()
        {
            Data = new ConcurrentDictionary<string, ProductVersioned>();
            var rand = new Random();

            Enumerable.Range(0, 100).Select(i => new ProductVersioned
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Product " + i,
                Price = rand.NextDouble() * 1000
            }).ToList().ForEach(p => Data.TryAdd(p.Id, p));
        }

        public IHttpActionResult Get()
        {
            return Ok(Data.Values.ToList());
        }

        public IHttpActionResult Put
        (
            [FromODataUri] string key, 
            [FromBody] ProductVersioned product
        )
        {
            key.Should().NotStartWith("'");
            key.Should().NotEndWith("'");

            return Updated(Data.Values.First());
        }
    }

    [ApiVersion("2.0")]
    [ControllerName("VersionedProducts")]
    public class VersionedProductsv2Controller : ODataController
    {
        private static readonly ConcurrentDictionary<string, ProductVersionedv2> Data;

        static VersionedProductsv2Controller()
        {
            Data = new ConcurrentDictionary<string, ProductVersionedv2>();
            var rand = new Random();

            Enumerable.Range(0, 100).Select(i => new ProductVersionedv2
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Product " + i,
                Price = rand.NextDouble() * 1000,
                Description = "Description " + i
            }).ToList().ForEach(p => Data.TryAdd(p.Id, p));
        }
        
        public IHttpActionResult Get()
        {
            return Ok(Data.Values.ToList());
        }

        public IHttpActionResult Put
        (
            [FromODataUri] string key,
            [FromBody] ProductVersionedv2 product
        )
        {
            key.Should().NotStartWith("'");
            key.Should().NotEndWith("'");

            return Updated(Data.Values.First());
        }
    }
    #endregion
}
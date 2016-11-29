using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Microsoft.Web.Http.Dispatcher;
using Microsoft.Web.Http.Versioning;

namespace Swashbuckle.OData.Tests
{
    public class UnitTestControllerSelector : DefaultHttpControllerSelector
    {
        private readonly Type[] _targetControllers;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestControllerSelector"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="targetControllers">The controller being targeted in the unit test.</param>
        public UnitTestControllerSelector(HttpConfiguration configuration, Type[] targetControllers) : base(configuration)
        {
            _targetControllers = targetControllers;
        }

        public override IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            if (_targetControllers != null)
            {
                return base.GetControllerMapping()
                    .Where(pair => _targetControllers.Contains(pair.Value.ControllerType))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            return new Dictionary<string, HttpControllerDescriptor>();
        }
    }

    /// <summary>
    /// Controller selector with Microsoft AspNet Versioning support.
    /// </summary>
    public class VersionedUnitTestControllerSelector : ApiVersionControllerSelector
    {
        /// <summary>
        /// Target controller list of array Types
        /// </summary>
        private readonly Type[] _targetControllers;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestControllerSelector"/> class.
        /// Inheriting from ApiVersionControllerSelector.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="options">ApiVersioning options</param>
        /// <param name="targetControllers">The controller being targeted in the unit test.</param>
        public VersionedUnitTestControllerSelector
        (
            HttpConfiguration configuration,
            ApiVersioningOptions options,
            Type[] targetControllers

        ) : base(configuration, options)
        {
            _targetControllers = targetControllers;
        }

        /// <summary>
        /// Controller mapping override to only get the target controller for the unit test.
        /// </summary>
        /// <returns>the unit test's target controller</returns>
        public override IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            if (_targetControllers != null)
            {
                return base.GetControllerMapping()
                    .Where(pair => _targetControllers.Contains(pair.Value.ControllerType))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            return new Dictionary<string, HttpControllerDescriptor>();
        }
    }
}
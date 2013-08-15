﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Collections.Generic;
    using System.Data.Entity.TestHelpers;
    using System.Linq;
    using System.Reflection;
    using Xunit;
    using Xunit.Sdk;

    /// <summary>
    ///     Attribute that is applied to a method to indicate that it is a fact that
    ///     should be run by the test runner in partial trust.
    /// </summary>
    /// <remarks>
    ///     The class containing the method must derive from <see cref="MarshalByRefObject" />.
    /// 
    ///     If the test class is decorated using <see cref="IUseFixture{T}" />, the fixture data is
    ///                                              instantiated in the main
    ///                                              <see cref="AppDomain" />
    ///                                              and then serialized to the partial trust
    ///                                              sandbox.
    /// </remarks>
    public class PartialTrustFactAttribute : ExtendedFactAttribute
    {
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            var fixtures = GetFixtures(method.Class);

            return base.EnumerateTestCommands(method).Select(tc => new PartialTrustCommand(tc, fixtures));
        }

        private static IDictionary<MethodInfo, object> GetFixtures(ITypeInfo typeUnderTest)
        {
            var fixtures = new Dictionary<MethodInfo, object>();

            foreach (var @interface in typeUnderTest.Type.GetInterfaces())
            {
                if (@interface.IsGenericType)
                {
                    var genericDefinition = @interface.GetGenericTypeDefinition();

                    if (genericDefinition == typeof(IUseFixture<>))
                    {
                        var dataType = @interface.GetGenericArguments()[0];
                        var fixtureData = Activator.CreateInstance(dataType);
                        var method = @interface.GetMethod("SetFixture", new[] { dataType });

                        fixtures[method] = fixtureData;
                    }
                }
            }

            return fixtures;
        }
    }
}

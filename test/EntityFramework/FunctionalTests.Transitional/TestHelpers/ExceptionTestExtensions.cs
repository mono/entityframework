﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public static class ExceptionTestExtensions
    {
        public static void ValidateMessage(
            this Exception exception,
            string expectedResourceKey,
            params object[] parameters)
        {
            Debug.Assert(exception != null);
            Debug.Assert(expectedResourceKey != null);

            ValidateMessage(exception, TestBase.EntityFrameworkAssembly, expectedResourceKey, null, parameters);
        }

        public static void ValidateMessage(
            this Exception exception,
            Assembly resourceAssembly,
            string expectedResourceKey,
            string resourceTable,
            params object[] parameters)
        {
            ValidateMessage(exception, resourceAssembly, expectedResourceKey, null, s => s, parameters);
        }

        public static void ValidateMessage(
            this Exception exception,
            Assembly resourceAssembly,
            string expectedResourceKey,
            string resourceTable,
            Func<string, string> messageModificationFunction,
            params object[] parameters)
        {
            Debug.Assert(exception != null);
            Debug.Assert(resourceAssembly != null);
            Debug.Assert(expectedResourceKey != null);

            if (resourceTable == null
                && resourceAssembly == TestBase.EntityFrameworkAssembly)
            {
                resourceTable = "System.Data.Entity.Properties.Resources";
            }

            var actualMessage = messageModificationFunction(exception.Message);
            var argException = exception as ArgumentException;
            if (argException != null)
            {
                var paramPartIndex = argException.Message.LastIndexOf("\r\n");
                if (paramPartIndex != -1)
                {
                    actualMessage = argException.Message.Substring(0, paramPartIndex);
                    Assert.True(parameters.Length >= 1, "Expected first parameter to be param for ArgumentException.");
                    Assert.Equal(parameters[0], argException.ParamName);
                    parameters = parameters.Skip(1).ToArray();
                }
            }
            var assemblyResourceLookup
                = resourceTable == null
                      ? new AssemblyResourceLookup(resourceAssembly)
                      : new AssemblyResourceLookup(resourceAssembly, resourceTable);

            new StringResourceVerifier(assemblyResourceLookup)
                .VerifyMatch(expectedResourceKey, actualMessage, parameters);
        }
    }
}
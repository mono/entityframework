﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Config
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Resources;
    using System.Linq;
    using Moq;
    using Xunit;

    public class DefaultExecutionStrategyResolverTests : TestBase
    {
        [Fact]
        public void GetService_returns_null_when_contract_interface_does_not_match()
        {
            Assert.Null(new DefaultExecutionStrategyResolver().GetService<IQueryable>());
        }

        [Fact]
        public void GetService_returns_execution_strategy()
        {
            Assert.IsType<NonRetryingExecutionStrategy>(
                new DefaultExecutionStrategyResolver().GetService<Func<IExecutionStrategy>>(new ExecutionStrategyKey("FooClient", "foo"))());
        }

        [Fact]
        public void GetService_throws_for_null_key()
        {
            Assert.Equal(
                "key",
                Assert.Throws<ArgumentNullException>(() => new DefaultExecutionStrategyResolver().GetService<Func<IExecutionStrategy>>(null)).ParamName);
        }

        [Fact]
        public void GetService_throws_for_wrong_key_type()
        {
            Assert.Equal(
                Strings.DbDependencyResolver_InvalidKey(typeof(ExecutionStrategyKey).Name, "Func<IExecutionStrategy>"),
                Assert.Throws<ArgumentException>(
                    () => new DefaultExecutionStrategyResolver().GetService<Func<IExecutionStrategy>>("a")).Message);
        }
    }
}

﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Linq;
    using Xunit;

    /// <summary>
    ///     General unit tests for concurrency exceptions.  Note that most of
    ///     the actual functionality is contained in core EF and is tested through
    ///     functional tests.
    /// </summary>
    public class DbUpdateConcurrencyExceptionTests : TestBase
    {
        #region Tests for FxCop-required constructors

        [Fact]
        public void DbUpdateConcurrencyException_exposes_public_empty_constructor()
        {
            new DbUpdateConcurrencyException();
        }

        [Fact]
        public void DbUpdateConcurrencyException_exposes_public_string_constructor()
        {
            var ex = new DbUpdateConcurrencyException("Foo");

            Assert.Equal("Foo", ex.Message);
        }

        [Fact]
        public void DbUpdateConcurrencyException_exposes_public_string_and_inner_exception_constructor()
        {
            var inner = new Exception();

            var ex = new DbUpdateConcurrencyException("Foo", inner);

            Assert.Equal("Foo", ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        #endregion

        [Fact]
        public void DbUpdateConcurrencyException_is_marked_as_Serializable()
        {
            Assert.True(typeof(DbUpdateConcurrencyException).GetCustomAttributes(typeof(SerializableAttribute), inherit: false).Any());
        }
    }
}

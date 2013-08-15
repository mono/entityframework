// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer
{
    using System.Data.Entity.SqlServer.Resources;
    using Moq;
    using Xunit;

    public class SqlTypesAssemblyLoaderTests
    {
        [Fact]
        public void TryGetSqlTypesAssembly_on_dev_machine_returns_assembly_for_SQL_2008_native_types()
        {
            Assert.Equal(
                "Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91",
                new SqlTypesAssemblyLoader().TryGetSqlTypesAssembly().SqlGeographyType.AssemblyQualifiedName);
        }

        [Fact]
        public void TryGetSqlTypesAssembly_returns_null_if_native_types_are_not_available()
        {
            Assert.Null(new SqlTypesAssemblyLoader(new[] { "SomeMissingAssembly" }).TryGetSqlTypesAssembly());
        }

        [Fact]
        public void GetSqlTypesAssembly_on_dev_machine_returns_assembly_for_SQL_2008_native_types()
        {
            Assert.Equal(
                "Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91",
                new SqlTypesAssemblyLoader().GetSqlTypesAssembly().SqlGeographyType.AssemblyQualifiedName);
        }

        [Fact]
        public void GetSqlTypesAssembly_throws_if_native_types_are_not_available()
        {
            Assert.Equal(
                Strings.SqlProvider_SqlTypesAssemblyNotFound,
                Assert.Throws<InvalidOperationException>(
                    () => new SqlTypesAssemblyLoader(new[] { "SomeMissingAssembly" }).GetSqlTypesAssembly()).Message);
        }

        [Fact]
        public void SqlTypes_returns_the_assembly_set_in_the_constructor()
        {
            var sqlTypesAssembly = new Mock<SqlTypesAssembly>().Object;

            Assert.Same(sqlTypesAssembly, new SqlTypesAssemblyLoader(sqlTypesAssembly).TryGetSqlTypesAssembly());
        }
    }
}

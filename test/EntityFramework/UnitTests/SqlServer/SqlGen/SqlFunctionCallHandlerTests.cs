﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServer.SqlGen
{
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.SqlServer.Resources;
    using System.Linq;
    using System.Text;
    using Moq;
    using Xunit;

    public class SqlFunctionCallHandlerTests
    {
        public class CastReturnTypeToInt16
        {
            [Fact]
            public void CastReturnTypeToInt16_returns_false_when_function_is_not_in_list_that_needs_this_cast()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToInt16(
                        CreateMockDbFunctionExpression("Edm.NotOnYourNelly", PrimitiveTypeKind.Int16).Object));
            }

            [Fact]
            public void CastReturnTypeToInt16_returns_true_when_function_is_in_list_to_need_cast()
            {
                Assert.True(
                    SqlFunctionCallHandler.CastReturnTypeToInt16(CreateMockDbFunctionExpression("Edm.Abs", PrimitiveTypeKind.Int16).Object));
            }

            [Fact]
            public void CastReturnTypeToInt16_returns_false_when_function_is_in_list_to_need_cast_but_return_type_does_not_match()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToInt16(CreateMockDbFunctionExpression("Edm.Abs", PrimitiveTypeKind.Int32).Object));
            }
        }

        public class CastReturnTypeToSingle
        {
            [Fact]
            public void CastReturnTypeToSingle_returns_false_when_function_is_not_in_list_that_needs_this_cast()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToSingle(
                        CreateMockDbFunctionExpression("Edm.NotOnYourNelly", PrimitiveTypeKind.Single).Object));
            }

            [Fact]
            public void CastReturnTypeToSingle_returns_true_when_function_is_in_list_to_need_cast()
            {
                Assert.True(
                    SqlFunctionCallHandler.CastReturnTypeToSingle(
                        CreateMockDbFunctionExpression("Edm.Floor", PrimitiveTypeKind.Single).Object));
            }

            [Fact]
            public void CastReturnTypeToSingle_returns_false_when_function_is_in_list_to_need_cast_but_return_type_does_not_match()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToSingle(
                        CreateMockDbFunctionExpression("Edm.Floor", PrimitiveTypeKind.Double).Object));
            }
        }

        public class CastReturnTypeToInt64
        {
            [Fact]
            public void CastReturnTypeToInt64_returns_false_when_function_is_not_in_list_that_needs_this_cast()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToInt64(
                        CreateMockDbFunctionExpression("Edm.NotOnYourNelly", PrimitiveTypeKind.Int64).Object));
            }

            [Fact]
            public void CastReturnTypeToInt64_returns_true_when_function_is_in_list_to_need_cast()
            {
                Assert.True(
                    SqlFunctionCallHandler.CastReturnTypeToInt64(
                        CreateMockDbFunctionExpression("SqlServer.CHARINDEX", PrimitiveTypeKind.Int64).Object));
            }

            [Fact]
            public void CastReturnTypeToInt64_returns_false_when_function_is_in_list_to_need_cast_but_return_type_does_not_match()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToInt64(
                        CreateMockDbFunctionExpression("SqlServer.CHARINDEX", PrimitiveTypeKind.Int32).Object));
            }
        }

        public class CastReturnTypeToInt32
        {
            [Fact]
            public void CastReturnTypeToInt32_returns_false_when_function_is_not_in_list_that_needs_this_cast()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToInt32(
                        new SqlGenerator(), CreateMockDbFunctionExpression("Edm.NotOnYourNelly", PrimitiveTypeKind.Int32).Object));
            }

            [Fact]
            public void CastReturnTypeToInt32_returns_true_when_function_is_in_list_to_need_cast()
            {
                Assert.True(
                    SqlFunctionCallHandler.CastReturnTypeToInt32(
                        CreateMockSqlGenerator("ntext").Object,
                        CreateMockDbFunctionExpression("SqlServer.PATINDEX", PrimitiveTypeKind.Int32).Object));
            }

            [Fact]
            public void CastReturnTypeToInt32_returns_false_when_function_is_in_list_to_need_cast_but_return_type_does_not_match()
            {
                Assert.False(
                    SqlFunctionCallHandler.CastReturnTypeToInt32(
                        CreateMockSqlGenerator("nope").Object,
                        CreateMockDbFunctionExpression("SqlServer.PATINDEX", PrimitiveTypeKind.Int32).Object));
            }
        }

        public class HandleDatepartDateFunction
        {
            [Fact]
            public void HandleDatepartDateFunction_throws_when_argument_is_not_a_constant()
            {
                var mockExpression = new Mock<DbFunctionExpression>();
                mockExpression.Setup(m => m.Function).Returns(CreateMockEdmFunction("My.Function").Object);
                mockExpression.Setup(m => m.Arguments).Returns(new[] { new Mock<DbExpression>().Object });

                Assert.Equal(
                    Strings.SqlGen_InvalidDatePartArgumentExpression("My", "Function"),
                    Assert.Throws<InvalidOperationException>(
                        () => SqlFunctionCallHandler.HandleDatepartDateFunction(null, mockExpression.Object)).Message);
            }

            [Fact]
            public void HandleDatepartDateFunction_throws_when_argument_is_not_a_string()
            {
                Assert.Equal(
                    Strings.SqlGen_InvalidDatePartArgumentExpression("My", "Function"),
                    Assert.Throws<InvalidOperationException>(
                        () =>
                        SqlFunctionCallHandler.HandleDatepartDateFunction(
                            null, CreateMockDbFunctionExpression("My.Function", PrimitiveTypeKind.String, 69).Object)).Message);
            }

            [Fact]
            public void HandleDatepartDateFunction_throws_when_argument_does_not_contain_a_valid_date_part()
            {
                Assert.Equal(
                    Strings.SqlGen_InvalidDatePartArgumentValue("mp4-27", "My", "Function"),
                    Assert.Throws<InvalidOperationException>(
                        () =>
                        SqlFunctionCallHandler.HandleDatepartDateFunction(
                            null, CreateMockDbFunctionExpression("My.Function", PrimitiveTypeKind.String, "mp4-27").Object)).Message);
            }

            [Fact]
            public void HandleDatepartDateFunction_builds_SQL_for_the_given_date_part()
            {
                var builder = new StringBuilder();
                using (var writer = new SqlWriter(builder))
                {
                    SqlFunctionCallHandler.HandleDatepartDateFunction(
                        null, CreateMockDbFunctionExpression("My.Function", PrimitiveTypeKind.String, "iso_week").Object)
                        .WriteSql(writer, null);

                    Assert.Equal("[My].[Function](iso_week)", builder.ToString());
                }
            }

            [Fact]
            public void HandleDatepartDateFunction_builds_SQL_for_the_given_date_part_with_arguments()
            {
                var builder = new StringBuilder();
                using (var writer = new SqlWriter(builder))
                {
                    SqlFunctionCallHandler.HandleDatepartDateFunction(
                        null, CreateMockDbFunctionExpression("My.Function", PrimitiveTypeKind.String, "iso_week", "One", "Two").Object)
                        .WriteSql(writer, null);

                    Assert.Equal("[My].[Function](iso_week, One, Two)", builder.ToString());
                }
            }
        }

        private static Mock<SqlGenerator> CreateMockSqlGenerator(string storeType)
        {
            var mockEdmType = new Mock<EdmType>();
            mockEdmType.Setup(m => m.Name).Returns(storeType);

            var mockStoreType = new Mock<TypeUsage>();
            mockStoreType.Setup(m => m.EdmType).Returns(mockEdmType.Object);

            var mockStoreProviderManifest = new Mock<DbProviderManifest>();
            mockStoreProviderManifest.Setup(m => m.GetStoreType(It.IsAny<TypeUsage>())).Returns(mockStoreType.Object);

            var mockStoreItemCollection = new Mock<StoreItemCollection>();
            mockStoreItemCollection.Setup(m => m.StoreProviderManifest).Returns(mockStoreProviderManifest.Object);

            var mockSqlGenerator = new Mock<SqlGenerator>();
            mockSqlGenerator.Setup(m => m.StoreItemCollection).Returns(mockStoreItemCollection.Object);

            return mockSqlGenerator;
        }

        private static Mock<DbFunctionExpression> CreateMockDbFunctionExpression(
            string functionName,
            PrimitiveTypeKind returnType,
            object argumentValue = null,
            params string[] additionalArguments)
        {
            additionalArguments = additionalArguments ?? new string[0];

            var mockEdmType = new Mock<PrimitiveType>();
            mockEdmType.Setup(m => m.BuiltInTypeKind).Returns(BuiltInTypeKind.PrimitiveType);
            mockEdmType.Setup(m => m.PrimitiveTypeKind).Returns(returnType);

            var mockTypeUsage = new Mock<TypeUsage>();
            mockTypeUsage.Setup(m => m.EdmType).Returns(mockEdmType.Object);

            var mockArgument = new Mock<DbConstantExpression>();
            mockArgument.Setup(m => m.ResultType).Returns(mockTypeUsage.Object);
            mockArgument.Setup(m => m.Value).Returns(argumentValue);

            var arguments = new DbExpression[] { mockArgument.Object }.Concat(
                additionalArguments.Select(
                    a =>
                        {
                            var mockSqlWriter = new Mock<ISqlFragment>();
                            mockSqlWriter.Setup(m => m.WriteSql(It.IsAny<SqlWriter>(), It.IsAny<SqlGenerator>()))
                                .Callback((SqlWriter writer, SqlGenerator generator) => writer.Write(a));

                            var mockAdditionalArgument = new Mock<DbConstantExpression>();
                            mockAdditionalArgument.Setup(m => m.ResultType).Returns(mockTypeUsage.Object);
                            mockAdditionalArgument.Setup(m => m.Value).Returns("Value");
                            mockAdditionalArgument.Setup(m => m.Accept(It.IsAny<DbExpressionVisitor<ISqlFragment>>()))
                                .Returns(mockSqlWriter.Object);

                            return mockAdditionalArgument.Object;
                        }));

            var mockExpression = new Mock<DbFunctionExpression>();
            mockExpression.Setup(m => m.Function).Returns(CreateMockEdmFunction(functionName).Object);
            mockExpression.Setup(m => m.Arguments).Returns(arguments.ToArray());

            return mockExpression;
        }

        private static Mock<EdmFunction> CreateMockEdmFunction(string functionName)
        {
            var mockProperty = new Mock<MetadataProperty>();
            mockProperty.Setup(m => m.Name).Returns("DataSpace");
            mockProperty.Setup(m => m.Value).Returns(DataSpace.CSpace);

            var mockEdmFunction = new Mock<EdmFunction>("F", "N", DataSpace.SSpace);
            mockEdmFunction.Setup(m => m.FullName).Returns(functionName);
            mockEdmFunction.Setup(m => m.NamespaceName).Returns(functionName.Split('.')[0]);
            mockEdmFunction.Setup(m => m.Name).Returns(functionName.Split('.')[1]);
            mockEdmFunction.Setup(m => m.DataSpace).Returns(DataSpace.CSpace);
            mockEdmFunction.Setup(m => m.MetadataProperties).Returns(
                new ReadOnlyMetadataCollection<MetadataProperty>(new[] { mockProperty.Object }));

            return mockEdmFunction;
        }
    }
}

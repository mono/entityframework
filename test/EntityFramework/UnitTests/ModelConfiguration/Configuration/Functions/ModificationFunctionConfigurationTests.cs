﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Functions
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.ModelConfiguration.Utilities;
    using System.Data.Entity.Resources;
    using System.Linq;
    using Xunit;

    public class ModificationFunctionConfigurationTests
    {
        [Fact]
        public void Can_clone_configuration()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            modificationFunctionConfiguration.HasName("Foo", "baz");

            var mockPropertyInfo = new MockPropertyInfo();

            modificationFunctionConfiguration.Parameter(new PropertyPath(mockPropertyInfo), "baz");
            modificationFunctionConfiguration.Result(new PropertyPath(mockPropertyInfo), "foo");
            modificationFunctionConfiguration.RowsAffectedParameter("bar");

            var clone = modificationFunctionConfiguration.Clone();

            Assert.NotSame(modificationFunctionConfiguration, clone);
            Assert.Equal("Foo", clone.Name);
            Assert.Equal("baz", clone.Schema);
            Assert.Equal(1, clone.ParameterNames.Count);
            Assert.Equal(1, clone.ResultBindings.Count);
            Assert.Equal("bar", clone.RowsAffectedParameterName);
        }

        [Fact]
        public void Can_set_function_name()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            modificationFunctionConfiguration.HasName("Foo");

            Assert.Equal("Foo", modificationFunctionConfiguration.Name);
        }

        [Fact]
        public void Can_set_rows_affected_parameter_name()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            modificationFunctionConfiguration.RowsAffectedParameter("Foo");

            Assert.Equal("Foo", modificationFunctionConfiguration.RowsAffectedParameterName);
        }

        [Fact]
        public void Can_add_parameter_configuration()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            Assert.Empty(modificationFunctionConfiguration.ParameterNames);

            var mockPropertyInfo = new MockPropertyInfo();

            modificationFunctionConfiguration.Parameter(new PropertyPath(mockPropertyInfo), "baz");

            Assert.Equal("baz", modificationFunctionConfiguration.ParameterNames.Single().Value.Item1);
        }

        [Fact]
        public void Can_set_column_name_for_result_binding()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo = new MockPropertyInfo();

            modificationFunctionConfiguration.Result(new PropertyPath(mockPropertyInfo), "foo");

            Assert.Same("foo", modificationFunctionConfiguration.ResultBindings.Single().Value);
        }

        [Fact]
        public void Can_configure_function_name_and_parameters()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            modificationFunctionConfiguration.HasName("Foo", "Bar");

            var mockPropertyInfo1 = new MockPropertyInfo();
            var mockPropertyInfo2 = new MockPropertyInfo();

            modificationFunctionConfiguration
                .Parameter(new PropertyPath(mockPropertyInfo1), "Foo");

            modificationFunctionConfiguration
                .Parameter(new PropertyPath(new[] { mockPropertyInfo1.Object, mockPropertyInfo2.Object }), "Bar");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            var property1 = new EdmProperty("P1");
            property1.SetClrPropertyInfo(mockPropertyInfo1);

            var property2 = new EdmProperty("P1");
            property2.SetClrPropertyInfo(mockPropertyInfo2);

            var functionParameter1
                = new FunctionParameter(
                    "P1",
                    TypeUsage.Create(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    ParameterMode.In);

            var functionParameter2
                = new FunctionParameter(
                    "P2",
                    TypeUsage.Create(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    ParameterMode.In);

            var functionParameter3
                = new FunctionParameter(
                    "Foo",
                    TypeUsage.Create(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    ParameterMode.In);

            var function
                = new EdmFunction(
                    "F", "N", DataSpace.SSpace,
                    new EdmFunctionPayload
                        {
                            Parameters = new[] { functionParameter1, functionParameter2, functionParameter3 }
                        });

            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);

            var associationEndMember1
                = new AssociationEndMember("AE1", new EntityType("E", "N", DataSpace.CSpace))
                      {
                          RelationshipMultiplicity = RelationshipMultiplicity.Many
                      };

            var associationEndMember2
                = new AssociationEndMember("AE2", new EntityType("E", "N", DataSpace.CSpace))
                      {
                          RelationshipMultiplicity = RelationshipMultiplicity.Many
                      };

            associationType.SourceEnd = associationEndMember1;
            associationType.TargetEnd = associationEndMember2;

            var associationSet = new AssociationSet("AS", associationType);

            associationSet.AddAssociationSetEnd(
                new AssociationSetEnd(entitySet, associationSet, associationEndMember2));

            modificationFunctionConfiguration.Configure(
                new StorageModificationFunctionMapping(
                    entitySet,
                    new EntityType("E", "N", DataSpace.CSpace),
                    function,
                    new[]
                        {
                            new StorageModificationFunctionParameterBinding(
                                functionParameter1,
                                new StorageModificationFunctionMemberPath(
                                new EdmMember[] { property1, associationEndMember2 },
                                associationSet),
                                false),
                            new StorageModificationFunctionParameterBinding(
                                functionParameter2,
                                new StorageModificationFunctionMemberPath(
                                new[] { property1, property2 },
                                null),
                                false)
                        },
                    null,
                    null));

            Assert.Equal("Foo", function.Name);
            Assert.Equal("Bar", function.Schema);
            Assert.Equal("Foo", functionParameter1.Name);
            Assert.Equal("Bar", functionParameter2.Name);
            Assert.Equal("Foo1", functionParameter3.Name);
        }

        [Fact]
        public void Can_configure_ia_fk_parameters()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo1 = new MockPropertyInfo();
            var mockPropertyInfo2 = new MockPropertyInfo();

            modificationFunctionConfiguration
                .Parameter(new PropertyPath(new[] { mockPropertyInfo1.Object, mockPropertyInfo2.Object }), "Foo");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            var property1 = new EdmProperty("P1");
            property1.SetClrPropertyInfo(mockPropertyInfo1);

            var function = new EdmFunction("F", "N", DataSpace.SSpace);
            var functionParameter1 = new FunctionParameter();

            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);

            var associationEndMember1
                = new AssociationEndMember("AE1", new EntityType("E", "N", DataSpace.CSpace))
                      {
                          RelationshipMultiplicity = RelationshipMultiplicity.Many
                      };

            var associationEndMember2
                = new AssociationEndMember("AE2", new EntityType("E", "N", DataSpace.CSpace))
                      {
                          RelationshipMultiplicity = RelationshipMultiplicity.One
                      };
            associationEndMember2.SetClrPropertyInfo(mockPropertyInfo1);

            associationType.SourceEnd = associationEndMember1;
            associationType.TargetEnd = associationEndMember2;

            var associationSet = new AssociationSet("AS", associationType);

            associationSet.AddAssociationSetEnd(
                new AssociationSetEnd(entitySet, associationSet, associationEndMember2));

            modificationFunctionConfiguration.Configure(
                new StorageModificationFunctionMapping(
                    entitySet,
                    new EntityType("E", "N", DataSpace.CSpace),
                    function,
                    new[]
                        {
                            new StorageModificationFunctionParameterBinding(
                                functionParameter1,
                                new StorageModificationFunctionMemberPath(
                                new EdmMember[] { property1, associationEndMember2 },
                                associationSet),
                                false)
                        },
                    null,
                    null));

            Assert.Equal("Foo", functionParameter1.Name);
        }

        [Fact]
        public void Configure_should_throw_when_parameter_binding_not_found()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo1 = new MockPropertyInfo();

            modificationFunctionConfiguration
                .Parameter(new PropertyPath(mockPropertyInfo1), "P1");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            Assert.Equal(
                Strings.ModificationFunctionParameterNotFound("P", "F"),
                Assert.Throws<InvalidOperationException>(
                    () => modificationFunctionConfiguration.Configure(
                        new StorageModificationFunctionMapping(
                              entitySet,
                              new EntityType("E", "N", DataSpace.CSpace),
                              new EdmFunction("F", "N", DataSpace.SSpace),
                              new StorageModificationFunctionParameterBinding[0],
                              null,
                              null))).Message);
        }

        [Fact]
        public void Configure_should_throw_when_original_value_parameter_binding_not_found()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo = new MockPropertyInfo();

            modificationFunctionConfiguration
                .Parameter(new PropertyPath(mockPropertyInfo), "P0", "P1");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            var property = new EdmProperty("P0");
            property.SetClrPropertyInfo(mockPropertyInfo);

            Assert.Equal(
                Strings.ModificationFunctionParameterNotFoundOriginal("P", "F"),
                Assert.Throws<InvalidOperationException>(
                    () => modificationFunctionConfiguration.Configure(
                        new StorageModificationFunctionMapping(
                              entitySet,
                              new EntityType("E", "N", DataSpace.CSpace),
                              new EdmFunction("F", "N", DataSpace.SSpace),
                              new[]
                                  {
                                      new StorageModificationFunctionParameterBinding(
                                          new FunctionParameter(),
                                          new StorageModificationFunctionMemberPath(
                                          new[] { property },
                                          null),
                                          true)
                                  },
                              null,
                              null))).Message);
        }

        [Fact]
        public void Configure_should_throw_when_result_binding_not_found()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo1 = new MockPropertyInfo();

            modificationFunctionConfiguration
                .Result(new PropertyPath(mockPropertyInfo1), "Foo");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            Assert.Equal(
                Strings.ResultBindingNotFound("P", "F"),
                Assert.Throws<InvalidOperationException>(
                    () => modificationFunctionConfiguration.Configure(
                        new StorageModificationFunctionMapping(
                              entitySet,
                              new EntityType("E", "N", DataSpace.CSpace),
                              new EdmFunction("F", "N", DataSpace.SSpace),
                              new StorageModificationFunctionParameterBinding[0],
                              null,
                              null))).Message);
        }

        [Fact]
        public void Can_configure_result_bindings()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo = new MockPropertyInfo();

            modificationFunctionConfiguration
                .Result(new PropertyPath(mockPropertyInfo), "Foo");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            var property = new EdmProperty("P1");
            property.SetClrPropertyInfo(mockPropertyInfo);

            var resultBinding = new StorageModificationFunctionResultBinding("Bar", property);

            modificationFunctionConfiguration.Configure(
                new StorageModificationFunctionMapping(
                    entitySet,
                    new EntityType("E", "N", DataSpace.CSpace),
                    new EdmFunction("F", "N", DataSpace.SSpace),
                    new[]
                        {
                            new StorageModificationFunctionParameterBinding(
                                new FunctionParameter(
                                "P",
                                TypeUsage.Create(
                                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                                ParameterMode.In),
                                new StorageModificationFunctionMemberPath(new[] { property }, null), false)
                        },
                    null,
                    new[] { resultBinding }));

            Assert.Equal("Foo", resultBinding.ColumnName);
        }

        [Fact]
        public void Can_configure_rows_affected_parameter_name()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            var mockPropertyInfo = new MockPropertyInfo();

            modificationFunctionConfiguration.RowsAffectedParameter("Foo");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            var property = new EdmProperty("P1");
            property.SetClrPropertyInfo(mockPropertyInfo);

            var rowsAffectedParameter = new FunctionParameter();

            modificationFunctionConfiguration.Configure(
                new StorageModificationFunctionMapping(
                    entitySet,
                    new EntityType("E", "N", DataSpace.CSpace),
                    new EdmFunction("F", "N", DataSpace.SSpace),
                    new[]
                        {
                            new StorageModificationFunctionParameterBinding(
                                new FunctionParameter(
                                "P",
                                TypeUsage.Create(
                                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                                ParameterMode.In),
                                new StorageModificationFunctionMemberPath(new[] { property }, null), false)
                        },
                    rowsAffectedParameter,
                    null));

            Assert.Equal("Foo", rowsAffectedParameter.Name);
        }

        [Fact]
        public void Configure_should_throw_when_rows_affected_parameter_not_found()
        {
            var modificationFunctionConfiguration = new ModificationFunctionConfiguration();

            modificationFunctionConfiguration.RowsAffectedParameter("boom");

            var entitySet = new EntitySet();
            entitySet.ChangeEntityContainerWithoutCollectionFixup(new EntityContainer("C", DataSpace.CSpace));

            Assert.Equal(
                Strings.NoRowsAffectedParameter("F"),
                Assert.Throws<InvalidOperationException>(
                    () => modificationFunctionConfiguration.Configure(
                        new StorageModificationFunctionMapping(
                              entitySet,
                              new EntityType("E", "N", DataSpace.CSpace),
                              new EdmFunction("F", "N", DataSpace.SSpace),
                              new StorageModificationFunctionParameterBinding[0],
                              null,
                              null))).Message);
        }

        [Fact]
        public void IsCompatibleWith_should_return_true_when_name_and_parameters_compatible()
        {
            var modificationFunctionConfiguration1 = new ModificationFunctionConfiguration();
            var modificationFunctionConfiguration2 = new ModificationFunctionConfiguration();

            Assert.True(modificationFunctionConfiguration1.IsCompatibleWith(modificationFunctionConfiguration2));

            modificationFunctionConfiguration1.HasName("P", "S");

            Assert.True(modificationFunctionConfiguration1.IsCompatibleWith(modificationFunctionConfiguration2));

            modificationFunctionConfiguration2.HasName("P", "S");

            Assert.True(modificationFunctionConfiguration1.IsCompatibleWith(modificationFunctionConfiguration2));

            var mockPropertyInfo1 = new MockPropertyInfo(typeof(int), "I");
            var mockPropertyInfo2 = new MockPropertyInfo(typeof(string), "S");
            var mockPropertyInfo3 = new MockPropertyInfo(typeof(bool), "B");

            modificationFunctionConfiguration1.Parameter(new PropertyPath(mockPropertyInfo1), "baz");
            modificationFunctionConfiguration1.Parameter(new PropertyPath(mockPropertyInfo2), "baz");
            modificationFunctionConfiguration1.Parameter(new PropertyPath(mockPropertyInfo3), "baz");

            Assert.True(modificationFunctionConfiguration1.IsCompatibleWith(modificationFunctionConfiguration2));

            modificationFunctionConfiguration2.Parameter(new PropertyPath(mockPropertyInfo3), "baz");
            modificationFunctionConfiguration2.Parameter(new PropertyPath(mockPropertyInfo2), "baz");
            modificationFunctionConfiguration2.Parameter(new PropertyPath(mockPropertyInfo1), "baz");

            Assert.True(modificationFunctionConfiguration1.IsCompatibleWith(modificationFunctionConfiguration2));
        }
    }
}

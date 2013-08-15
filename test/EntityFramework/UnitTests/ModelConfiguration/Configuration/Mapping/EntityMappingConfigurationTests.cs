// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Mapping
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Edm;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using Xunit;

    public sealed class EntityMappingConfigurationTests
    {
        [Fact]
        public void TableName_can_get_and_set()
        {
            var entityMappingConfiguration = new EntityMappingConfiguration
                                                 {
                                                     TableName = new DatabaseName("Foo")
                                                 };

            Assert.Equal("Foo", entityMappingConfiguration.TableName.Name);
        }

        [Fact]
        public void Configure_should_update_table_name_when_base_type_is_null()
        {
            var entityMappingConfiguration
                = new EntityMappingConfiguration
                      {
                          TableName = new DatabaseName("Foo")
                      };

            var entityTypeMapping = new StorageEntityTypeMapping(null);

            entityTypeMapping.AddType(new EntityType("E", "N", DataSpace.CSpace));

            var databaseMapping =
                new DbDatabaseMapping().Initialize(new EdmModel(DataSpace.CSpace), new EdmModel(DataSpace.SSpace));

            var table = databaseMapping.Database.AddTable("foo");
            var entitySet = databaseMapping.Database.GetEntitySet(table);

            entityTypeMapping.AddFragment(new StorageMappingFragment(entitySet, entityTypeMapping, false));
            
            entityMappingConfiguration.Configure(
                databaseMapping, ProviderRegistry.Sql2008_ProviderManifest, entityTypeMapping.EntityType, ref entityTypeMapping, false, 0, 1);

            Assert.Equal("Foo", table.GetTableName().Name);
        }

        [Fact]
        public void AddValueCondition_overwrites_existing_value_for_a_discriminator()
        {
            var entityMappingConfiguration1 = new EntityMappingConfiguration();
            entityMappingConfiguration1
                .AddValueCondition(
                    new ValueConditionConfiguration(entityMappingConfiguration1, "disc")
                        {
                            Value = 1
                        });
            entityMappingConfiguration1
                .AddValueCondition(
                    new ValueConditionConfiguration(entityMappingConfiguration1, "disc")
                        {
                            Value = 2
                        });

            Assert.Equal(2, entityMappingConfiguration1.ValueConditions.First().Value);
        }
    }
}

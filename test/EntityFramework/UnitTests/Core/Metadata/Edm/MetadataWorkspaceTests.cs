﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Config;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Mapping.ViewGeneration;
    using System.Data.Entity.Core.Objects.DataClasses;
    using System.Data.Entity.ViewGeneration;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using Moq;
    using Xunit;

    public class MetadataWorkspaceTests
    {
        public class Constructors : TestBase
        {
            [Fact]
            public void Loader_constructors_validate_for_null_delegates()
            {
                Assert.Equal(
                    "cSpaceLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(null, () => null, () => null)).ParamName);
                Assert.Equal(
                    "sSpaceLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(() => null, null, () => null)).ParamName);
                Assert.Equal(
                    "csMappingLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(() => null, () => null, null)).ParamName);

                Assert.Equal(
                    "cSpaceLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(null, () => null, () => null, () => null))
                          .ParamName);
                Assert.Equal(
                    "sSpaceLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(() => null, null, () => null, () => null))
                          .ParamName);
                Assert.Equal(
                    "csMappingLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(() => null, () => null, null, () => null))
                          .ParamName);
                Assert.Equal(
                    "oSpaceLoader",
                    Assert.Throws<ArgumentNullException>(() => new MetadataWorkspace(() => null, () => null, () => null, null))
                          .ParamName);
            }

            [Fact]
            public void Parameterless_constructor_sets_up_default_o_space_collection()
            {
                Assert.IsType<ObjectItemCollection>(new MetadataWorkspace().GetItemCollection(DataSpace.OSpace));
            }

            [Fact]
            public void Three_delegates_constructor_uses_given_delegates_and_sets_up_default_o_space_and_oc_mapping()
            {
                var edmItemCollection = new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() });
                var storeItemCollection = new StoreItemCollection(new[] { XDocument.Parse(Ssdl).CreateReader() });
                var storageMappingItemCollection = LoadMsl(edmItemCollection, storeItemCollection);

                var workspace = new MetadataWorkspace(
                    () => edmItemCollection,
                    () => storeItemCollection,
                    () => storageMappingItemCollection);

                Assert.Same(edmItemCollection, workspace.GetItemCollection(DataSpace.CSpace));
                Assert.Same(storeItemCollection, workspace.GetItemCollection(DataSpace.SSpace));
                Assert.Same(storageMappingItemCollection, workspace.GetItemCollection(DataSpace.CSSpace));

                var objectItemCollection = (ObjectItemCollection)workspace.GetItemCollection(DataSpace.OSpace);
                var ocMappingCollection = (DefaultObjectMappingItemCollection)workspace.GetItemCollection(DataSpace.OCSpace);

                Assert.Same(objectItemCollection, ocMappingCollection.ObjectItemCollection);
                Assert.Same(edmItemCollection, ocMappingCollection.EdmItemCollection);
            }

            [Fact]
            public void Four_delegates_constructor_uses_given_delegates_and_sets_up_default_oc_mapping()
            {
                var edmItemCollection = new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() });
                var storeItemCollection = new StoreItemCollection(new[] { XDocument.Parse(Ssdl).CreateReader() });
                var objectItemCollection = new ObjectItemCollection();
                var storageMappingItemCollection = LoadMsl(edmItemCollection, storeItemCollection);

                var workspace = new MetadataWorkspace(
                    () => edmItemCollection,
                    () => storeItemCollection,
                    () => storageMappingItemCollection,
                    () => objectItemCollection);

                Assert.Same(edmItemCollection, workspace.GetItemCollection(DataSpace.CSpace));
                Assert.Same(storeItemCollection, workspace.GetItemCollection(DataSpace.SSpace));
                Assert.Same(storageMappingItemCollection, workspace.GetItemCollection(DataSpace.CSSpace));
                Assert.Same(objectItemCollection, workspace.GetItemCollection(DataSpace.OSpace));

                var ocMappingCollection = (DefaultObjectMappingItemCollection)workspace.GetItemCollection(DataSpace.OCSpace);
                Assert.Same(objectItemCollection, ocMappingCollection.ObjectItemCollection);
                Assert.Same(edmItemCollection, ocMappingCollection.EdmItemCollection);
            }

            [Fact]
            public void Paths_constructor_loads_collections_from_given_paths_and_sets_up_o_space_and_oc_mapping()
            {
                RunTestWithTempMetadata(
                    Csdl, Ssdl, Msl,
                    paths =>
                        {
                            var workspace = new MetadataWorkspace(paths, new Assembly[0]);

                            var cSpace = (EdmItemCollection)workspace.GetItemCollection(DataSpace.CSpace);
                            Assert.NotNull(cSpace.GetType("Entity", "AdventureWorksModel"));

                            var sSpace = (StoreItemCollection)workspace.GetItemCollection(DataSpace.SSpace);
                            Assert.NotNull(sSpace.GetType("Entities", "AdventureWorksModel.Store"));

                            var csMapping = (StorageMappingItemCollection)workspace.GetItemCollection(DataSpace.CSSpace);
                            Assert.Same(cSpace, csMapping.EdmItemCollection);
                            Assert.Same(sSpace, csMapping.StoreItemCollection);

                            var oSpace = (ObjectItemCollection)workspace.GetItemCollection(DataSpace.OSpace);
                            var ocMapping = (DefaultObjectMappingItemCollection)workspace.GetItemCollection(DataSpace.OCSpace);
                            Assert.Same(oSpace, ocMapping.ObjectItemCollection);
                            Assert.Same(cSpace, ocMapping.EdmItemCollection);
                        });
            }
        }

        public class RegisterItemCollection : TestBase
        {
            [Fact]
            public void Item_collections_can_be_registered_into_an_empty_workspace()
            {
                Item_collections_can_be_registered(new MetadataWorkspace());
            }

            [Fact]
            public void Registering_a_new_item_collection_replaces_any_existing_registration()
            {
                var storageMappingItemCollection = LoadMsl(
                    new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() }),
                    new StoreItemCollection(new[] { XDocument.Parse(Ssdl).CreateReader() }));

                Item_collections_can_be_registered(
                    new MetadataWorkspace(
                        () => storageMappingItemCollection.EdmItemCollection,
                        () => storageMappingItemCollection.StoreItemCollection,
                        () => storageMappingItemCollection));
            }

            private static void Item_collections_can_be_registered(MetadataWorkspace workspace)
            {
                var edmItemCollection = new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() });
                var storeItemCollection = new StoreItemCollection(new[] { XDocument.Parse(Ssdl).CreateReader() });
                var objectItemCollection = new ObjectItemCollection();
                var storageMappingItemCollection = LoadMsl(edmItemCollection, storeItemCollection);
                var ocMappingItemCollection = new DefaultObjectMappingItemCollection(edmItemCollection, objectItemCollection);

#pragma warning disable 612,618
                workspace.RegisterItemCollection(edmItemCollection);
                workspace.RegisterItemCollection(storeItemCollection);
                workspace.RegisterItemCollection(objectItemCollection);
                workspace.RegisterItemCollection(storageMappingItemCollection);
                workspace.RegisterItemCollection(ocMappingItemCollection);
#pragma warning restore 612,618

                Assert.Same(edmItemCollection, workspace.GetItemCollection(DataSpace.CSpace));
                Assert.Same(storeItemCollection, workspace.GetItemCollection(DataSpace.SSpace));
                Assert.Same(storageMappingItemCollection, workspace.GetItemCollection(DataSpace.CSSpace));
                Assert.Same(objectItemCollection, workspace.GetItemCollection(DataSpace.OSpace));
                Assert.Same(ocMappingItemCollection, workspace.GetItemCollection(DataSpace.OCSpace));
            }

            [Fact]
            public void Registering_c_space_causes_oc_mapping_to_also_be_registered_if_it_is_not_already_registered()
            {
                var edmItemCollection = new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() });

                var workspace = new MetadataWorkspace();
#pragma warning disable 612,618
                workspace.RegisterItemCollection(edmItemCollection);
#pragma warning restore 612,618

                Assert.Same(edmItemCollection, workspace.GetItemCollection(DataSpace.CSpace));

                var ocMappingCollection = (DefaultObjectMappingItemCollection)workspace.GetItemCollection(DataSpace.OCSpace);
                Assert.Same(workspace.GetItemCollection(DataSpace.OSpace), ocMappingCollection.ObjectItemCollection);
                Assert.Same(edmItemCollection, ocMappingCollection.EdmItemCollection);
            }

            [Fact]
            public void Registering_c_space_or_o_space_does_not_cause_oc_mapping_to_be_registered_if_it_is_already_registered()
            {
                var edmItemCollection = new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() });
                var objectItemCollection = new ObjectItemCollection();
                var ocMappingItemCollection = new DefaultObjectMappingItemCollection(edmItemCollection, objectItemCollection);

                var workspace = new MetadataWorkspace();
#pragma warning disable 612,618
                workspace.RegisterItemCollection(ocMappingItemCollection);
                workspace.RegisterItemCollection(edmItemCollection);
                workspace.RegisterItemCollection(objectItemCollection);
#pragma warning restore 612,618

                Assert.Same(ocMappingItemCollection, workspace.GetItemCollection(DataSpace.OCSpace));
                Assert.Same(edmItemCollection, workspace.GetItemCollection(DataSpace.CSpace));
                Assert.Same(objectItemCollection, workspace.GetItemCollection(DataSpace.OSpace));
            }

            [Fact]
            public void
                Registering_o_space_causes_oc_mapping_to_also_be_registered_if_it_is_not_already_registered_and_c_space_is_registered()
            {
                var edmItemCollection = new EdmItemCollection(new[] { XDocument.Parse(Csdl).CreateReader() });
                var objectItemCollection = new ObjectItemCollection();

                var workspace = new MetadataWorkspace();
#pragma warning disable 612,618
                workspace.RegisterItemCollection(edmItemCollection);
                workspace.RegisterItemCollection(objectItemCollection);
#pragma warning restore 612,618

                Assert.Same(edmItemCollection, workspace.GetItemCollection(DataSpace.CSpace));
                Assert.Same(objectItemCollection, workspace.GetItemCollection(DataSpace.OSpace));

                var ocMappingCollection = (DefaultObjectMappingItemCollection)workspace.GetItemCollection(DataSpace.OCSpace);
                Assert.Same(objectItemCollection, ocMappingCollection.ObjectItemCollection);
                Assert.Same(edmItemCollection, ocMappingCollection.EdmItemCollection);
            }

            [Fact]
            public void Registering_o_space_does_not_cause_oc_mapping_to_be_registered_if_c_space_is_not_registered()
            {
                var objectItemCollection = new ObjectItemCollection();

                var workspace = new MetadataWorkspace();
#pragma warning disable 612,618
                workspace.RegisterItemCollection(objectItemCollection);
#pragma warning restore 612,618

                Assert.Same(objectItemCollection, workspace.GetItemCollection(DataSpace.OSpace));
                ItemCollection _;
                Assert.False(workspace.TryGetItemCollection(DataSpace.OCSpace, out _));
            }
        }

        public class ImplicitLoadAssemblyForType : TestBase
        {
            [Fact]
            public void ImplicitLoadAssemblyForType_checks_only_given_assembly_for_views_if_assembly_not_filtered()
            {
                var mockCache = new Mock<IViewAssemblyCache>();
                var workspace = new MetadataWorkspace(
                    () => new EdmItemCollection(Enumerable.Empty<XmlReader>()),
                    () => null,
                    () => null,
                    () => new ObjectItemCollection(mockCache.Object));

                workspace.ImplicitLoadAssemblyForType(typeof(FactAttribute), null);

                mockCache.Verify(m => m.CheckAssembly(typeof(FactAttribute).Assembly, false), Times.Once());
            }

            [Fact]
            public void ImplicitLoadAssemblyForType_checks_only_calling_assembly_for_views_if_type_assembly_filtered_and_no_schema_attribute()
            {
                var mockCache = new Mock<IViewAssemblyCache>();
                var workspace = new MetadataWorkspace(
                    () => new EdmItemCollection(Enumerable.Empty<XmlReader>()),
                    () => null,
                    () => null,
                    () => new ObjectItemCollection(mockCache.Object));

                workspace.ImplicitLoadAssemblyForType(typeof(object), typeof(FactAttribute).Assembly);

                mockCache.Verify(m => m.CheckAssembly(typeof(object).Assembly, It.IsAny<bool>()), Times.Never());
                mockCache.Verify(m => m.CheckAssembly(typeof(FactAttribute).Assembly, false), Times.Once());
            }

            [Fact]
            public void ImplicitLoadAssemblyForType_checks_calling_schema_assembly_and_references_for_views_if_type_assembly_filtered()
            {
                var assembly = new DynamicAssembly();
                assembly.HasAttribute(new EdmSchemaAttribute());
                var callingAssembly = assembly.Compile(new AssemblyName("WithEdmSchemaAttribute"));

                var mockCache = new Mock<IViewAssemblyCache>();
                var workspace = new MetadataWorkspace(
                    () => new EdmItemCollection(Enumerable.Empty<XmlReader>()),
                    () => null,
                    () => null,
                    () => new ObjectItemCollection(mockCache.Object));

                workspace.ImplicitLoadAssemblyForType(typeof(object), callingAssembly);

                mockCache.Verify(m => m.CheckAssembly(typeof(object).Assembly, It.IsAny<bool>()), Times.Never());
                mockCache.Verify(m => m.CheckAssembly(callingAssembly, true), Times.Once());
            }
        }

        public class LoadFromAssembly : TestBase
        {
            [Fact]
            public void LoadFromAssembly_checks_only_given_assembly_for_views()
            {
                var mockCache = new Mock<IViewAssemblyCache>();
                var workspace = new MetadataWorkspace(
                    () => new EdmItemCollection(Enumerable.Empty<XmlReader>()),
                    () => null,
                    () => null,
                    () => new ObjectItemCollection(mockCache.Object));

                workspace.LoadFromAssembly(typeof(object).Assembly);

                mockCache.Verify(m => m.CheckAssembly(typeof(object).Assembly, false), Times.Once());
            }
        }

        public class ClearCache : TestBase
        {
            [Fact]
            public void ClearCache_clears_cached_assembly_information_for_views()
            {
                var cache = DbConfiguration.GetService<IViewAssemblyCache>();
                cache.CheckAssembly(typeof(PregenContextEdmxViews).Assembly, followReferences: true);
                Assert.True(cache.Assemblies.Contains(typeof(PregenContextEdmxViews).Assembly));

                MetadataWorkspace.ClearCache();

                Assert.Equal(0, cache.Assemblies.Count());
            }
        }

        private static StorageMappingItemCollection LoadMsl(EdmItemCollection edmItemCollection, StoreItemCollection storeItemCollection)
        {
            IList<EdmSchemaError> errors;
            return StorageMappingItemCollection.Create(
                edmItemCollection,
                storeItemCollection,
                new[] { XDocument.Parse(Msl).CreateReader() },
                null,
                out errors);
        }

        private const string Ssdl =
            "<Schema Namespace='AdventureWorksModel.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>"
            +
            "  <EntityContainer Name='AdventureWorksModelStoreContainer'>" +
            "    <EntitySet Name='Entities' EntityType='AdventureWorksModel.Store.Entities' Schema='dbo' />" +
            "  </EntityContainer>" +
            "  <EntityType Name='Entities'>" +
            "    <Key>" +
            "      <PropertyRef Name='Id' />" +
            "    </Key>" +
            "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />" +
            "    <Property Name='Name' Type='nvarchar(max)' Nullable='false' />" +
            "  </EntityType>" +
            "</Schema>";

        private const string Csdl =
            "<Schema Namespace='AdventureWorksModel' Alias='Self' p1:UseStrongSpatialTypes='false' xmlns:annotation='http://schemas.microsoft.com/ado/2009/02/edm/annotation' xmlns:p1='http://schemas.microsoft.com/ado/2009/02/edm/annotation' xmlns='http://schemas.microsoft.com/ado/2009/11/edm'>"
            +
            "   <EntityContainer Name='AdventureWorksEntities3' p1:LazyLoadingEnabled='true' >" +
            "       <EntitySet Name='Entities' EntityType='AdventureWorksModel.Entity' />" +
            "   </EntityContainer>" +
            "   <EntityType Name='Entity'>" +
            "       <Key>" +
            "           <PropertyRef Name='Id' />" +
            "       </Key>" +
            "       <Property Type='Int32' Name='Id' Nullable='false' annotation:StoreGeneratedPattern='Identity' />" +
            "       <Property Type='String' Name='Name' Nullable='false' />" +
            "   </EntityType>" +
            "</Schema>";

        private const string Msl =
            "<Mapping Space='C-S' xmlns='http://schemas.microsoft.com/ado/2009/11/mapping/cs'>" +
            "  <EntityContainerMapping StorageEntityContainer='AdventureWorksModelStoreContainer' CdmEntityContainer='AdventureWorksEntities3'>"
            +
            "    <EntitySetMapping Name='Entities'>" +
            "      <EntityTypeMapping TypeName='IsTypeOf(AdventureWorksModel.Entity)'>" +
            "        <MappingFragment StoreEntitySet='Entities'>" +
            "          <ScalarProperty Name='Id' ColumnName='Id' />" +
            "          <ScalarProperty Name='Name' ColumnName='Name' />" +
            "        </MappingFragment>" +
            "      </EntityTypeMapping>" +
            "    </EntitySetMapping>" +
            "  </EntityContainerMapping>" +
            "</Mapping>";

    }
}
    
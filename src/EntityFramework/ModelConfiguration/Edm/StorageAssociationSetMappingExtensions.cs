// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    internal static class StorageAssociationSetMappingExtensions
    {
        public static StorageAssociationSetMapping Initialize(this StorageAssociationSetMapping associationSetMapping)
        {
            DebugCheck.NotNull(associationSetMapping);

            associationSetMapping.SourceEndMapping = new StorageEndPropertyMapping();
            associationSetMapping.TargetEndMapping = new StorageEndPropertyMapping();

            return associationSetMapping;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static object GetConfiguration(this StorageAssociationSetMapping associationSetMapping)
        {
            DebugCheck.NotNull(associationSetMapping);

            return associationSetMapping.Annotations.GetConfiguration();
        }

        public static void SetConfiguration(this StorageAssociationSetMapping associationSetMapping, object configuration)
        {
            DebugCheck.NotNull(associationSetMapping);

            associationSetMapping.Annotations.SetConfiguration(configuration);
        }
    }
}

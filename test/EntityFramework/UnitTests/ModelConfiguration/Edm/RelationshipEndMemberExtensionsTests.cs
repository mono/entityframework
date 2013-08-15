// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public sealed class RelationshipEndMemberExtensionsTests
    {
        [Fact]
        public void IsMany_should_return_true_when_end_kind_is_many()
        {
            var associationEnd = new AssociationEndMember("E", new EntityType("E", "N", DataSpace.CSpace))
                                     {
                                         RelationshipMultiplicity = RelationshipMultiplicity.Many
                                     };

            Assert.True(associationEnd.IsMany());
        }

        [Fact]
        public void IsOptional_should_return_true_when_end_kind_is_optional()
        {
            var associationEnd = new AssociationEndMember("E", new EntityType("E", "N", DataSpace.CSpace))
                                     {
                                         RelationshipMultiplicity = RelationshipMultiplicity.ZeroOrOne
                                     };

            Assert.True(associationEnd.IsOptional());
        }

        [Fact]
        public void IsRequired_should_return_true_when_end_kind_is_required()
        {
            var associationEnd = new AssociationEndMember("E", new EntityType("E", "N", DataSpace.CSpace))
                                     {
                                         RelationshipMultiplicity = RelationshipMultiplicity.One
                                     };

            Assert.True(associationEnd.IsRequired());
        }
    }
}

﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using Moq;
    using Xunit;

    public class EdmMemberTests
    {
        [Fact]
        public void Can_set_name_property()
        {
            var primitiveType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);

            var property = EdmProperty.Primitive("P", primitiveType);

            property.Name = "Foo";

            Assert.Equal("Foo", property.Name);
        }

        [Fact]
        public void Can_set_name_and_parent_notified()
        {
            var primitiveType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);

            var property = EdmProperty.Primitive("P", primitiveType);

            var entityTypeMock = new Mock<StructuralType>();

            property.ChangeDeclaringTypeWithoutCollectionFixup(entityTypeMock.Object);

            property.Name = "Foo";

            entityTypeMock.Verify(e => e.NotifyItemIdentityChanged(), Times.Once());
        }

        [Fact]
        public void Identity_uniquified_when_duplicate_name_set()
        {
            var primitiveType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);

            var property1 = EdmProperty.Primitive("Foo", primitiveType);
            var property2 = EdmProperty.Primitive("Bar", primitiveType);
            var property3 = EdmProperty.Primitive("Boo", primitiveType);

            var entityType = new EntityType("T","S",DataSpace.CSpace);

            entityType.AddMember(property1);
            entityType.AddMember(property2);
            entityType.AddMember(property3);

            property2.Name = "Foo";
            property3.Name = "Foo";

            Assert.Equal("Foo1", property2.Identity);
            Assert.Equal("Foo2", property3.Identity);
            Assert.Equal(3, entityType.Properties.Count);
        }

        [Fact]
        public void Identity_synced_when_member_goes_readonly_and_parent_notified()
        {
            var primitiveType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);

            var property1 = EdmProperty.Primitive("Foo", primitiveType);
            var property2 = EdmProperty.Primitive("Bar", primitiveType);
            var property3 = EdmProperty.Primitive("Boo", primitiveType);

            var entityTypeMock = new Mock<EntityType>("T", "S", DataSpace.CSpace)
            {
                CallBase = true
            };
            entityTypeMock.Setup(e => e.NotifyItemIdentityChanged());

            entityTypeMock.Object.AddMember(property1);
            entityTypeMock.Object.AddMember(property2);
            entityTypeMock.Object.AddMember(property3);

            property2.Name = "Foo";
            property3.Name = "Foo";

            Assert.Equal("Foo1", property2.Identity);
            Assert.Equal("Foo2", property3.Identity);
            entityTypeMock.Verify(e => e.NotifyItemIdentityChanged(), Times.Exactly(2));

            property2.SetReadOnly();

            Assert.Equal("Foo", property2.Identity);
            entityTypeMock.Verify(e => e.NotifyItemIdentityChanged(), Times.Exactly(3));

            property3.SetReadOnly();

            Assert.Equal("Foo", property3.Identity);
            entityTypeMock.Verify(e => e.NotifyItemIdentityChanged(), Times.Exactly(4));
        }


        [Fact]
        public void IsPrimaryKeyColumn_should_return_true_when_parent_key_members_contains_member()
        {
            var primitiveType = PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32);

            var property = EdmProperty.Primitive("P", primitiveType);

            Assert.False(property.IsPrimaryKeyColumn);

            new EntityType("E", "N", DataSpace.CSpace).AddKeyMember(property);

            Assert.True(property.IsPrimaryKeyColumn);
        }
    }
}

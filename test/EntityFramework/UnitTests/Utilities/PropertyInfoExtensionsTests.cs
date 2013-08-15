// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Utilities
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Spatial;
    using System.IO;
    using System.Reflection;
    using Moq;
    using Xunit;

    public sealed class PropertyInfoExtensionsTests
    {
        [Fact]
        public void IsValidStructuralProperty_should_return_true_when_property_read_write()
        {
            var mockProperty = new MockPropertyInfo(typeof(int), "P");

            Assert.True(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_false_when_property_invalid()
        {
            var mockProperty = new MockPropertyInfo(typeof(object), "P");

            Assert.False(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_false_when_property_abstract()
        {
            var mockProperty = new MockPropertyInfo().Abstract();

            Assert.False(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_false_when_property_write_only()
        {
            var mockProperty = new MockPropertyInfo();
            mockProperty.SetupGet(p => p.CanRead).Returns(false);

            Assert.False(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_false_when_property_read_only()
        {
            var mockProperty = new MockPropertyInfo(typeof(object), "Prop");
            mockProperty.SetupGet(p => p.CanWrite).Returns(false);

            var mockType = new MockType();
            mockType.Setup(m => m.GetProperties(It.IsAny<BindingFlags>())).Returns(new[] { mockProperty.Object });

            mockProperty.SetupGet(p => p.DeclaringType).Returns(mockType.Object);

            Assert.False(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_true_when_property_read_only_collection()
        {
            var mockProperty = new MockPropertyInfo(typeof(List<string>), "Coll");
            mockProperty.SetupGet(p => p.CanWrite).Returns(false);

            var mockType = new MockType();
            mockType.Setup(m => m.GetProperties(It.IsAny<BindingFlags>())).Returns(new[] { mockProperty.Object });

            mockProperty.SetupGet(p => p.DeclaringType).Returns(mockType.Object);

            Assert.True(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_false_for_indexed_property()
        {
            var mockProperty = new MockPropertyInfo();
            mockProperty.Setup(p => p.GetIndexParameters()).Returns(new ParameterInfo[1]);

            Assert.False(mockProperty.Object.IsValidStructuralProperty());
        }

        [Fact]
        public void IsValidStructuralProperty_should_return_true_when_declaring_type_can_be_set()
        {
            Assert.True(typeof(Derived).GetProperty("Prop").IsValidStructuralProperty());
        }

        public class Base
        {
            public int Prop { get; private set; }
        }

        public class Derived : Base
        {
        }

        [Fact]
        public void CanWriteExtended_returns_true_for_property_that_has_a_private_setter()
        {
            Assert.True(typeof(Base).GetProperty("Prop").CanWriteExtended());
        }

        [Fact]
        public void CanWriteExtended_returns_true_for_property_that_has_a_private_setter_on_the_declaring_type()
        {
            Assert.True(typeof(Derived).GetProperty("Prop").CanWriteExtended());
        }

        [Fact]
        public void GetPropertyInfoForSet_returns_given_property_if_it_has_a_setter()
        {
            var propertyInfo = typeof(Base).GetProperty("Prop");
            Assert.Same(propertyInfo, propertyInfo.GetPropertyInfoForSet());
        }

        [Fact]
        public void GetPropertyInfoForSet_returns_declaring_type_property_if_it_has_a_setter()
        {
            Assert.Same(typeof(Base).GetProperty("Prop"), typeof(Derived).GetProperty("Prop").GetPropertyInfoForSet());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_nullable_scalar()
        {
            var mockProperty = new MockPropertyInfo(typeof(int?), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_string()
        {
            var mockProperty = new MockPropertyInfo(typeof(string), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_byte_array()
        {
            var mockProperty = new MockPropertyInfo(typeof(byte[]), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_geography()
        {
            var mockProperty = new MockPropertyInfo(typeof(DbGeography), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_geometry()
        {
            var mockProperty = new MockPropertyInfo(typeof(DbGeometry), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_scalar()
        {
            var mockProperty = new MockPropertyInfo(typeof(decimal), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_enum()
        {
            var mockProperty = new MockPropertyInfo(typeof(FileMode), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_true_for_nullable_enum()
        {
            var mockProperty = new MockPropertyInfo(typeof(FileMode?), "P");

            Assert.True(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void IsValidEdmScalarProperty_should_return_false_when_invalid_type()
        {
            var mockProperty = new MockPropertyInfo(typeof(object), "P");

            Assert.False(mockProperty.Object.IsValidEdmScalarProperty());
        }

        [Fact]
        public void AsEdmPrimitiveProperty_sets_fields_from_propertyInfo()
        {
            var propertyInfo = typeof(PropertyInfoExtensions_properties_fixture).GetProperty("Key");
            var property = propertyInfo.AsEdmPrimitiveProperty();

            Assert.Equal("Key", property.Name);
            Assert.Equal(false, property.Nullable);
            Assert.Equal(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32), property.PrimitiveType);
        }

        [Fact]
        public void AsEdmPrimitiveProperty_sets_is_nullable_for_nullable_type()
        {
            PropertyInfo propertyInfo = new MockPropertyInfo(typeof(string), "P");
            var property = propertyInfo.AsEdmPrimitiveProperty();

            Assert.Equal(true, property.Nullable);
        }

        [Fact]
        public void AsEdmPrimitiveProperty_returns_null_for_non_primitive_type()
        {
            var propertyInfo = typeof(PropertyInfoExtensions_properties_fixture).GetProperty("EdmProperty");
            var property = propertyInfo.AsEdmPrimitiveProperty();

            Assert.Null(property);
        }

        private class PropertyInfoExtensions_properties_fixture
        {
            public int Key { get; set; }
            public EntityType EdmProperty { get; set; }
        }
    }
}

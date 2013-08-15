﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Types
{
    using System.Data.Entity.ModelConfiguration.Configuration.Properties.Primitive;
    using System.Data.Entity.Resources;
    using System.Linq;
    using Xunit;

    public class LightweightEntityConfigurationOfTTests
    {
        [Fact]
        public void Ctor_evaluates_preconditions()
        {
            ArgumentException ex;
            var type = typeof(LocalEntityType);

            ex = Assert.Throws<ArgumentNullException>(
                () => new LightweightEntityConfiguration<object>(null, () => new EntityTypeConfiguration(type)));

            Assert.Equal("type", ex.ParamName);

            ex = Assert.Throws<ArgumentNullException>(
                () => new LightweightEntityConfiguration<object>(type, null));

            Assert.Equal("configuration", ex.ParamName);

            ex = Assert.Throws<ArgumentException>(
                () => new LightweightEntityConfiguration<Assert>(
                          type,
                          () => new EntityTypeConfiguration(type)));

            Assert.Equal(
                Strings.LightweightEntityConfiguration_TypeMismatch(type, typeof(Assert)),
                ex.Message);
        }

        [Fact]
        public void Ignore_evaluates_preconditions()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<object>(type, () => innerConfig);

            var ex = Assert.Throws<ArgumentNullException>(
                () => config.Ignore<object>(null));

            Assert.Equal("propertyExpression", ex.ParamName);
        }

        [Fact]
        public void Ignore_configures()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<LocalEntityType>(type, () => innerConfig);

            config.Ignore(t => t.Property1);

            Assert.Equal(1, innerConfig.IgnoredProperties.Count());
            Assert.True(innerConfig.IgnoredProperties.Any(p => p.Name == "Property1"));
        }

        [Fact]
        public void Property_evaluates_preconditions()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<object>(type, () => innerConfig);

            var ex = Assert.Throws<ArgumentNullException>(
                () => config.Property<object>(null));

            Assert.Equal("propertyExpression", ex.ParamName);
        }

        [Fact]
        public void Property_returns_configuration()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<LocalEntityType>(type, () => innerConfig);

            var result = config.Property(e => e.Property1);

            Assert.NotNull(result);
            Assert.NotNull(result.ClrPropertyInfo);
            Assert.Equal("Property1", result.ClrPropertyInfo.Name);
            Assert.Equal(typeof(decimal), result.ClrPropertyInfo.PropertyType);
            Assert.NotNull(result.Configuration);
            Assert.IsType<DecimalPropertyConfiguration>(result.Configuration());
        }

        [Fact]
        public void HasKey_evaluates_preconditions()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<object>(type, () => innerConfig);

            var ex = Assert.Throws<ArgumentNullException>(
                () => config.HasKey<object>(null));

            Assert.Equal("keyExpression", ex.ParamName);
        }

        [Fact]
        public void HasKey_configures_when_unset()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<LocalEntityType>(type, () => innerConfig);

            var result = config.HasKey(e => e.Property1);

            Assert.Equal(1, innerConfig.KeyProperties.Count());
            Assert.True(innerConfig.KeyProperties.Any(p => p.Name == "Property1"));
            Assert.Same(config, result);
        }

        [Fact]
        public void HasKey_composite_configures_when_unset()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<LocalEntityType>(type, () => innerConfig);

            var result = config.HasKey(
                e => new
                         {
                             e.Property1,
                             e.Property2
                         });

            Assert.Equal(2, innerConfig.KeyProperties.Count());
            Assert.True(innerConfig.KeyProperties.Any(p => p.Name == "Property1"));
            Assert.True(innerConfig.KeyProperties.Any(p => p.Name == "Property2"));
            Assert.Same(config, result);
        }

        [Fact]
        public void MapToStoredProcedures_with_no_args_should_add_configuration()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<LocalEntityType>(type, () => innerConfig);

            config.MapToStoredProcedures();

            Assert.True(innerConfig.IsMappedToFunctions);
        }

        [Fact]
        public void MapToStoredProcedures_with_action_should_invoke_and_add_configuration()
        {
            var type = typeof(LocalEntityType);
            var innerConfig = new EntityTypeConfiguration(type);
            var config = new LightweightEntityConfiguration<LocalEntityType>(type, () => innerConfig);

            ModificationFunctionsConfiguration<LocalEntityType> configuration = null;

            config.MapToStoredProcedures(c => configuration = c);

            Assert.Same(
                configuration.Configuration,
                innerConfig.ModificationFunctionsConfiguration);
        }

        private class LocalEntityType
        {
            public decimal Property1 { get; set; }
            public int Property2 { get; set; }
        }
    }
}

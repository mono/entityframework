﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration.Types
{
    using System.ComponentModel;
    using System.Data.Entity.ModelConfiguration.Configuration.Properties.Primitive;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;

    /// <summary>
    ///     Allows configuration to be performed for an entity type in a model.
    ///     This configuration functionality is available via lightweight conventions.
    /// </summary>
    /// <typeparam name="T"> A type inherited by the entity type. </typeparam>
    public class LightweightEntityConfiguration<T>
        where T : class
    {
        private readonly LightweightEntityConfiguration _configuration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="LightweightEntityConfiguration{T}" /> class.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type" /> of this entity type.
        /// </param>
        /// <param name="configuration"> The configuration object that this instance wraps. </param>
        internal LightweightEntityConfiguration(Type type, Func<EntityTypeConfiguration> configuration)
        {
            Check.NotNull(type, "type");
            Check.NotNull(configuration, "configuration");

            if (!typeof(T).IsAssignableFrom(type))
            {
                throw Error.LightweightEntityConfiguration_TypeMismatch(type, typeof(T));
            }

            _configuration = new LightweightEntityConfiguration(type, configuration);
        }

        /// <summary>
        ///     Gets the <see cref="Type" /> of this entity type.
        /// </summary>
        public Type ClrType
        {
            get { return _configuration.ClrType; }
        }

        /// <summary>
        ///     Configures the entity set name to be used for this entity type.
        ///     The entity set name can only be configured for the base type in each set.
        /// </summary>
        /// <param name="entitySetName"> The name of the entity set. </param>
        /// <returns>
        ///     The same <see cref="LightweightEntityConfiguration{T}" /> instance so that multiple calls can be chained.
        /// </returns>
        /// <remarks>
        ///     Calling this will have no effect once it has been configured.
        /// </remarks>
        public LightweightEntityConfiguration<T> HasEntitySetName(string entitySetName)
        {
            _configuration.HasEntitySetName(entitySetName);

            return this;
        }

        /// <summary>
        ///     Excludes a property from the model so that it will not be mapped to the database.
        /// </summary>
        /// <typeparam name="TProperty"> The type of the property to be ignored. </typeparam>
        /// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public LightweightEntityConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, "propertyExpression");

            _configuration.Ignore(propertyExpression.GetSimplePropertyAccess().Single());

            return this;
        }

        /// <summary>
        ///     Configures a property that is defined on this type.
        /// </summary>
        /// <typeparam name="TProperty"> The type of the property being configured. </typeparam>
        /// <param name="propertyExpression"> A lambda expression representing the property to be configured. C#: t => t.MyProperty VB.Net: Function(t) t.MyProperty </param>
        /// <returns> A configuration object that can be used to configure the property. </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public LightweightPropertyConfiguration Property<TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            Check.NotNull(propertyExpression, "propertyExpression");

            return _configuration.Property(propertyExpression.GetComplexPropertyAccess());
        }

        /// <summary>
        ///     Configures the primary key property(s) for this entity type.
        /// </summary>
        /// <typeparam name="TProperty"> The type of the key. </typeparam>
        /// <param name="keyExpression"> A lambda expression representing the property to be used as the primary key. C#: t => t.Id VB.Net: Function(t) t.Id If the primary key is made up of multiple properties then specify an anonymous type including the properties. C#: t => new { t.Id1, t.Id2 } VB.Net: Function(t) New With { t.Id1, t.Id2 } </param>
        /// <returns>
        ///     The same <see cref="LightweightEntityConfiguration{T}" /> instance so that multiple calls can be chained.
        /// </returns>
        /// <remarks>
        ///     Calling this will have no effect once it has been configured.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public LightweightEntityConfiguration<T> HasKey<TProperty>(Expression<Func<T, TProperty>> keyExpression)
        {
            Check.NotNull(keyExpression, "keyExpression");

            _configuration.HasKey(keyExpression.GetSimplePropertyAccessList().Select(p => p.Single()));

            return this;
        }

        /// <summary>
        ///     Configures the table name that this entity type is mapped to.
        /// </summary>
        /// <param name="tableName"> The name of the table. </param>
        /// <remarks>
        ///     Calling this will have no effect once it has been configured.
        /// </remarks>
        public LightweightEntityConfiguration<T> ToTable(string tableName)
        {
            Check.NotEmpty(tableName, "tableName");

            _configuration.ToTable(tableName);

            return this;
        }

        /// <summary>
        ///     Configures the table name that this entity type is mapped to.
        /// </summary>
        /// <param name="tableName"> The name of the table. </param>
        /// <param name="schemaName"> The database schema of the table. </param>
        /// <remarks>
        ///     Calling this will have no effect once it has been configured.
        /// </remarks>
        public LightweightEntityConfiguration<T> ToTable(string tableName, string schemaName)
        {
            Check.NotEmpty(tableName, "tableName");

            _configuration.ToTable(tableName, schemaName);

            return this;
        }

        public LightweightEntityConfiguration<T> MapToStoredProcedures()
        {
            _configuration.MapToStoredProcedures();

            return this;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public LightweightEntityConfiguration<T> MapToStoredProcedures(
            Action<ModificationFunctionsConfiguration<T>> modificationFunctionsConfigurationAction)
        {
            Check.NotNull(modificationFunctionsConfigurationAction, "modificationFunctionsConfigurationAction");

            var modificationFunctionMappingConfiguration = new ModificationFunctionsConfiguration<T>();

            modificationFunctionsConfigurationAction(modificationFunctionMappingConfiguration);

            _configuration.MapToStoredProcedures(modificationFunctionMappingConfiguration.Configuration);

            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}

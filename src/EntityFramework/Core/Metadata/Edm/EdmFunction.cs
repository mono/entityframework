// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     Class for representing a function
    /// </summary>
    public class EdmFunction : EdmType
    {
        internal EdmFunction(string name, string namespaceName, DataSpace dataSpace)
            : this(name, namespaceName, dataSpace, new EdmFunctionPayload())
        {
            // testing only
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal EdmFunction(string name, string namespaceName, DataSpace dataSpace, EdmFunctionPayload payload)
            : base(name, namespaceName, dataSpace)
        {
            //---- name of the 'schema'
            //---- this is used by the SQL Gen utility and update pipeline to support generation of the correct function name in the store
            _schemaName = payload.Schema;

            var returnParameters = payload.ReturnParameters ?? new FunctionParameter[0];

            foreach (var returnParameter in returnParameters)
            {
                if (returnParameter == null)
                {
                    throw new ArgumentException(Strings.ADP_CollectionParameterElementIsNull("ReturnParameters"));
                }

                if (returnParameter.Mode != ParameterMode.ReturnValue)
                {
                    throw new ArgumentException(Strings.NonReturnParameterInReturnParameterCollection);
                }
            }

            _returnParameters = new ReadOnlyMetadataCollection<FunctionParameter>(
                returnParameters
                    .Select(
                        (returnParameter) =>
                        SafeLink<EdmFunction>.BindChild(this, FunctionParameter.DeclaringFunctionLinker, returnParameter))
                    .ToList());

            if (payload.IsAggregate.HasValue)
            {
                SetFunctionAttribute(ref _functionAttributes, FunctionAttributes.Aggregate, payload.IsAggregate.Value);
            }
            if (payload.IsBuiltIn.HasValue)
            {
                SetFunctionAttribute(ref _functionAttributes, FunctionAttributes.BuiltIn, payload.IsBuiltIn.Value);
            }
            if (payload.IsNiladic.HasValue)
            {
                SetFunctionAttribute(ref _functionAttributes, FunctionAttributes.NiladicFunction, payload.IsNiladic.Value);
            }
            if (payload.IsComposable.HasValue)
            {
                SetFunctionAttribute(ref _functionAttributes, FunctionAttributes.IsComposable, payload.IsComposable.Value);
            }
            if (payload.IsFromProviderManifest.HasValue)
            {
                SetFunctionAttribute(
                    ref _functionAttributes, FunctionAttributes.IsFromProviderManifest, payload.IsFromProviderManifest.Value);
            }
            if (payload.IsCachedStoreFunction.HasValue)
            {
                SetFunctionAttribute(ref _functionAttributes, FunctionAttributes.IsCachedStoreFunction, payload.IsCachedStoreFunction.Value);
            }
            if (payload.IsFunctionImport.HasValue)
            {
                SetFunctionAttribute(ref _functionAttributes, FunctionAttributes.IsFunctionImport, payload.IsFunctionImport.Value);
            }

            if (payload.ParameterTypeSemantics.HasValue)
            {
                _parameterTypeSemantics = payload.ParameterTypeSemantics.Value;
            }

            if (payload.StoreFunctionName != null)
            {
                _storeFunctionNameAttribute = payload.StoreFunctionName;
            }

            if (payload.EntitySets != null)
            {
                if (payload.EntitySets.Length != returnParameters.Length)
                {
                    throw new ArgumentException(Strings.NumberOfEntitySetsDoesNotMatchNumberOfReturnParameters);
                }

                _entitySets = new ReadOnlyMetadataCollection<EntitySet>(payload.EntitySets);
            }
            else
            {
                if (_returnParameters.Count > 1)
                {
                    throw new ArgumentException(Strings.NullEntitySetsForFunctionReturningMultipleResultSets);
                }

                _entitySets = new ReadOnlyMetadataCollection<EntitySet>(_returnParameters.Select(p => (EntitySet)null).ToList());
            }

            if (payload.CommandText != null)
            {
                _commandTextAttribute = payload.CommandText;
            }

            if (payload.Parameters != null)
            {
                // validate the parameters
                foreach (var parameter in payload.Parameters)
                {
                    if (parameter == null)
                    {
                        throw new ArgumentException(Strings.ADP_CollectionParameterElementIsNull("parameters"));
                    }

                    if (parameter.Mode == ParameterMode.ReturnValue)
                    {
                        throw new ArgumentException(Strings.ReturnParameterInInputParameterCollection);
                    }
                }

                // Populate the parameters
                _parameters = new SafeLinkCollection<EdmFunction, FunctionParameter>(
                    this, FunctionParameter.DeclaringFunctionLinker, new MetadataCollection<FunctionParameter>(payload.Parameters));
            }
            else
            {
                _parameters = new ReadOnlyMetadataCollection<FunctionParameter>(new MetadataCollection<FunctionParameter>());
            }
        }

        private readonly ReadOnlyMetadataCollection<FunctionParameter> _returnParameters;
        private readonly ReadOnlyMetadataCollection<FunctionParameter> _parameters;
        private readonly FunctionAttributes _functionAttributes = FunctionAttributes.Default;
        private readonly string _storeFunctionNameAttribute;
        private readonly ParameterTypeSemantics _parameterTypeSemantics;
        private readonly string _commandTextAttribute;
        private string _schemaName;
        private readonly ReadOnlyMetadataCollection<EntitySet> _entitySets;

        /// <summary>
        ///     Gets the built-in type kind for this <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />.
        /// </summary>
        /// <returns>
        ///     One of the enumeration values of the <see cref="T:System.Data.Entity.Core.Metadata.Edm.BuiltInTypeKind" /> enumeration.
        /// </returns>
        public override BuiltInTypeKind BuiltInTypeKind
        {
            get { return BuiltInTypeKind.EdmFunction; }
        }

        /// <summary>Returns the full name (namespace plus name) of this type. </summary>
        /// <returns>The full name of the type.</returns>
        public override string FullName
        {
            get { return NamespaceName + "." + Name; }
        }

        /// <summary>
        ///     Gets the parameters of this <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />.
        /// </summary>
        /// <returns>
        ///     A collection of type <see cref="T:System.Data.Entity.Core.Metadata.Edm.ReadOnlyMetadataCollection`1" /> that contains the parameters of this
        ///     <see
        ///         cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />
        ///     .
        /// </returns>
        public ReadOnlyMetadataCollection<FunctionParameter> Parameters
        {
            get { return _parameters; }
        }

        /// <summary>
        ///     Returns true if this is a C-space function and it has an eSQL body defined as DefiningExpression.
        /// </summary>
        internal bool HasUserDefinedBody
        {
            get { return IsModelDefinedFunction && !String.IsNullOrEmpty(CommandTextAttribute); }
        }

        /// <summary>
        ///     For function imports, optionally indicates the entity set to which the result is bound.
        ///     If the function import has multiple result sets, returns the entity set to which the first result is bound
        /// </summary>
        [MetadataProperty(BuiltInTypeKind.EntitySet, false)]
        internal EntitySet EntitySet
        {
            get { return _entitySets.Count != 0 ? _entitySets[0] : null; }
        }

        /// <summary>
        ///     For function imports, indicates the entity sets to which the return parameters are bound.
        ///     The number of elements in the collection matches the number of return parameters.
        ///     A null element in the collection indicates that the corresponding are not bound to an entity set.
        /// </summary>
        [MetadataProperty(BuiltInTypeKind.EntitySet, true)]
        internal ReadOnlyMetadataCollection<EntitySet> EntitySets
        {
            get { return _entitySets; }
        }

        /// <summary>
        ///     Gets the return parameter of this <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Data.Entity.Core.Metadata.Edm.FunctionParameter" /> object that represents the return parameter of this
        ///     <see
        ///         cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />
        ///     .
        /// </returns>
        [MetadataProperty(BuiltInTypeKind.FunctionParameter, false)]
        public FunctionParameter ReturnParameter
        {
            get { return _returnParameters.FirstOrDefault(); }
        }

        /// <summary>
        ///     Gets the return parameters of this <see cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />.
        /// </summary>
        /// <returns>
        ///     A collection of type <see cref="T:System.Data.Entity.Core.Metadata.Edm.ReadOnlyMetadataCollection`1" /> that represents the return parameters of this
        ///     <see
        ///         cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />
        ///     .
        /// </returns>
        [MetadataProperty(BuiltInTypeKind.FunctionParameter, true)]
        public ReadOnlyMetadataCollection<FunctionParameter> ReturnParameters
        {
            get { return _returnParameters; }
        }

        [MetadataProperty(PrimitiveTypeKind.String, false)]
        public string StoreFunctionNameAttribute
        {
            get { return _storeFunctionNameAttribute; }
        }

        [MetadataProperty(typeof(ParameterTypeSemantics), false)]
        public ParameterTypeSemantics ParameterTypeSemanticsAttribute
        {
            get { return _parameterTypeSemantics; }
        }

        // Function attribute parameters
        [MetadataProperty(PrimitiveTypeKind.Boolean, false)]
        public bool AggregateAttribute
        {
            get { return GetFunctionAttribute(FunctionAttributes.Aggregate); }
        }

        [MetadataProperty(PrimitiveTypeKind.Boolean, false)]
        public virtual bool BuiltInAttribute
        {
            get { return GetFunctionAttribute(FunctionAttributes.BuiltIn); }
        }

        [MetadataProperty(PrimitiveTypeKind.Boolean, false)]
        public bool IsFromProviderManifest
        {
            get { return GetFunctionAttribute(FunctionAttributes.IsFromProviderManifest); }
        }

        [MetadataProperty(PrimitiveTypeKind.Boolean, false)]
        public bool NiladicFunctionAttribute
        {
            get { return GetFunctionAttribute(FunctionAttributes.NiladicFunction); }
        }

        /// <summary>Gets or sets whether this instance is mapped to a function or to a stored procedure.</summary>
        /// <returns>true if this instance is mapped to a function; false if this instance is mapped to a stored procedure.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Composable")]
        [MetadataProperty(PrimitiveTypeKind.Boolean, false)]
        public bool IsComposableAttribute
        {
            get { return GetFunctionAttribute(FunctionAttributes.IsComposable); }
        }

        /// <summary>Gets a query in the language that is used by the database management system or storage model. </summary>
        /// <returns>
        ///     A string value in the syntax used by the database management system or storage model that contains the query or update statement of the
        ///     <see
        ///         cref="T:System.Data.Entity.Core.Metadata.Edm.EdmFunction" />
        ///     .
        /// </returns>
        [MetadataProperty(PrimitiveTypeKind.String, false)]
        public string CommandTextAttribute
        {
            get { return _commandTextAttribute; }
        }

        internal bool IsCachedStoreFunction
        {
            get { return GetFunctionAttribute(FunctionAttributes.IsCachedStoreFunction); }
        }

        internal bool IsModelDefinedFunction
        {
            get { return DataSpace == DataSpace.CSpace && !IsCachedStoreFunction && !IsFromProviderManifest && !IsFunctionImport; }
        }

        internal bool IsFunctionImport
        {
            get { return GetFunctionAttribute(FunctionAttributes.IsFunctionImport); }
        }

        [MetadataProperty(PrimitiveTypeKind.String, false)]
        public string Schema
        {
            get { return _schemaName; }
            set
            {
                Check.NotEmpty(value, "value");
                Util.ThrowIfReadOnly(this);

                _schemaName = value;
            }
        }

        /// <summary>
        ///     Sets this item to be readonly, once this is set, the item will never be writable again.
        /// </summary>
        internal override void SetReadOnly()
        {
            if (!IsReadOnly)
            {
                base.SetReadOnly();
                Parameters.Source.SetReadOnly();
                foreach (var returnParameter in ReturnParameters)
                {
                    returnParameter.SetReadOnly();
                }
            }
        }

        /// <summary>
        ///     Builds function identity string in the form of "functionName (param1, param2, ... paramN)".
        /// </summary>
        internal override void BuildIdentity(StringBuilder builder)
        {
            // If we've already cached the identity, simply append it
            if (null != CacheIdentity)
            {
                builder.Append(CacheIdentity);
                return;
            }

            BuildIdentity(
                builder,
                FullName,
                Parameters,
                (param) => param.TypeUsage,
                (param) => param.Mode);
        }

        /// <summary>
        ///     Builds identity based on the functionName and parameter types. All parameters are assumed to be
        ///     <see
        ///         cref="ParameterMode.In" />
        ///     .
        ///     Returns string in the form of "functionName (param1, param2, ... paramN)".
        /// </summary>
        internal static string BuildIdentity(string functionName, IEnumerable<TypeUsage> functionParameters)
        {
            var identity = new StringBuilder();

            BuildIdentity(
                identity,
                functionName,
                functionParameters,
                (param) => param,
                (param) => ParameterMode.In);

            return identity.ToString();
        }

        /// <summary>
        ///     Builds identity based on the functionName and parameters metadata.
        ///     Returns string in the form of "functionName (param1, param2, ... paramN)".
        /// </summary>
        internal static void BuildIdentity<TParameterMetadata>(
            StringBuilder builder,
            string functionName,
            IEnumerable<TParameterMetadata> functionParameters,
            Func<TParameterMetadata, TypeUsage> getParameterTypeUsage,
            Func<TParameterMetadata, ParameterMode> getParameterMode)
        {
            //
            // Note: some callers depend on the format of the returned identity string.
            //

            // Start with the function name
            builder.Append(functionName);

            // Then add the string representing the list of parameters
            builder.Append('(');
            var first = true;
            foreach (var parameter in functionParameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(",");
                }
                builder.Append(Helper.ToString(getParameterMode(parameter)));
                builder.Append(' ');
                getParameterTypeUsage(parameter).BuildIdentity(builder);
            }
            builder.Append(')');
        }

        private bool GetFunctionAttribute(FunctionAttributes attribute)
        {
            return attribute == (attribute & _functionAttributes);
        }

        private static void SetFunctionAttribute(ref FunctionAttributes field, FunctionAttributes attribute, bool isSet)
        {
            if (isSet)
            {
                // make sure that attribute bits are set to 1
                field |= attribute;
            }
            else
            {
                // make sure that attribute bits are set to 0
                field ^= field & attribute;
            }
        }

        [Flags]
        private enum FunctionAttributes : byte
        {
            None = 0,
            Aggregate = 1,
            BuiltIn = 2,
            NiladicFunction = 4,
            IsComposable = 8,
            IsFromProviderManifest = 16,
            IsCachedStoreFunction = 32,
            IsFunctionImport = 64,
            Default = IsComposable,
        }

        /// <summary>
        ///     The factory method for constructing the <see cref="EdmFunction" /> object.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="namespaceName">The namespace of the function.</param>
        /// <param name="dataSpace">The namespace the function belongs to.</param>
        /// <param name="payload">Additional function attributes and properties.</param>
        /// <param name="metadataProperties">Metadata properties that will be added to the function. Can be null.</param>
        /// <returns>
        ///     A new, read-only instance of the <see cref="EdmFunction" /> type.
        /// </returns>
        public static EdmFunction Create(
            string name,
            string namespaceName,
            DataSpace dataSpace,
            EdmFunctionPayload payload,
            IEnumerable<MetadataProperty> metadataProperties)
        {
            Check.NotNull(name, "name");
            Check.NotNull(namespaceName, "namespaceName");

            var function = new EdmFunction(name, namespaceName, dataSpace, payload);

            if (metadataProperties != null)
            {
                function.AddMetadataProperties(metadataProperties.ToList());
            }

            function.SetReadOnly();

            return function;
        }
    }

    /// <summary>
    ///     Contains additional attributes and properties of the <see cref="EdmFunction" />
    /// </summary>
    /// <remarks>
    ///     Note that <see cref="EdmFunctionPayload" /> objects are short lived and exist only to
    ///     make <see cref="EdmFunction" /> initialization easier. Instance of this type are not
    ///     compared to each other and arrays returned by array properties are copied to internal
    ///     collections in the <see cref="EdmFunction" /> ctor. Therefore it is fine to suppress the
    ///     Code Analysis messages.
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct EdmFunctionPayload
    {
        public string Schema { get; set; }
        public string StoreFunctionName { get; set; }
        public string CommandText { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public EntitySet[] EntitySets { get; set; }

        public bool? IsAggregate { get; set; }
        public bool? IsBuiltIn { get; set; }
        public bool? IsNiladic { get; set; }
        public bool? IsComposable { get; set; }
        public bool? IsFromProviderManifest { get; set; }
        public bool? IsCachedStoreFunction { get; set; }
        public bool? IsFunctionImport { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public FunctionParameter[] ReturnParameters { get; set; }

        public ParameterTypeSemantics? ParameterTypeSemantics { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public FunctionParameter[] Parameters { get; set; }
    }
}

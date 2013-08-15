// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    /// <summary>
    ///     Represents the base item class for all the metadata
    /// </summary>
    public abstract partial class MetadataItem : IMetadataItem
    {
        /// <summary>
        ///     Implementing this internal constructor so that this class can't be derived
        ///     outside this assembly
        /// </summary>
        internal MetadataItem()
        {
        }

        internal MetadataItem(MetadataFlags flags)
        {
            _flags = flags;
        }

        [Flags]
        internal enum MetadataFlags
        {
            // GlobalItem
            None = 0, // DataSpace flags are off by one so that zero can be the uninitialized state
            CSpace = 1, // (1 << 0)
            OSpace = 2, // (1 << 1)
            OCSpace = 3, // CSpace | OSpace
            SSpace = 4, // (1 << 2)
            CSSpace = 5, // CSpace | SSpace

            DataSpace = OSpace | CSpace | SSpace | OCSpace | CSSpace,

            // MetadataItem
            Readonly = (1 << 3),

            // EdmType
            IsAbstract = (1 << 4),

            // FunctionParameter
            In = (1 << 9),
            Out = (1 << 10),
            InOut = In | Out,
            ReturnValue = (1 << 11),

            ParameterMode = (In | Out | InOut | ReturnValue),
        }

        private MetadataFlags _flags;
        private readonly object _flagsLock = new object();
        private MetadataCollection<MetadataProperty> _itemAttributes;
        private readonly List<DataModelAnnotation> annotationsList = new List<DataModelAnnotation>();

        /// <summary>
        ///     Gets the currently assigned annotations.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DataModelAnnotation> Annotations
        {
            get { return annotationsList; }
        }

        /// <summary>Gets the built-in type kind for this type.</summary>
        /// <returns>
        ///     A <see cref="T:System.Data.Entity.Core.Metadata.Edm.BuiltInTypeKind" /> object that represents the built-in type kind for this type.
        /// </returns>
        public abstract BuiltInTypeKind BuiltInTypeKind { get; }

        /// <summary>Gets the list of properties of the current type.</summary>
        /// <returns>
        ///     A collection of type <see cref="T:System.Data.Entity.Core.Metadata.Edm.ReadOnlyMetadataCollection`1" /> that contains the list of properties of the current type.
        /// </returns>
        [MetadataProperty(BuiltInTypeKind.MetadataProperty, true)]
        public virtual ReadOnlyMetadataCollection<MetadataProperty> MetadataProperties
        {
            get
            {
                if (null == _itemAttributes)
                {
                    var itemAttributes = new MetadataPropertyCollection(this);
                    if (IsReadOnly)
                    {
                        itemAttributes.SetReadOnly();
                    }
                    Interlocked.CompareExchange(
                        ref _itemAttributes, itemAttributes, null);
                }
                return _itemAttributes.AsReadOnlyMetadataCollection();
            }
        }

        /// <summary>
        ///     List of item attributes on this type
        /// </summary>
        internal MetadataCollection<MetadataProperty> RawMetadataProperties
        {
            get { return _itemAttributes; }
        }

        /// <summary>Gets or sets the documentation associated with this type.</summary>
        /// <returns>
        ///     A <see cref="T:System.Data.Entity.Core.Metadata.Edm.Documentation" /> object that represents the documentation on this type.
        /// </returns>
        public Documentation Documentation { get; set; }

        /// <summary>
        ///     Identity of the item
        /// </summary>
        internal abstract String Identity { get; }

        /// <summary>
        ///     Just checks for identities to be equal
        /// </summary>
        /// <param name="item"> </param>
        /// <returns> </returns>
        internal virtual bool EdmEquals(MetadataItem item)
        {
            return ((null != item) &&
                    ((this == item) || // same reference
                     (BuiltInTypeKind == item.BuiltInTypeKind &&
                      Identity == item.Identity)));
        }

        /// <summary>
        ///     Returns true if this item is not-changeable. Otherwise returns false.
        /// </summary>
        internal bool IsReadOnly
        {
            get { return GetFlag(MetadataFlags.Readonly); }
        }

        /// <summary>
        ///     Validates the types and sets the readOnly property to true. Once the type is set to readOnly,
        ///     it can never be changed.
        /// </summary>
        internal virtual void SetReadOnly()
        {
            if (!IsReadOnly)
            {
                if (null != _itemAttributes)
                {
                    _itemAttributes.SetReadOnly();
                }
                SetFlag(MetadataFlags.Readonly, true);
            }
        }

        /// <summary>
        ///     Builds identity string for this item. By default, the method calls the identity property.
        /// </summary>
        /// <param name="builder"> </param>
        internal virtual void BuildIdentity(StringBuilder builder)
        {
            builder.Append(Identity);
        }

        /// <summary>
        ///     Adds the given metadata property to the metadata property collection
        /// </summary>
        /// <param name="metadataProperty"> </param>
        internal void AddMetadataProperties(List<MetadataProperty> metadataProperties)
        {
            MetadataProperties.Source.AtomicAddRange(metadataProperties);
        }

        internal DataSpace GetDataSpace()
        {
            switch (_flags & MetadataFlags.DataSpace)
            {
                default:
                    return (DataSpace)(-1);
                case MetadataFlags.CSpace:
                    return DataSpace.CSpace;
                case MetadataFlags.OSpace:
                    return DataSpace.OSpace;
                case MetadataFlags.SSpace:
                    return DataSpace.SSpace;
                case MetadataFlags.OCSpace:
                    return DataSpace.OCSpace;
                case MetadataFlags.CSSpace:
                    return DataSpace.CSSpace;
            }
        }

        internal void SetDataSpace(DataSpace space)
        {
            _flags = (_flags & ~MetadataFlags.DataSpace) | (MetadataFlags.DataSpace & Convert(space));
        }

        private static MetadataFlags Convert(DataSpace space)
        {
            switch (space)
            {
                default:
                    return MetadataFlags.None; // invalid
                case DataSpace.CSpace:
                    return MetadataFlags.CSpace;
                case DataSpace.OSpace:
                    return MetadataFlags.OSpace;
                case DataSpace.SSpace:
                    return MetadataFlags.SSpace;
                case DataSpace.OCSpace:
                    return MetadataFlags.OCSpace;
                case DataSpace.CSSpace:
                    return MetadataFlags.CSSpace;
            }
        }

        internal ParameterMode GetParameterMode()
        {
            switch (_flags & MetadataFlags.ParameterMode)
            {
                default:
                    return (ParameterMode)(-1); // invalid
                case MetadataFlags.In:
                    return ParameterMode.In;
                case MetadataFlags.Out:
                    return ParameterMode.Out;
                case MetadataFlags.InOut:
                    return ParameterMode.InOut;
                case MetadataFlags.ReturnValue:
                    return ParameterMode.ReturnValue;
            }
        }

        internal void SetParameterMode(ParameterMode mode)
        {
            _flags = (_flags & ~MetadataFlags.ParameterMode) | (MetadataFlags.ParameterMode & Convert(mode));
        }

        private static MetadataFlags Convert(ParameterMode mode)
        {
            switch (mode)
            {
                default:
                    return MetadataFlags.ParameterMode; // invalid
                case ParameterMode.In:
                    return MetadataFlags.In;
                case ParameterMode.Out:
                    return MetadataFlags.Out;
                case ParameterMode.InOut:
                    return MetadataFlags.InOut;
                case ParameterMode.ReturnValue:
                    return MetadataFlags.ReturnValue;
            }
        }

        internal bool GetFlag(MetadataFlags flag)
        {
            return (flag == (_flags & flag));
        }

        internal void SetFlag(MetadataFlags flag, bool value)
        {
            if ((flag & MetadataFlags.Readonly)
                == MetadataFlags.Readonly)
            {
                Debug.Assert(
                    System.Convert.ToInt32(flag & ~MetadataFlags.Readonly, CultureInfo.InvariantCulture) == 0,
                    "SetFlag() invoked with Readonly and additional flags.");
            }

            lock (_flagsLock)
            {
                // an attempt to set the ReadOnly flag on a MetadataItem that is already read-only
                // is a no-op
                //
                if (IsReadOnly && ((flag & MetadataFlags.Readonly) == MetadataFlags.Readonly))
                {
                    return;
                }

                Util.ThrowIfReadOnly(this);
                if (value)
                {
                    _flags |= flag;
                }
                else
                {
                    _flags &= ~flag;
                }
            }
        }
    }
}

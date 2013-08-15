﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Utilities;

    internal class ModificationFunctionsConfiguration
    {
        private ModificationFunctionConfiguration _insertModificationFunctionConfiguration;
        private ModificationFunctionConfiguration _updateModificationFunctionConfiguration;
        private ModificationFunctionConfiguration _deleteModificationFunctionConfiguration;

        public ModificationFunctionsConfiguration()
        {
        }

        private ModificationFunctionsConfiguration(ModificationFunctionsConfiguration source)
        {
            DebugCheck.NotNull(source);

            if (source._insertModificationFunctionConfiguration != null)
            {
                _insertModificationFunctionConfiguration = source._insertModificationFunctionConfiguration.Clone();
            }

            if (source._updateModificationFunctionConfiguration != null)
            {
                _updateModificationFunctionConfiguration = source._updateModificationFunctionConfiguration.Clone();
            }

            if (source._deleteModificationFunctionConfiguration != null)
            {
                _deleteModificationFunctionConfiguration = source._deleteModificationFunctionConfiguration.Clone();
            }
        }

        public virtual ModificationFunctionsConfiguration Clone()
        {
            return new ModificationFunctionsConfiguration(this);
        }

        public virtual void Insert(ModificationFunctionConfiguration modificationFunctionConfiguration)
        {
            DebugCheck.NotNull(modificationFunctionConfiguration);

            _insertModificationFunctionConfiguration = modificationFunctionConfiguration;
        }

        public virtual void Update(ModificationFunctionConfiguration modificationFunctionConfiguration)
        {
            DebugCheck.NotNull(modificationFunctionConfiguration);

            _updateModificationFunctionConfiguration = modificationFunctionConfiguration;
        }

        public virtual void Delete(ModificationFunctionConfiguration modificationFunctionConfiguration)
        {
            DebugCheck.NotNull(modificationFunctionConfiguration);

            _deleteModificationFunctionConfiguration = modificationFunctionConfiguration;
        }

        public ModificationFunctionConfiguration InsertModificationFunctionConfiguration
        {
            get { return _insertModificationFunctionConfiguration; }
        }

        public ModificationFunctionConfiguration UpdateModificationFunctionConfiguration
        {
            get { return _updateModificationFunctionConfiguration; }
        }

        public ModificationFunctionConfiguration DeleteModificationFunctionConfiguration
        {
            get { return _deleteModificationFunctionConfiguration; }
        }

        public virtual void Configure(StorageEntityTypeModificationFunctionMapping modificationFunctionMapping)
        {
            DebugCheck.NotNull(modificationFunctionMapping);

            if (_insertModificationFunctionConfiguration != null)
            {
                _insertModificationFunctionConfiguration
                    .Configure(modificationFunctionMapping.InsertFunctionMapping);
            }

            if (_updateModificationFunctionConfiguration != null)
            {
                _updateModificationFunctionConfiguration
                    .Configure(modificationFunctionMapping.UpdateFunctionMapping);
            }

            if (_deleteModificationFunctionConfiguration != null)
            {
                _deleteModificationFunctionConfiguration
                    .Configure(modificationFunctionMapping.DeleteFunctionMapping);
            }
        }

        public void Configure(StorageAssociationSetModificationFunctionMapping modificationFunctionMapping)
        {
            DebugCheck.NotNull(modificationFunctionMapping);

            if (_insertModificationFunctionConfiguration != null)
            {
                _insertModificationFunctionConfiguration
                    .Configure(modificationFunctionMapping.InsertFunctionMapping);
            }

            if (_deleteModificationFunctionConfiguration != null)
            {
                _deleteModificationFunctionConfiguration
                    .Configure(modificationFunctionMapping.DeleteFunctionMapping);
            }
        }

        public bool IsCompatibleWith(ModificationFunctionsConfiguration other)
        {
            DebugCheck.NotNull(other);

            if ((_insertModificationFunctionConfiguration != null)
                && (other._insertModificationFunctionConfiguration != null)
                && !_insertModificationFunctionConfiguration.IsCompatibleWith(other._insertModificationFunctionConfiguration))
            {
                return false;
            }

            if ((_deleteModificationFunctionConfiguration != null)
                && (other._deleteModificationFunctionConfiguration != null)
                && !_deleteModificationFunctionConfiguration.IsCompatibleWith(other._deleteModificationFunctionConfiguration))
            {
                return false;
            }

            return true;
        }
    }
}

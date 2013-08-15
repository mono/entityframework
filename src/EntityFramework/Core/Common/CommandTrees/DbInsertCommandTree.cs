// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using ReadOnlyModificationClauses =
    System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Entity.Core.Common.CommandTrees.DbModificationClause>;

// System.Data.Common.ReadOnlyCollection conflicts

namespace System.Data.Entity.Core.Common.CommandTrees
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.CommandTrees.Internal;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Utilities;

    /// <summary>Represents a single row insert operation expressed as a command tree. This class cannot be inherited.</summary>
    /// <remarks>
    ///     Represents a single row insert operation expressed as a canonical command tree.
    ///     When the <see cref="Returning" /> property is set, the command returns a reader; otherwise,
    ///     it returns a scalar value indicating the number of rows affected.
    /// </remarks>
    public class DbInsertCommandTree : DbModificationCommandTree
    {
        private readonly ReadOnlyModificationClauses _setClauses;
        private readonly DbExpression _returning;

        internal DbInsertCommandTree()
        {
        }

        internal DbInsertCommandTree(
            MetadataWorkspace metadata, DataSpace dataSpace, DbExpressionBinding target, ReadOnlyModificationClauses setClauses,
            DbExpression returning)
            : base(metadata, dataSpace, target)
        {
            DebugCheck.NotNull(setClauses);
            // returning may be null

            _setClauses = setClauses;
            _returning = returning;
        }

        /// <summary>Gets the list of insert set clauses that define the insert operation. </summary>
        /// <returns>The list of insert set clauses that define the insert operation. </returns>
        public IList<DbModificationClause> SetClauses
        {
            get { return _setClauses; }
        }

        /// <summary>
        ///     Gets an <see cref="T:System.Data.Entity.Core.Common.CommandTrees.DbExpression" /> that specifies a projection of results to be returned based on the modified rows.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Data.Entity.Core.Common.CommandTrees.DbExpression" /> that specifies a projection of results to be returned based on the modified rows. null indicates that no results should be returned from this command.
        /// </returns>
        public DbExpression Returning
        {
            get { return _returning; }
        }

        public override DbCommandTreeKind CommandTreeKind
        {
            get { return DbCommandTreeKind.Insert; }
        }

        internal override bool HasReader
        {
            get { return null != Returning; }
        }

        internal override void DumpStructure(ExpressionDumper dumper)
        {
            base.DumpStructure(dumper);

            dumper.Begin("SetClauses");
            foreach (var clause in SetClauses)
            {
                if (null != clause)
                {
                    clause.DumpStructure(dumper);
                }
            }
            dumper.End("SetClauses");

            if (null != Returning)
            {
                dumper.Dump(Returning, "Returning");
            }
        }

        internal override string PrintTree(ExpressionPrinter printer)
        {
            return printer.Print(this);
        }
    }
}

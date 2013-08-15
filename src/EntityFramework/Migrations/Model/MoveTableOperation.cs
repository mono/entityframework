// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Model
{
    using System.Data.Entity.Utilities;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///     Represents moving a table from one schema to another.
    /// </summary>
    public class MoveTableOperation : MigrationOperation
    {
        private readonly string _name;
        private readonly string _newSchema;

        /// <summary>
        ///     Initializes a new instance of the MoveTableOperation class.
        /// </summary>
        /// <param name="name"> Name of the table to be moved. </param>
        /// <param name="newSchema"> Name of the schema to move the table to. </param>
        /// <param name="anonymousArguments"> Additional arguments that may be processed by providers. Use anonymous type syntax to specify arguments e.g. 'new { SampleArgument = "MyValue" }'. </param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public MoveTableOperation(string name, string newSchema, object anonymousArguments = null)
            : base(anonymousArguments)
        {
            Check.NotEmpty(name, "name");

            _name = name;
            _newSchema = newSchema;
        }

        /// <summary>
        ///     Gets the name of the table to be moved.
        /// </summary>
        public virtual string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///     Gets the name of the schema to move the table to.
        /// </summary>
        public virtual string NewSchema
        {
            get { return _newSchema; }
        }

        /// <summary>
        ///     Gets an operation that moves the table back to its original schema.
        /// </summary>
        public override MigrationOperation Inverse
        {
            get
            {
                var databaseName = _name.ToDatabaseName();

                return new MoveTableOperation(NewSchema + '.' + databaseName.Name, databaseName.Schema)
                           {
                               IsSystem = IsSystem
                           };
            }
        }

        /// <inheritdoc />
        public override bool IsDestructiveChange
        {
            get { return false; }
        }

        public string ContextKey { get; internal set; }

        public bool IsSystem { get; internal set; }

        public CreateTableOperation CreateTableOperation { get; internal set; }
    }
}

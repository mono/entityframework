// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Sql
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Config;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Migrations.Model;
    using System.Data.Entity.Migrations.Utilities;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.SqlServerCompact.Resources;
    using System.Data.Entity.SqlServerCompact.Utilities;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Provider to convert provider agnostic migration operations into SQL commands
    ///     that can be run against a Microsoft SQL Server Compact Edition database.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [DbProviderName("System.Data.SqlServerCe.4.0")]
    public class SqlCeMigrationSqlGenerator : MigrationSqlGenerator
    {
        internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffK";
        internal const string DateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        private const int DefaultMaxLength = 128;
        private const int DefaultNumericPrecision = 18;
        private const byte DefaultTimePrecision = 7;
        private const byte DefaultScale = 0;

        private static readonly Regex _sqlKeywordUpcaser
            = new Regex(
                @"^(insert|values|delete|where|update|declare|select|from|output|from|join|set|default\svalues|and)",
                RegexOptions.Multiline | RegexOptions.Compiled);

        private DbProviderServices _providerServices;
        private DbProviderManifest _providerManifest;

        private List<MigrationStatement> _statements;
        private HashSet<string> _generatedSchemas;

        /// <summary>
        ///     Converts a set of migration operations into Microsoft SQL Server specific SQL.
        /// </summary>
        /// <param name="migrationOperations"> The operations to be converted. </param>
        /// <param name="providerManifestToken"> Token representing the version of SQL Server being targeted (i.e. "2005", "2008"). </param>
        /// <returns> A list of SQL statements to be executed to perform the migration operations. </returns>
        public override IEnumerable<MigrationStatement> Generate(
            IEnumerable<MigrationOperation> migrationOperations, string providerManifestToken)
        {
            Check.NotNull(migrationOperations, "migrationOperations");
            Check.NotNull(providerManifestToken, "providerManifestToken");

            _statements = new List<MigrationStatement>();
            _generatedSchemas = new HashSet<string>();

            InitializeProviderServices(providerManifestToken);

            migrationOperations.Each<dynamic>(o => Generate(o));

            return _statements;
        }

        private void InitializeProviderServices(string providerManifestToken)
        {
            using (var connection = CreateConnection())
            {
                _providerServices = DbProviderServices.GetProviderServices(connection);
                _providerManifest = _providerServices.GetProviderManifest(providerManifestToken);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="MigrationOperation" />.
        ///     Allows derived providers to handle additional operation types.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="migrationOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(MigrationOperation migrationOperation)
        {
            Check.NotNull(migrationOperation, "migrationOperation");

            throw Error.SqlServerMigrationSqlGenerator_UnknownOperation(GetType().Name, migrationOperation.GetType().FullName);
        }

        /// <summary>
        ///     Creates an empty connection for the current provider.
        ///     Allows derived providers to use connection other than <see cref="SqlConnection" />.
        /// </summary>
        /// <returns> </returns>
        protected virtual DbConnection CreateConnection()
        {
            return DbConfiguration.GetService<DbProviderFactory>("System.Data.SqlServerCe.4.0").CreateConnection();
        }

        protected virtual void Generate(CreateProcedureOperation createProcedureOperation)
        {
        }

        protected virtual void Generate(DropProcedureOperation dropProcedureOperation)
        {
        }

        /// <summary>
        ///     Generates SQL for a <see cref="CreateTableOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(CreateTableOperation createTableOperation)
        {
            Check.NotNull(createTableOperation, "createTableOperation");

            var databaseName = createTableOperation.Name.ToDatabaseName();

            if (!string.IsNullOrWhiteSpace(databaseName.Schema))
            {
                if (!databaseName.Schema.EqualsIgnoreCase("dbo")
                    && !_generatedSchemas.Contains(databaseName.Schema))
                {
                    GenerateCreateSchema(databaseName.Schema);

                    _generatedSchemas.Add(databaseName.Schema);
                }
            }

            using (var writer = Writer())
            {
                WriteCreateTable(createTableOperation, writer);

                Statement(writer);
            }
        }

        private void WriteCreateTable(CreateTableOperation createTableOperation, IndentedTextWriter writer)
        {
            DebugCheck.NotNull(createTableOperation);
            DebugCheck.NotNull(writer);

            writer.WriteLine("CREATE TABLE " + Name(createTableOperation.Name) + " (");
            writer.Indent++;

            createTableOperation.Columns.Each(
                (c, i) =>
                    {
                        Generate(c, writer);

                        if (i < createTableOperation.Columns.Count - 1)
                        {
                            writer.WriteLine(",");
                        }
                    });

            if (createTableOperation.PrimaryKey != null)
            {
                writer.WriteLine(",");
                writer.Write("CONSTRAINT ");
                writer.Write(Quote(createTableOperation.PrimaryKey.Name));
                writer.Write(" PRIMARY KEY ");

                if (!createTableOperation.PrimaryKey.IsClustered)
                {
                    writer.Write("NONCLUSTERED ");
                }

                writer.Write("(");
                writer.Write(createTableOperation.PrimaryKey.Columns.Join(Quote));
                writer.WriteLine(")");
            }
            else
            {
                writer.WriteLine();
            }

            writer.Indent--;
            writer.Write(")");
        }

        /// <summary>
        ///     Generates SQL to mark a table as a system table.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="table"> The table to mark as a system table. </param>
        protected virtual void GenerateMakeSystemTable(CreateTableOperation createTableOperation, IndentedTextWriter writer)
        {
        }

        /// <summary>
        ///     Generates SQL to create a database schema.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createTableOperation"> The name of the schema to create. </param>
        protected virtual void GenerateCreateSchema(string schema)
        {
        }

        /// <summary>
        ///     Generates SQL for a <see cref="AddForeignKeyOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="addForeignKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AddForeignKeyOperation addForeignKeyOperation)
        {
            Check.NotNull(addForeignKeyOperation, "addForeignKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addForeignKeyOperation.DependentTable));
                writer.Write(" ADD CONSTRAINT ");
                writer.Write(Quote(addForeignKeyOperation.Name));
                writer.Write(" FOREIGN KEY (");
                writer.Write(addForeignKeyOperation.DependentColumns.Select(Quote).Join());
                writer.Write(") REFERENCES ");
                writer.Write(Name(addForeignKeyOperation.PrincipalTable));
                writer.Write(" (");
                writer.Write(addForeignKeyOperation.PrincipalColumns.Select(Quote).Join());
                writer.Write(")");

                if (addForeignKeyOperation.CascadeDelete)
                {
                    writer.Write(" ON DELETE CASCADE");
                }

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="DropForeignKeyOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropForeignKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropForeignKeyOperation dropForeignKeyOperation)
        {
            Check.NotNull(dropForeignKeyOperation, "dropForeignKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropForeignKeyOperation.DependentTable));
                writer.Write(" DROP CONSTRAINT ");
                writer.Write(Quote(dropForeignKeyOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="CreateIndexOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="createIndexOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(CreateIndexOperation createIndexOperation)
        {
            Check.NotNull(createIndexOperation, "createIndexOperation");

            using (var writer = Writer())
            {
                writer.Write("CREATE ");

                if (createIndexOperation.IsUnique)
                {
                    writer.Write("UNIQUE ");
                }

                if (createIndexOperation.IsClustered)
                {
                    writer.Write("CLUSTERED ");
                }

                writer.Write("INDEX ");
                writer.Write(Quote(createIndexOperation.Name));
                writer.Write(" ON ");
                writer.Write(Name(createIndexOperation.Table));
                writer.Write("(");
                writer.Write(createIndexOperation.Columns.Join(Quote));
                writer.Write(")");

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="DropIndexOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropIndexOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropIndexOperation dropIndexOperation)
        {
            using (var writer = Writer())
            {
                writer.Write("DROP INDEX ");
                writer.Write(Name(dropIndexOperation.Table));
                writer.Write(".");
                writer.Write(Quote(dropIndexOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="AddPrimaryKeyOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="addPrimaryKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AddPrimaryKeyOperation addPrimaryKeyOperation)
        {
            Check.NotNull(addPrimaryKeyOperation, "addPrimaryKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addPrimaryKeyOperation.Table));
                writer.Write(" ADD CONSTRAINT ");
                writer.Write(Quote(addPrimaryKeyOperation.Name));
                writer.Write(" PRIMARY KEY ");

                if (!addPrimaryKeyOperation.IsClustered)
                {
                    writer.Write("NONCLUSTERED ");
                }

                writer.Write("(");
                writer.Write(addPrimaryKeyOperation.Columns.Select(Quote).Join());
                writer.Write(")");

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="DropPrimaryKeyOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropPrimaryKeyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropPrimaryKeyOperation dropPrimaryKeyOperation)
        {
            Check.NotNull(dropPrimaryKeyOperation, "dropPrimaryKeyOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropPrimaryKeyOperation.Table));
                writer.Write(" DROP CONSTRAINT ");
                writer.Write(Quote(dropPrimaryKeyOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="AddColumnOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="addColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AddColumnOperation addColumnOperation)
        {
            Check.NotNull(addColumnOperation, "addColumnOperation");

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(addColumnOperation.Table));
                writer.Write(" ADD ");

                var column = addColumnOperation.Column;

                Generate(column, writer);

                if ((column.IsNullable != null)
                    && !column.IsNullable.Value
                    && (column.DefaultValue == null)
                    && (string.IsNullOrWhiteSpace(column.DefaultValueSql))
                    && !column.IsIdentity
                    && !column.IsTimestamp
                    && !column.StoreType.EqualsIgnoreCase("rowversion")
                    && !column.StoreType.EqualsIgnoreCase("timestamp"))
                {
                    writer.Write(" DEFAULT ");

                    if (column.Type == PrimitiveTypeKind.DateTime)
                    {
                        writer.Write(Generate(DateTime.Parse("1900-01-01 00:00:00", CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        writer.Write(Generate((dynamic)column.ClrDefaultValue));
                    }
                }

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="DropColumnOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropColumnOperation dropColumnOperation)
        {
            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(dropColumnOperation.Table));
                writer.Write(" DROP COLUMN ");
                writer.Write(Quote(dropColumnOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="AlterColumnOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="alterColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(AlterColumnOperation alterColumnOperation)
        {
            var column = alterColumnOperation.Column;

            using (var writer = Writer())
            {
                writer.Write("ALTER TABLE ");
                writer.Write(Name(alterColumnOperation.Table));
                writer.Write(" ALTER COLUMN ");
                writer.Write(Quote(column.Name));
                writer.Write(" ");
                writer.Write(BuildColumnType(column));

                if ((column.IsNullable != null)
                    && !column.IsNullable.Value)
                {
                    writer.Write(" NOT NULL");
                }

                Statement(writer);
            }

            if ((column.DefaultValue != null)
                || !string.IsNullOrWhiteSpace(column.DefaultValueSql))
            {
                using (var writer = Writer())
                {
                    writer.Write("ALTER TABLE ");
                    writer.Write(Name(alterColumnOperation.Table));
                    writer.Write(" ALTER COLUMN ");
                    writer.Write(Quote(column.Name));
                    writer.Write(" SET DEFAULT ");
                    writer.Write(
                        (column.DefaultValue != null)
                            ? Generate((dynamic)column.DefaultValue)
                            : column.DefaultValueSql
                        );

                    Statement(writer);
                }
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="DropTableOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="dropTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(DropTableOperation dropTableOperation)
        {
            Check.NotNull(dropTableOperation, "dropTableOperation");

            using (var writer = Writer())
            {
                writer.Write("DROP TABLE ");
                writer.Write(Name(dropTableOperation.Name));

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="SqlOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="sqlOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(SqlOperation sqlOperation)
        {
            Check.NotNull(sqlOperation, "sqlOperation");

            Statement(sqlOperation.Sql, sqlOperation.SuppressTransaction);
        }

        /// <summary>
        ///     Generates SQL for a <see cref="RenameColumnOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="renameColumnOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(RenameColumnOperation renameColumnOperation)
        {
            throw Error.SqlCeColumnRenameNotSupported();
        }

        /// <summary>
        ///     Generates SQL for a <see cref="RenameTableOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="renameTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(RenameTableOperation renameTableOperation)
        {
            using (var writer = Writer())
            {
                writer.Write("EXECUTE sp_rename @objname = N'");
                writer.Write(renameTableOperation.Name.ToDatabaseName().Name);
                writer.Write("', @newname = N'");
                writer.Write(renameTableOperation.NewName);
                writer.Write("', @objtype = N'OBJECT'");

                Statement(writer);
            }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="MoveTableOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="moveTableOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(MoveTableOperation moveTableOperation)
        {
        }

        private void Generate(ColumnModel column, IndentedTextWriter writer)
        {
            DebugCheck.NotNull(column);
            DebugCheck.NotNull(writer);

            writer.Write(Quote(column.Name));
            writer.Write(" ");
            writer.Write(BuildColumnType(column));

            if ((column.IsNullable != null)
                && !column.IsNullable.Value)
            {
                writer.Write(" NOT NULL");
            }

            if (column.DefaultValue != null)
            {
                writer.Write(" DEFAULT ");
                writer.Write(Generate((dynamic)column.DefaultValue));
            }
            else if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
            {
                writer.Write(" DEFAULT ");
                writer.Write(column.DefaultValueSql);
            }
            else if (column.IsIdentity)
            {
                if ((column.Type == PrimitiveTypeKind.Guid)
                    && (column.DefaultValue == null))
                {
                    writer.Write(" DEFAULT " + GuidColumnDefault);
                }
                else
                {
                    writer.Write(" IDENTITY");
                }
            }
        }

        /// <summary>
        ///     Returns the column default value to use for store-generated GUID columns when
        ///     no default value is explicitly specified in the migration.
        ///     Always returns newid() for SQL Compact.
        /// </summary>
        /// <value>The string newid().</value>
        protected virtual string GuidColumnDefault
        {
            get { return "newid()"; }
        }

        /// <summary>
        ///     Generates SQL for a <see cref="HistoryOperation" />.
        ///     Generated SQL should be added using the Statement method.
        /// </summary>
        /// <param name="historyOperation"> The operation to produce SQL for. </param>
        protected virtual void Generate(HistoryOperation historyOperation)
        {
            Check.NotNull(historyOperation, "historyOperation");

            using (var writer = Writer())
            {
                historyOperation.Commands.Each(
                    c =>
                        {
                            var sql = UpperCaseKeywords(c.CommandText);

                            // inline params
                            c.Parameters
                             .Cast<DbParameter>()
                             .Each(p => sql = sql.Replace(p.ParameterName, Generate((dynamic)p.Value)));

                            writer.Write(sql);
                        });

                Statement(writer);
            }
        }

        private static string UpperCaseKeywords(string commandText)
        {
            DebugCheck.NotEmpty(commandText);

            return _sqlKeywordUpcaser.Replace(commandText, m => m.Groups[1].Value.ToUpperInvariant());
        }

        /// <summary>
        ///     Generates SQL to specify a constant byte[] default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(byte[] defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");

            return "0x" + defaultValue.ToHexString();
        }

        /// <summary>
        ///     Generates SQL to specify a constant bool default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(bool defaultValue)
        {
            return defaultValue ? "1" : "0";
        }

        /// <summary>
        ///     Generates SQL to specify a constant DateTime default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DateTime defaultValue)
        {
            return "'" + defaultValue.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant DateTimeOffset default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DateTimeOffset defaultValue)
        {
            return "'" + defaultValue.ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture) + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant Guid default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(Guid defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant string default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(string defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");

            return "'" + defaultValue + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant TimeSpan default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(TimeSpan defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant geogrpahy default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DbGeography defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant geometry default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(DbGeometry defaultValue)
        {
            return "'" + defaultValue + "'";
        }

        /// <summary>
        ///     Generates SQL to specify a constant default value being set on a column.
        ///     This method just generates the actual value, not the SQL to set the default value.
        /// </summary>
        /// <param name="defaultValue"> The value to be set. </param>
        /// <returns> SQL representing the default value. </returns>
        protected virtual string Generate(object defaultValue)
        {
            Check.NotNull(defaultValue, "defaultValue");
            Debug.Assert(defaultValue.GetType().IsValueType);

            return string.Format(CultureInfo.InvariantCulture, "{0}", defaultValue);
        }

        /// <summary>
        ///     Generates SQL to specify the data type of a column.
        ///     This method just generates the actual type, not the SQL to create the column.
        /// </summary>
        /// <param name="defaultValue"> The definition of the column. </param>
        /// <returns> SQL representing the data type. </returns>
        protected virtual string BuildColumnType(ColumnModel columnModel)
        {
            Check.NotNull(columnModel, "columnModel");

            if (columnModel.IsTimestamp)
            {
                return "rowversion";
            }

            return BuildPropertyType(columnModel);
        }

        private string BuildPropertyType(PropertyModel propertyModel)
        {
            DebugCheck.NotNull(propertyModel);

            var originalStoreTypeName = propertyModel.StoreType;

            if (string.IsNullOrWhiteSpace(originalStoreTypeName))
            {
                var typeUsage = _providerManifest.GetStoreType(propertyModel.TypeUsage).EdmType;

                originalStoreTypeName = typeUsage.Name;
            }

            var storeTypeName = originalStoreTypeName;

            const string MaxSuffix = "(max)";

            if (storeTypeName.EndsWith(MaxSuffix, StringComparison.Ordinal))
            {
                storeTypeName = Quote(storeTypeName.Substring(0, storeTypeName.Length - MaxSuffix.Length)) + MaxSuffix;
            }
            else
            {
                storeTypeName = Quote(storeTypeName);
            }

            switch (originalStoreTypeName)
            {
                case "decimal":
                case "numeric":
                    storeTypeName += "(" + (propertyModel.Precision ?? DefaultNumericPrecision)
                                     + ", " + (propertyModel.Scale ?? DefaultScale) + ")";
                    break;
                case "datetime2":
                case "datetimeoffset":
                case "time":
                    storeTypeName += "(" + (propertyModel.Precision ?? DefaultTimePrecision) + ")";
                    break;
                case "binary":
                case "varbinary":
                case "nvarchar":
                case "varchar":
                case "char":
                case "nchar":
                    storeTypeName += "(" + (propertyModel.MaxLength ?? DefaultMaxLength) + ")";
                    break;
            }

            return storeTypeName;
        }

        /// <summary>
        ///     Generates a quoted name. The supplied name may or may not contain the schema.
        /// </summary>
        /// <param name="name"> The name to be quoted. </param>
        /// <returns> The quoted name. </returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#")]
        protected virtual string Name(string name)
        {
            return Quote(name.ToDatabaseName().Name);
        }

        /// <summary>
        ///     Quotes an identifier for SQL Server.
        /// </summary>
        /// <param name="identifier"> The identifier to be quoted. </param>
        /// <returns> The quoted identifier. </returns>
        protected virtual string Quote(string identifier)
        {
            return "[" + identifier + "]";
        }

        /// <summary>
        ///     Adds a new Statement to be executed against the database.
        /// </summary>
        /// <param name="sql"> The statement to be executed. </param>
        /// <param name="suppressTransaction"> Gets or sets a value indicating whether this statement should be performed outside of the transaction scope that is used to make the migration process transactional. If set to true, this operation will not be rolled back if the migration process fails. </param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void Statement(string sql, bool suppressTransaction = false, string batchTerminator = null)
        {
            Check.NotEmpty(sql, "sql");

            _statements.Add(
                new MigrationStatement
                    {
                        Sql = sql,
                        SuppressTransaction = suppressTransaction,
                        BatchTerminator = batchTerminator
                    });
        }

        /// <summary>
        ///     Gets a new <see cref="IndentedTextWriter" /> that can be used to build SQL.
        ///     This is just a helper method to create a writer. Writing to the writer will
        ///     not cause SQL to be registered for execution. You must pass the generated
        ///     SQL to the Statement method.
        /// </summary>
        /// <returns> An empty text writer to use for SQL generation. </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected static IndentedTextWriter Writer()
        {
            return new IndentedTextWriter(new StringWriter(CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Adds a new Statement to be executed against the database.
        /// </summary>
        /// <param name="writer"> The writer containing the SQL to be executed. </param>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        protected void Statement(IndentedTextWriter writer, string batchTerminator = null)
        {
            Check.NotNull(writer, "writer");

            Statement(writer.InnerWriter.ToString(), batchTerminator: batchTerminator);
        }
    }
}

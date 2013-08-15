﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.SqlServerCompact
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Config;
    using System.Data.Entity.Core;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Migrations.Sql;
    using System.Data.Entity.SqlServer;
    using System.Data.Entity.SqlServerCompact.Resources;
    using System.Linq;
    using SimpleModel;
    using Xunit;

    public class SqlCeProviderServicesTests : TestBase
    {
        [Fact]
        public void GetProviderManifest_throws_when_empty()
        {
            var ex = Assert.Throws<ProviderIncompatibleException>(
                () => SqlCeProviderServices.Instance.GetProviderManifest(string.Empty));

            // NOTE: Verifying base exception since DbProviderServices wraps errors
            var baseException = ex.GetBaseException();

            Assert.IsType<ArgumentException>(baseException);
            Assert.Equal(Strings.UnableToDetermineStoreVersion, baseException.Message);
        }

        [Fact]
        public void Has_ProviderInvariantNameAttribute()
        {
            Assert.Equal(
                "System.Data.SqlServerCe.4.0",
                DbProviderNameAttribute.GetFromType(typeof(SqlCeProviderServices)).Single().Name);
        }

        public class GetService
        {
            [Fact]
            public void GetService_resolves_the_SQL_CE_Migrations_SQL_generator()
            {
                Assert.IsType<SqlCeMigrationSqlGenerator>(
                    SqlCeProviderServices.Instance.GetService<MigrationSqlGenerator>("System.Data.SqlServerCe.4.0"));
            }

            [Fact]
            public void GetService_returns_null_for_SQL_generators_for_other_invariant_names()
            {
                Assert.Null(SqlCeProviderServices.Instance.GetService<MigrationSqlGenerator>("System.Data.SqlClient"));
            }

            [Fact]
            public void GetService_resolves_the_default_SQL_Compact_connection_factory()
            {
                Assert.IsType<SqlCeConnectionFactory>(SqlCeProviderServices.Instance.GetService<IDbConnectionFactory>());
            }
        }

        public class DbCreateDatabase : TestBase
        {
            [Fact]
            public void DbCreateDatabase_dispatches_commands_to_interceptors()
            {
                using (var context = new DdlDatabaseContext(SimpleCeConnection<DdlDatabaseContext>()))
                {
                    var storeItemCollection =
                        (StoreItemCollection)
                        ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace.GetItemCollection(DataSpace.SSpace);

                    context.Database.Delete();

                    var interceptor = new TestNonQueryInterceptor();
                    Interception.AddInterceptor(interceptor);
                    try
                    {
                        SqlCeProviderServices.Instance.CreateDatabase(context.Database.Connection, null, storeItemCollection);
                    }
                    finally
                    {
                        Interception.RemoveInterceptor(interceptor);
                    }

                    Assert.Equal(3, interceptor.CommandTexts.Count);

                    Assert.True(interceptor.CommandTexts.Any(t => t.StartsWith("CREATE TABLE \"Products\" ")));
                    Assert.True(interceptor.CommandTexts.Any(t => t.StartsWith("CREATE TABLE \"Categories\" ")));
                    Assert.True(interceptor.CommandTexts.Any(t => t.StartsWith("ALTER TABLE \"Products\" ADD CONSTRAINT ")));
                }
            }

            public class DdlDatabaseContext : SimpleModelContext
            {
                static DdlDatabaseContext()
                {
                    Database.SetInitializer<DdlDatabaseContext>(null);
                }

                public DdlDatabaseContext(DbConnection connection)
                    : base(connection, contextOwnsConnection: true)
                {

                }
            }

            public class TestNonQueryInterceptor : DbInterceptor
            {
                public readonly List<string> CommandTexts = new List<string>();

                public override void NonQueryExecuting(DbCommand command, DbInterceptionContext interceptionContext)
                {
                    CommandTexts.Add(command.CommandText);

                    Assert.Empty(interceptionContext.DbContexts);
                    Assert.Empty(interceptionContext.ObjectContexts);
                }
            }
        }
    }
}

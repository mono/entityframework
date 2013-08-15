﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations
{
    using System.Data.Entity.Migrations.Extensions;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Data.Entity.Migrations.Resources;
    using System.Diagnostics;

    internal class UpdateDatabaseCommand : MigrationsDomainCommand
    {
        public UpdateDatabaseCommand(
            string sourceMigration, string targetMigration, bool script, bool force, bool verbose)
        {
            Debug.Assert(
                string.IsNullOrWhiteSpace(sourceMigration) || script,
                "sourceMigration can only be specified when script is true");

            Execute(
                () =>
                {
                    var project = Project;

                    using (var facade = GetFacade())
                    {
                        if (script)
                        {
                            var sql = facade.ScriptUpdate(sourceMigration, targetMigration, force);

                            project.NewSqlFile(sql);
                        }
                        else
                        {
                            if (!verbose)
                            {
                                WriteLine(Strings.UpdateDatabaseCommand_VerboseInstructions);
                            }

                            try
                            {
                                facade.Update(targetMigration, force);
                            }
                            catch (AutomaticMigrationsDisabledException ex)
                            {
                                facade.LogWarningDelegate(ex.Message);
                                facade.LogWarningDelegate(Strings.AutomaticMigrationDisabledInfo);
                            }
                        }
                    }
                });
        }
    }
}

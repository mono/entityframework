﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations
{
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data.Common;
    using System.Data.Entity.Migrations.Design;
    using System.Data.Entity.Spatial;
    using System.Data.Entity.Utilities;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Resources;
    using System.Text;

    public class MigrationCompiler
    {
        private readonly CodeDomProvider _codeProvider;

        public MigrationCompiler(string language)
        {
            _codeProvider = CodeDomProvider.CreateProvider(language);
        }

        public Assembly Compile(string @namespace, params ScaffoldedMigration[] scaffoldedMigrations)
        {
            var options = new CompilerParameters
                              {
                                  GenerateExecutable = false,
                                  GenerateInMemory = true
                              };

            options.ReferencedAssemblies.Add(typeof(string).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(Expression).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(DbMigrator).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(DbContext).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(DbConnection).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(Component).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(MigrationCompiler).Assembly.Location);
            options.ReferencedAssemblies.Add(typeof(DbGeography).Assembly.Location);

            var embededResources = GenerateEmbeddedResources(scaffoldedMigrations, @namespace);
            embededResources.Each(r => options.EmbeddedResources.Add(r));

            var sources = scaffoldedMigrations.SelectMany(g => new[] { g.UserCode, g.DesignerCode });

            var compilerResults = _codeProvider.CompileAssemblyFromSource(options, sources.ToArray());

            embededResources.Each(File.Delete);

            if (compilerResults.Errors.Count > 0)
            {
                Console.WriteLine(scaffoldedMigrations.First().UserCode);

                throw new InvalidOperationException(BuildCompileErrorMessage(compilerResults.Errors));
            }

            return compilerResults.CompiledAssembly;
        }

        private static string BuildCompileErrorMessage(CompilerErrorCollection errors)
        {
            var stringBuilder = new StringBuilder();

            foreach (CompilerError error in errors)
            {
                stringBuilder.AppendLine(error.ToString());
            }

            return stringBuilder.ToString();
        }

        private static IEnumerable<string> GenerateEmbeddedResources(
            IEnumerable<ScaffoldedMigration> scaffoldedMigrations, string @namespace)
        {
            foreach (var scaffoldedMigration in scaffoldedMigrations)
            {
                var className = GetClassName(scaffoldedMigration.MigrationId);
                var embededResource = Path.Combine(
                    Path.GetTempPath(),
                    @namespace + "." + className + ".resources");

                using (var writer = new ResourceWriter(embededResource))
                {
                    scaffoldedMigration
                        .Resources
                        .Each(e => writer.AddResource(e.Key, e.Value));
                }

                yield return embededResource;
            }
        }

        private static string GetClassName(string migrationId)
        {
            return migrationId
                .Split(new[] { '_' }, 2)
                .Last()
                .Replace(" ", string.Empty);
        }
    }
}

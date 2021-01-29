using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SqlServer;

namespace Prototype1.Migration
{
    public static class MigrationManager
    {
        private static readonly IAnnouncer Announcer = new NullAnnouncer();

        private static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(480);

        public static void MigrateToLatest(string connectionString, string migrationAssemblyName)
        {
            var assembly = Assembly.Load(new AssemblyName(migrationAssemblyName));

            var migrationContext = new RunnerContext(Announcer) { Namespace = migrationAssemblyName + ".Migrations"};

            var options = new ProcessorOptions { PreviewOnly = false, Timeout = DefaultTimeout };
            var factory = new SqlServer2008ProcessorFactory();
            var processor = factory.Create(connectionString, Announcer, options);
            //var runner = new MigrationRunner(assembly, migrationContext, processor);

            //foreach (var a in runner.MigrationAssemblies.Assemblies)
            //    RunScriptGroup(ScriptRunningGroup.PreMigrationScript, a, processor);

            //runner.MigrateUp(true);

            //foreach (var a in runner.MigrationAssemblies.Assemblies)
            //    RunScriptGroup(ScriptRunningGroup.PostMigrationScript, a, processor);
        }

        private enum ScriptRunningGroup
        {
            PreMigrationScript,
            PostMigrationScript
        }

        private static readonly Dictionary<string, Dictionary<ScriptRunningGroup, List<string>>> EmbeddedScriptGroups = new Dictionary<string,Dictionary<ScriptRunningGroup,List<string>>>();

        private static Dictionary<ScriptRunningGroup, List<string>> GetEmbeddedScriptGroups(Assembly migrationAssembly)
        {
            if (!EmbeddedScriptGroups.ContainsKey(migrationAssembly.FullName))
            {
                var migrationScripts = migrationAssembly.GetManifestResourceNames().ToList();

                var enumType = typeof(ScriptRunningGroup);

                EmbeddedScriptGroups.Add(migrationAssembly.FullName,
                    (from groupName in Enum.GetValues(enumType).Cast<ScriptRunningGroup>()
                        from scripts in migrationScripts
                        where
                            scripts.StartsWith(string.Format("{0}.{1}", migrationAssembly.GetName().Name,
                                Enum.GetName(enumType, groupName)))
                        group scripts by groupName).ToDictionary(x => x.Key, x => x.ToList()));
            }
            return EmbeddedScriptGroups[migrationAssembly.FullName];
        }

        private static void RunScriptGroup(ScriptRunningGroup @group, Assembly migrationAssembly, IMigrationProcessor processor)
        {
            if (migrationAssembly == null)
            {
                migrationAssembly = typeof (MigrationManager).Assembly;
            }
            else
            {
                RunScriptGroup(@group, null, processor);
            }

            var scriptGroups = GetEmbeddedScriptGroups(migrationAssembly);
            List<string> value;
            if (!scriptGroups.TryGetValue(@group, out value)) return;

            foreach (
                var embeddedScript in
                    value.Select(
                        script =>
                            new ExecuteEmbeddedSqlScriptExpression
                            {
                                SqlScript = script,
                                MigrationAssemblies = new AssemblyCollection(new[] {migrationAssembly})
                            }))
            {
                Announcer.Say(string.Format("Running {0}: {1}", @group, embeddedScript.SqlScript));
                embeddedScript.ExecuteWith(processor);
            }
        }
    }
}

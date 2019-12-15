using JetBrains.Annotations;
using Netspark.CleanArchitecture.Scaffold.Extensions;
using Netspark.CleanArchitecture.Scaffold.Utils;
using Pluralize.NET.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class Scaffolder
    {
        private readonly YamlConfig _config;
        private readonly IVerbService _verbService;

        public Scaffolder([NotNull]ScaffolderOptions options, [NotNull]YamlConfig config, [NotNull]IVerbService verbService)
        {
            _config = MergeOptions(options, config);
            _verbService = verbService;
        }

        public void Run()
        {
            var templates = LoadTemplates();
            foreach(var domainNode in _config.Domains)
            {
                var commands = domainNode.Children.FirstOrDefault(c => c.Name == "Commands")?.Children;
                var queries = domainNode.Children.FirstOrDefault(c => c.Name == "Queries")?.Children;

                var commandFiles = GenerateCommands(commands, templates);
                var queryFiles = GenerateQueries(queries, templates);
                var commandTestFiles = GenerateCommandTests(commands, templates);
                var queryTestFiles = GenerateQueryTests(queries, templates);

                SaveFiles(commandFiles, _config);
                SaveFiles(queryFiles, _config);
                SaveFiles(commandTestFiles, _config);
                SaveFiles(queryTestFiles, _config);
            }
        }

        private YamlConfig MergeOptions(ScaffolderOptions options, YamlConfig config)
        {
            config.MergeStrategy = options.MergeStrategy;

            // set config folder
            var configFile = GetRootedPath(options.ConfigFile);
            config.ConfigFolder = Path.GetDirectoryName(configFile);

            // set output folder
            config.OutputFolder = GetRootedPath(options.OutputFolder);

            return config;
        }


        private IDictionary<ResourceTemplateType, ResourceTemplate> LoadTemplates() => GetType()
                .Assembly
                .GetManifestResourceNames()
                .Where(r => r.Contains(".Templates."))
                .Select(r => new ResourceTemplate(r, LoadEmbeddedTemplateResource(r)))
                .ToDictionary(t => t.Type);


        private void SaveFiles(IDictionary<string, string> filesOut, YamlConfig config)
        {
            var copy = new HashSet<string>();
            var skip = new HashSet<string>();
            foreach (var fileOut in filesOut)
            {
                var srcFile = GetSaveFilePath(config.ConfigFolder, fileOut.Key);
                var srcFolder = Path.GetDirectoryName(srcFile);

                var outFile = GetSaveFilePath(config.OutputFolder, fileOut.Key);
                var outFolder = Path.GetDirectoryName(outFile);

                var same = srcFolder == outFolder;

                switch (config.MergeStrategy)
                {
                    case MergeStrategy.Append:
                        if (!File.Exists(srcFile))
                        {
                            EnsureDirectory(outFolder);
                            File.WriteAllText(outFile, fileOut.Value);
                        }
                        break;
                    case MergeStrategy.Overwrite:
                        EnsureDirectory(outFolder);
                        File.WriteAllText(outFile, fileOut.Value);
                        break;
                    case MergeStrategy.Skip:
                        if (!skip.Contains(srcFolder) && (copy.Contains(srcFolder) || !Directory.Exists(srcFolder) || !Directory.GetFileSystemEntries(srcFolder).Any()))
                        {
                            EnsureDirectory(outFolder);
                            File.WriteAllText(outFile, fileOut.Value);
                            copy.Add(srcFolder);
                        }
                        else
                        {
                            skip.Add(srcFolder);
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Merge strategy '{_config.MergeStrategy}' is not yet implemented!");
                }
            }
        }

        #region Generation
        private IDictionary<string, string> GenerateQueryTests(IList<AppNode> queries, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (queries == null)
                return result;

            foreach (var queryNode in queries)
            {
                AddQueryTestTemplate(templates, result, queryNode);
            }
            return result;
        }

        private IDictionary<string, string> GenerateCommandTests(IList<AppNode> commands, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (commands == null)
                return result;

            foreach (var cmdNode in commands)
            {
                AddCommandTestTemplate(templates, result, cmdNode);
            }
            return result;
        }

        private IDictionary<string, string> GenerateQueries(IList<AppNode> queries, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (queries == null)
                return result;

            foreach (var queryNode in queries)
            {
                if (queryNode.Name.EndsWith("List"))
                {
                    AddDtoTemplate(templates, result, queryNode);
                    AddListHandlerTemplate(templates, result, queryNode);
                    AddListVmTemplate(templates, result, queryNode);
                }
                else
                {
                    AddDetailHandlerTemplate(templates, result, queryNode);
                    AddDetailVmTemplate(templates, result, queryNode);
                }

                AddQueryTemplate(templates, result, queryNode);
                AddQueryValidatorTemplate(templates, result, queryNode);
            }
            return result;
        }

        private IDictionary<string, string> GenerateCommands(IList<AppNode> commands, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (commands == null)
                return result;

            foreach(var cmdNode in commands)
            {
                AddCommandTemplate(templates, result, cmdNode);
                AddCommandHandlerTemplate(templates, result, cmdNode);
                AddCommandValidatorTemplate(templates, result, cmdNode);
                AddEventTemplate(templates, result, cmdNode);
            }
            return result;
        }

        private string AddDtoTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var dto = GetDtoPlaceholder(queryNode);
            var path = $"{queryNode.GetFullPath()}/{dto}.cs";
            var template = GetDtoTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetDtoTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppListDto];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var domainNsPlaceholder = $"{_config.Namespace}.Domain";
            var namespacePlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var dtoPlaceholder = GetDtoPlaceholder(queryNode);

            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.DomainNsPlaceholder, domainNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.DtoPlaceholder, dtoPlaceholder);

            return template;
        }

        private string AddQueryValidatorTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var path = $"{queryNode.GetFullPath()}/{queryNode.Name}QueryValidator.cs";
            var template = GetQueryValidatorTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetQueryValidatorTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppQueryValidator];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.Application.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var validatorPlaceholder = $"{queryNode.Name}QueryValidator";

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryPlaceholder);
            template.SetParameter(TemplateParameterType.ValidatorPlaceholder, validatorPlaceholder);

            return template;
        }


        private string AddQueryTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var path = $"{queryNode.GetFullPath()}/{queryNode.Name}Query.cs";
            var template = GetQueryTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetQueryTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppQuery];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.Application.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var vmPlaceholder = $"{queryNode.Name}Vm";

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryPlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);

            return template;
        }

        private string AddListVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var vm = GetQueryBaseName(queryNode, trimGet: true);
            var path = $"{queryNode.GetFullPath()}/{vm}Vm.cs";
            var template = GetListVmTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetListVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppListVm];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.Application.{queryNode.GetFullPath(".")}";
            var dtoPlaceholder = GetDtoPlaceholder(queryNode);
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}Vm";

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.DtoPlaceholder, dtoPlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);

            return template;
        }

        private string AddListHandlerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var path = $"{queryNode.GetFullPath()}/{queryNode.Name}QueryHandler.cs";
            var template = GetListHandlerTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetListHandlerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppListHandler];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var namespacePlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var handlerPlaceholder = $"{queryPlaceholder}Handler";
            var dtoPlaceholder = GetDtoPlaceholder(queryNode);
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}Vm";

            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.HandlerPlaceholder, handlerPlaceholder);
            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryPlaceholder);
            template.SetParameter(TemplateParameterType.DtoPlaceholder, dtoPlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);
            template.SetParameter(TemplateParameterType.DbContextPlaceholder, _config.DbContext);

            return template;
        }

        private string AddDetailHandlerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var path = $"{queryNode.GetFullPath()}/{queryNode.Name}QueryHandler.cs";
            var template = GetDetailHandlerTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetDetailHandlerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppDetailHandler];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var namespacePlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var handlerPlaceholder = $"{queryPlaceholder}Handler";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}Vm";

            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.HandlerPlaceholder, handlerPlaceholder);
            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryPlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);
            template.SetParameter(TemplateParameterType.DbContextPlaceholder, _config.DbContext);

            return template;
        }

        private string AddDetailVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var vm = GetQueryBaseName(queryNode, trimGet: true);
            var path = $"{queryNode.GetFullPath()}/{vm}Vm.cs";
            var template = GetDetailVmTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetDetailVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppDetailVm];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var domainNsPlaceholder = $"{_config.Namespace}.Domain";
            var namespacePlaceholder = $"{_config.Namespace}.Application.{queryNode.GetFullPath(".")}";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}Vm";

            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.DomainNsPlaceholder, domainNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);

            return template;
        }

        private static string GetDtoPlaceholder(AppNode queryNode)
        {
            var dtoPlaceholder = GetQueryBaseName(queryNode, trimList: true);
            dtoPlaceholder = new Pluralizer().Singularize(dtoPlaceholder);
            dtoPlaceholder = $"{dtoPlaceholder}Dto";
            return dtoPlaceholder;
        }

        private static string GetQueryBaseName(AppNode queryNode, bool trimGet = true, bool trimList = false)
        {
            var trim = queryNode.Name;
            trim = trimGet && trim.StartsWith("Get")
                            ? trim.Substring("Get".Length)
                            : trim;

            trim = trimList && trim.EndsWith("List")
                            ? trim.Substring(0, trim.Length - "List".Length)
                            : trim;
            return trim;
        }

        private string AddQueryTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            var path = $"{cmdNode.Parent.GetFullPath()}/{cmdNode.Name}QueryTests.cs";
            var template = GetQueryTestTemplate(templates, cmdNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetQueryTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.UnitTestQuery];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var persistNsPlaceholder = $"{_config.Namespace}.Persistence";
            var queryNsPlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var namespacePlaceholder = $"{appNsPlaceholder}.UnitTests.{queryNode.Parent.GetFullPath(".")}";

            var vmPlaceholder = queryNode.Name.StartsWith("Get") 
                ? queryNode.Name.Substring("Get".Length) 
                : queryNode.Name;
            vmPlaceholder = $"{vmPlaceholder}Vm";

            template.SetParameter(TemplateParameterType.QueryNsPlaceholder, queryNsPlaceholder);
            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.PersistenceNsPlaceholder, persistNsPlaceholder);

            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryNode.Name);// special case here without "Query" suffix
            template.SetParameter(TemplateParameterType.FixturePlaceholder, $"{queryNode.Name}QueryTests");
            template.SetParameter(TemplateParameterType.HandlerPlaceholder, $"{queryNode.Name}QueryHandler");
            template.SetParameter(TemplateParameterType.DbContextPlaceholder, GetQueryTestDbContext(_config.DbContext));
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);

            return template;
        }

        private string AddCommandTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            var path = $"{cmdNode.Parent.GetFullPath()}/{cmdNode.Name}CommandTests.cs";
            var template = GetCommandTestTemplate(templates, cmdNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetCommandTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode cmdNode)
        {
            var template = templates[ResourceTemplateType.UnitTestCommand];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var namespacePlaceholder = $"{appNsPlaceholder}.UnitTests.{cmdNode.Parent.GetFullPath(".")}";
            var commandNsPlaceholder = $"{appNsPlaceholder}.{cmdNode.GetFullPath(".")}";

            template.SetParameter(TemplateParameterType.CommandNsPlaceholder, commandNsPlaceholder);
            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);

            template.SetParameter(TemplateParameterType.CommandPlaceholder, $"{cmdNode.Name}Command");
            template.SetParameter(TemplateParameterType.FixturePlaceholder, $"{cmdNode.Name}CommandTests");
            template.SetParameter(TemplateParameterType.HandlerPlaceholder, $"{cmdNode.Name}CommandHandler");

            return template;
        }

        private string AddCommandTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            var path = $"{cmdNode.GetFullPath()}/{cmdNode.Name}Command.cs";
            var template = GetCommandTemplate(templates, cmdNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetCommandTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode cmdNode)
        {
            var template = templates[ResourceTemplateType.AppCommand];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.Application.{cmdNode.GetFullPath(".")}";
            template.SetParameter(TemplateParameterType.CommandPlaceholder, $"{cmdNode.Name}Command");
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            return template;
        }

        private string AddCommandValidatorTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            var path = $"{cmdNode.GetFullPath()}/{cmdNode.Name}CommandValidator.cs";
            var template = GetCommandValidatorTemplate(templates, cmdNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetCommandValidatorTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode cmdNode)
        {
            var template = templates[ResourceTemplateType.AppValidator];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.Application.{cmdNode.GetFullPath(".")}";
            template.SetParameter(TemplateParameterType.CommandPlaceholder, $"{cmdNode.Name}Command");
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.ValidatorPlaceholder, $"{cmdNode.Name}CommandValidator");

            return template;
        }



        private string AddCommandHandlerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            var path = $"{cmdNode.GetFullPath()}/{cmdNode.Name}CommandHandler.cs";
            var template = GetCommandHandlerTemplate(templates, cmdNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetCommandHandlerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode cmdNode)
        {
            var template = templates[ResourceTemplateType.AppHandler];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var domainNsPlaceholder = $"{_config.Namespace}.Domain";
            var namespacePlaceholder = $"{appNsPlaceholder}.{cmdNode.GetFullPath(".")}";

            template.SetParameter(TemplateParameterType.CommandPlaceholder, $"{cmdNode.Name}Command");
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.DomainNsPlaceholder, domainNsPlaceholder);
            template.SetParameter(TemplateParameterType.DbContextPlaceholder, _config.DbContext);

            template.SetParameter(TemplateParameterType.EventPlaceholder, GetEventNameOrUpdated(cmdNode.Name));
            template.SetParameter(TemplateParameterType.HandlerPlaceholder, $"{cmdNode.Name}CommandHandler");

            return template;
        }


        private string[] AddEventTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            if (cmdNode.Name.StartsWith("upsert", StringComparison.OrdinalIgnoreCase))
            {
                var cmdBase = cmdNode.Name.Substring("upsert".Length);

                var eventCreated = GetEventName("Create" + cmdBase);
                var createdPath = $"{cmdNode.GetFullPath()}/{eventCreated}.cs";
                var createdTemplate = GetEventTemplate(templates, cmdNode, eventCreated);
                result[createdPath] = createdTemplate.GetReplacedContent();

                var eventUpdated = GetEventName("Update" + cmdBase);
                var updatedPath = $"{cmdNode.GetFullPath()}/{eventUpdated}.cs";
                var updatedTemplate = GetEventTemplate(templates, cmdNode, eventUpdated);
                result[updatedPath] = updatedTemplate.GetReplacedContent();

                return new[] { createdPath, updatedPath };
            }
            else
            {
                var eventName = GetEventName(cmdNode.Name);
                var path = $"{cmdNode.GetFullPath()}/{eventName}.cs";
                var template = GetEventTemplate(templates, cmdNode, eventName);
                result[path] = template.GetReplacedContent();

                return new[] { path };
            }
        }

        private ResourceTemplate GetEventTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode cmdNode, string eventPlaceholder)
        {
            var template = templates[ResourceTemplateType.AppEvent];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.Application";
            var namespacePlaceholder = $"{appNsPlaceholder}.{cmdNode.GetFullPath(".")}";
            var handlerPlaceholder = $"{eventPlaceholder}EventHandler";

            template.SetParameter(TemplateParameterType.EventPlaceholder, eventPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.HandlerPlaceholder, handlerPlaceholder);

            return template;
        }

        #endregion

        #region Filesystem Helpers
        private static void EnsureDirectory(string outFolder)
        {
            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);
        }

        private string GetSaveFilePath(string baseFolder, string relativePath)
        {
            bool isTest = relativePath.EndsWith("Tests.cs");
            var subFolder = isTest ? "Application.UnitTests" : "Application";
            var path = isTest ? _config.TestsPath : _config.SrcPath;

            string result = PathUtils.GetAbsolutePath(baseFolder, path);
            result = Path.Combine(result, subFolder, relativePath);

            return result;
        }

        private string GetRootedPath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : PathUtils.GetAbsolutePath(path);
        }

        #endregion

        #region Naming Helpers
        private string GetEventNameOrUpdated(string cmdName)
        {
            if (cmdName.StartsWith("upsert", StringComparison.OrdinalIgnoreCase))
            {
                var cmdBase = cmdName.Substring("upsert".Length);
                return GetEventName("Update" + cmdBase);
            }
            else
            {
                return GetEventName(cmdName);
            }
        }

        private string GetEventName(string cmdName)
        {
            var words = cmdName.SplitIntoWords();

            var past = _verbService.GetPastSimpleForm(words[0]);
            string name = string.Join("", words.Skip(1)) + past;
            return name;
        }


        private string GetQueryTestDbContext(string dbContext)
        {
            if  (dbContext?.Length > 2)
            {
                return dbContext[0] == 'I' && char.IsUpper(dbContext[1])
                    ? dbContext.Substring(1)
                    : dbContext;
            }
            return dbContext;
        }

        #endregion

        private string LoadEmbeddedTemplateResource(string name)
        {
            using (var s = GetType().Assembly.GetManifestResourceStream(name))
            {
                using(var reader = new StreamReader(s))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}

using JetBrains.Annotations;
using Netspark.CleanArchitecture.Scaffold.Extensions;
using Netspark.CleanArchitecture.Scaffold.Utils;
using Pluralize.NET.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class Scaffolder
    {
        private readonly YamlConfig _config;
        private readonly IVerbService _verbService;

        public Scaffolder([NotNull] ScaffolderOptions options, [NotNull] YamlConfig config, [NotNull] IVerbService verbService)
        {
            _config = MergeOptions(options, config);
            _verbService = verbService;
        }

        public void Run()
        {
            var templates = LoadTemplates(_config.TemplatesVersion);
            var isRooted = _config.Domains.All(m => m.Name.StartsWithAny("Commands", "Queries"));
            foreach (var domainNode in _config.Domains)
            {
                var commands = domainNode.FindCommands();
                var queries = domainNode.FindQueries();

                var commandFiles = GenerateCommands(commands, templates);
                var queryFiles = GenerateQueries(queries, templates);
                var commandTestFiles = GenerateCommandTests(commands, templates);
                var queryTestFiles = GenerateQueryTests(queries, templates);
                var exampleFiles = GenerateExamples(domainNode, templates);
                var controllerFiles = isRooted
                    ? new Dictionary<string, ControllerFile>()
                    : GenerateControllers(domainNode, templates);

                SaveFiles(commandFiles, _config);
                SaveFiles(queryFiles, _config);
                SaveFiles(commandTestFiles, _config);
                SaveFiles(queryTestFiles, _config);
                SaveFiles(controllerFiles, _config);
                SaveFiles(exampleFiles, _config);
            }

            // generate rooted controllers
            if (!isRooted)
                return;

            var cmdRoot = _config.Domains.FirstOrDefault(m => m.Name.StartsWith("Commands"));
            var queryRoot = _config.Domains.FirstOrDefault(m => m.Name.StartsWith("Queries"));

            var rootedControllerFiles = GenerateControllers(cmdRoot, queryRoot, templates);

            SaveFiles(rootedControllerFiles, _config);
        }

        private YamlConfig MergeOptions(ScaffolderOptions options, YamlConfig config)
        {
            // copy pass-through options
            config.MergeStrategy = options.MergeStrategy;
            config.GenerateCommands = options.GenerateCommands;
            config.GenerateQueries = options.GenerateQueries;
            config.GenerateControllerActions = options.GenerateControllerActions;
            config.GenerateUnitTests = options.GenerateUnitTests;
            config.GenerateIntegrationTests = options.GenerateIntegrationTests;
            config.GenerateValidators = options.GenerateValidators;
            config.GenerateHandlers = options.GenerateHandlers;
            config.GenerateEvents = options.GenerateEvents;
            config.GenerateExamples = options.GenerateExamples;
            config.TemplatesVersion = options.TemplatesVersion;

            // set config folder
            var configFile = GetRootedPath(options.ConfigFile);
            config.ConfigFolder = Path.GetDirectoryName(configFile);

            // set output folder
            config.OutputFolder = GetRootedPath(options.OutputFolder);

            config.ApiUrlPrefix = config.ApiUrlPrefix?.TrimEnd('/');

            return config;
        }


        private IDictionary<ResourceTemplateType, ResourceTemplate> LoadTemplates(string version) => GetType()
                .Assembly
                .GetManifestResourceNames()
                .Where(r => r.Contains($".Templates.{version}."))
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

        private void SaveFiles(IDictionary<string, ControllerFile> filesOut, YamlConfig config)
        {
            if (config.MergeStrategy != MergeStrategy.Append)
            {
                SaveFiles(filesOut.ToDictionary(f => f.Key, f => f.Value.Content), config);
                return;
            }

            var copy = new HashSet<string>();
            var skip = new HashSet<string>();
            foreach (var fileOut in filesOut)
            {
                var srcFile = GetSaveFilePath(config.ConfigFolder, fileOut.Key);
                var srcFolder = Path.GetDirectoryName(srcFile);

                var outFile = GetSaveFilePath(config.OutputFolder, fileOut.Key);
                var outFolder = Path.GetDirectoryName(outFile);

                var same = srcFolder == outFolder;

                if (!File.Exists(srcFile))
                {
                    EnsureDirectory(outFolder);
                    File.WriteAllText(outFile, fileOut.Value.Content);
                }
                else
                {
                    var originalContent = File.ReadAllText(srcFile);
                    var outContent = fileOut.Value.AppendMissingActions(originalContent);
                    File.WriteAllText(outFile, outContent);
                }
            }
        }

        #region Generation

        private IDictionary<string, ControllerFile> GenerateControllers(AppNode domainNode, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, ControllerFile>();

            if (!_config.GenerateControllerActions)
                return result;

            var subDomains = domainNode.FindSubDomains();
            var baseFolder = domainNode.Name.StartsWithAny("Commands", "Queries")
                ? ""
                : $"{domainNode.Name}/";

            if (subDomains.Any())
            {
                // multidomain mode
                foreach (var subDomain in subDomains)
                {
                    var (template, actions, usings) = GetControllerTemplate(templates, subDomain, domainNode);
                    var path = $"{baseFolder}{subDomain.Name}Controller.cs";
                    result[path] = new ControllerFile(path, template.GetReplacedContent(), usings, actions);
                }
            }
            else
            {
                // single domain mode
                var (template, actions, usings) = GetControllerTemplate(templates, domainNode);
                var path = $"{baseFolder}{domainNode.Name}Controller.cs";
                result[path] = new ControllerFile(path, template.GetReplacedContent(), usings, actions);
            }

            return result;
        }

        private IDictionary<string, ControllerFile> GenerateControllers(AppNode cmdRoot, AppNode queryRoot, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, ControllerFile>();

            if (!_config.GenerateControllerActions)
                return result;

            var subDomains = cmdRoot
                .FindSubDomains()
                .Concat(queryRoot.FindSubDomains())
                .GroupBy(m => m.Name);

            if (subDomains.Any())
            {
                // multidomain mode
                foreach (var subDomain in subDomains)
                {
                    var (template, actions, usings) = GetControllerTemplate(templates, subDomain.Key, subDomain);
                    var path = $"{subDomain.Key}Controller.cs";
                    result[path] = new ControllerFile(path, template.GetReplacedContent(), usings, actions);
                }
            }

            return result;
        }

        private IDictionary<string, string> GenerateExamples(AppNode domainNode, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();

            if (!_config.GenerateExamples)
                return result;

            var subDomains = domainNode.FindSubDomains();
            if (subDomains.Any())
            {
                // multidomain mode
                foreach (var subDomain in subDomains)
                {
                    AddExampleTemplate(templates, result, subDomain);
                }
            }
            else
            {
                // single domain mode
                AddExampleTemplate(templates, result, domainNode);
            }

            return result;
        }

        private IDictionary<string, string> GenerateQueryTests(IList<AppNode> queries, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (queries == null)
                return result;

            foreach (var queryNode in queries)
            {
                if (_config.GenerateUnitTests)
                    AddQueryUnitTestTemplate(templates, result, queryNode);

                if (_config.GenerateIntegrationTests)
                    AddQueryIntegrationTestTemplate(templates, result, queryNode);
            }

            return result;
        }

        private IDictionary<string, string> GenerateCommandTests(IList<AppNode> commands, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (commands == null || !_config.GenerateUnitTests)
                return result;

            foreach (var cmdNode in commands)
            {
                AddCommandUnitTestTemplate(templates, result, cmdNode);
            }

            return result;
        }

        private IDictionary<string, string> GenerateQueries(IList<AppNode> queries, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            var needQueries = _config.GenerateQueries;
            var needHandlers = _config.GenerateHandlers;
            var needValidators = _config.GenerateValidators;

            if (queries == null)
                return result;

            foreach (var queryNode in queries)
            {
                if (queryNode.Name.EndsWithAny("List", "Grid"))
                {
                    AddDtoTemplate(templates, result, queryNode);

                    if (needHandlers)
                        AddListHandlerTemplate(templates, result, queryNode);

                    AddListVmTemplate(templates, result, queryNode);
                }
                else
                {
                    if (needHandlers)
                        AddDetailHandlerTemplate(templates, result, queryNode);

                    AddDetailVmTemplate(templates, result, queryNode);
                }

                if (needQueries)
                    AddQueryTemplate(templates, result, queryNode);

                if (needValidators)
                    AddQueryValidatorTemplate(templates, result, queryNode);
            }

            return result;
        }

        private IDictionary<string, string> GenerateCommands(IList<AppNode> commands, IDictionary<ResourceTemplateType, ResourceTemplate> templates)
        {
            var result = new Dictionary<string, string>();
            if (commands == null)
                return result;

            foreach (var cmdNode in commands)
            {
                if (_config.GenerateCommands)
                    AddCommandTemplate(templates, result, cmdNode);

                if (_config.GenerateHandlers)
                    AddCommandHandlerTemplate(templates, result, cmdNode);

                if (_config.GenerateValidators)
                    AddCommandValidatorTemplate(templates, result, cmdNode);

                if (_config.GenerateEvents)
                    AddEventTemplate(templates, result, cmdNode);
            }

            return result;
        }

        private string GetUsingsPlaceholder(IEnumerable<AppNode> operations)
        {
            var usings = new HashSet<string>();
            foreach (var operation in operations)
            {
                usings.Add($"using {_config.Namespace}.{_config.AppSuffix}.{operation.GetFullPath(".")};");
                usings.Add($"using {_config.Namespace}.{_config.AppSuffix}.{operation.Parent.GetFullPath(".")};");
            }

            return string.Join(Environment.NewLine, usings.OrderBy(u => u));
        }

        private (ResourceTemplate Template, IDictionary<string, string> Actions, string[] Usings) GetControllerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode domainNode, AppNode subdomainNode = null)
        {
            var template = templates[ResourceTemplateType.WebController];
            template.ResetParameters();

            var workNode = subdomainNode ?? domainNode;

            var queries = workNode.FindQueries();
            var commands = workNode.FindCommands();

            var queriesNsPlaceholder = GetUsingsPlaceholder(queries);
            var commandsNsPlaceholder = GetUsingsPlaceholder(commands);

            var webNsPlaceholder = $"{_config.Namespace}.{_config.UiSuffix}";
            var examplesNsPlaceholder = $"using {_config.UiSuffix}.Controllers.Examples;";

            var usings = string.Join(Environment.NewLine,
                    new[]
                    {
                        queriesNsPlaceholder,
                        commandsNsPlaceholder,
                        examplesNsPlaceholder
                    })
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var namespacePlaceholder = $"{webNsPlaceholder}.Controllers.{domainNode.Name}";

            var controllerPlaceholder = $"{workNode.Name}Controller";
            var actions = GetControllerActions(templates, workNode);
            var actionsPlaceholder = string.Join("", actions.Values);

            template.SetParameter(TemplateParameterType.CommandsNsPlaceholder, commandsNsPlaceholder);
            template.SetParameter(TemplateParameterType.QueriesNsPlaceholder, queriesNsPlaceholder);
            template.SetParameter(TemplateParameterType.WebNsPlaceholder, webNsPlaceholder);
            template.SetParameter(TemplateParameterType.ExampleNsPlaceholder, examplesNsPlaceholder);

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.ControllerPlaceholder, controllerPlaceholder);
            template.SetParameter(TemplateParameterType.ActionsPlaceholder, actionsPlaceholder);

            return (template, actions, usings);
        }

        private (ResourceTemplate Template, IDictionary<string, string> Actions, string[] Usings) GetControllerTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, string domainName, IEnumerable<AppNode> subDomains)
        {
            var template = templates[ResourceTemplateType.WebController];
            template.ResetParameters();

            var queries = subDomains.SelectMany(m => m.FindQueries());
            var commands = subDomains.SelectMany(m => m.FindCommands());

            var queriesNsPlaceholder = GetUsingsPlaceholder(queries);
            var commandsNsPlaceholder = GetUsingsPlaceholder(commands);

            var webNsPlaceholder = $"{_config.UiSuffix}";
            var examplesNsPlaceholder = _config.GenerateExamples
                ? $"using {_config.UiSuffix}.Controllers.Examples;"
                : "";

            var usings = string.Join(Environment.NewLine, 
                    new[] 
                    { 
                        queriesNsPlaceholder, 
                        commandsNsPlaceholder, 
                        examplesNsPlaceholder 
                    })
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var namespacePlaceholder = $"{webNsPlaceholder}.Controllers.{domainName}";

            var controllerPlaceholder = $"{domainName}Controller";

            var commandsAndQueries = commands.Concat(queries);
            var actions = GetControllerActions(templates, domainName, commandsAndQueries);
            var actionsPlaceholder = string.Join("", actions.Values);

            template.SetParameter(TemplateParameterType.CommandsNsPlaceholder, commandsNsPlaceholder);
            template.SetParameter(TemplateParameterType.QueriesNsPlaceholder, queriesNsPlaceholder);
            template.SetParameter(TemplateParameterType.WebNsPlaceholder, webNsPlaceholder);
            template.SetParameter(TemplateParameterType.ExampleNsPlaceholder, examplesNsPlaceholder);

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.ControllerPlaceholder, controllerPlaceholder);
            template.SetParameter(TemplateParameterType.ActionsPlaceholder, actionsPlaceholder);

            return (template, actions, usings);
        }

        private IDictionary<string, string> GetControllerActions(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode domainNode)
        {
            var actions = domainNode.FindCommands()
                .Union(domainNode.FindQueries());

            return GetControllerActions(templates, domainNode.Name, actions);
        }


        private IDictionary<string, string> GetControllerActions(IDictionary<ResourceTemplateType, ResourceTemplate> templates, string domainName, IEnumerable<AppNode> commandsAndQueries)
        {
            var pluralizer = new Pluralizer();
            var singular = pluralizer.Singularize(domainName);
            var plural = pluralizer.Pluralize(domainName);

            var actions = commandsAndQueries
                .Select(c =>
                {
                    var group = GetActionGroup(c, pluralizer);
                    return new ControllerActionGroup
                    {
                        Node = c,
                        Cut = group.Cut,
                        Sort = group.Plural.Replace(plural, ""),
                        Singular = group.Singular,
                        Plural = group.Plural,
                        DomainSingular = singular,
                        DomainPlural = plural
                    };
                })
                .GroupBy(a => a.Plural)
                // treat entity with domain name as primary
                .OrderBy(g => g.Key.Replace(plural, ""));

            var result = new Dictionary<string, string>();
            foreach (var actionGroup in actions)
            {
                foreach (var action in actionGroup.OrderBy(a => a.Node.IsCommand).ThenBy(a => a.Node.Name))
                {
                    var template = GetActionTemplate(templates, action);
                    result[action.Node.Name] = template.GetReplacedContent();
                }
            }

            return result;
        }

        private ResourceTemplate GetActionTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, ControllerActionGroup action)
        {
            switch (action.Node.NodeType)
            {
                case AppNodeType.Query:
                case AppNodeType.ListQuery:
                case AppNodeType.BoolQuery:
                    return GetQueryActionTemplate(templates, action);
                case AppNodeType.Command:
                    return GetCommandActionTemplate(templates, action, ResourceTemplateType.WebControllerCommand);
                case AppNodeType.InsertCommand:
                    return GetCommandActionTemplate(templates, action, ResourceTemplateType.WebControllerCreate);
                case AppNodeType.UpdateCommand:
                    return GetCommandActionTemplate(templates, action, ResourceTemplateType.WebControllerUpdate);
                case AppNodeType.DeleteCommand:
                    return GetCommandActionTemplate(templates, action, ResourceTemplateType.WebControllerDelete);
                default:
                    return null;
            }
        }

        private ResourceTemplate GetCommandActionTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, ControllerActionGroup action, ResourceTemplateType type)
        {
            var template = templates[type];
            template.ResetParameters();

            var routePlaceholder = GetCommandRoutePlaceholder(action.Node, action.DomainPlural, action.DomainSingular);

            var actionName = $"{action.Node.Name}{_config.ActionSuffix}";
            template.SetParameter(TemplateParameterType.RoutePlaceholder, routePlaceholder);
            template.SetParameter(TemplateParameterType.CommandPlaceholder, action.Node.Name);
            template.SetParameter(TemplateParameterType.ActionPlaceholder, actionName);

            return template;
        }

        private ResourceTemplate GetQueryActionTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, ControllerActionGroup action)
        {
            var template = templates[ResourceTemplateType.WebControllerGet];
            template.ResetParameters();

            var routePlaceholder = GetQueryRoutePlaceholder(action.Node, action.DomainPlural, action.DomainSingular);
            string vmPlaceholder = GetVmPlaceholder(action.Node);
            var actionName = $"{action.Node.Name}{_config.ActionSuffix}";

            template.SetParameter(TemplateParameterType.RoutePlaceholder, routePlaceholder);
            template.SetParameter(TemplateParameterType.QueryPlaceholder, action.Node.Name);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);
            template.SetParameter(TemplateParameterType.ActionPlaceholder, actionName);

            return template;
        }

        private string GetVmPlaceholder(AppNode node)
        {
            return $"{GetQueryBaseName(node, trimGet: true)}{_config.VmSuffix}";
        }

        private static string GetCommandRoutePlaceholder(AppNode node, string domainPlural, string domainSingular)
        {
            var baseName = node.Name;
            var words = baseName.SplitIntoWords();
            var parts = words.Skip(1)
                             .Select(w => w == domainPlural
                                            || w == domainSingular
                                         ? "/"
                                         : $"{w.ToLower()}-")
                             .ToList();

            parts.Add("/");
            parts.Add(words[0].ToLower());

            return CleanRoutePlaceholder(parts);
        }

        private static string GetQueryRoutePlaceholder(AppNode node, string domainPlural, string domainSingular)
        {
            var baseName = GetQueryBaseName(node, trimGet: true);
            var words = baseName.SplitIntoWords()
                                .Select(w => w == domainPlural
                                            || w == domainSingular
                                         ? "/"
                                         : $"{w.ToLower()}-");

            return CleanRoutePlaceholder(words);
        }

        private static string CleanRoutePlaceholder(IEnumerable<string> words)
        {
            var routePlaceholder = string.Join("", words)
                .Replace("-/", "/")
                .TrimEnd('-', '/')
                .TrimStart('/');

            return routePlaceholder;
        }


        private (string Cut, string Singular, string Plural) GetActionGroup(AppNode action, Pluralizer pluralizer = null)
        {
            pluralizer = pluralizer ?? new Pluralizer();
            switch (action.NodeType)
            {
                case AppNodeType.Query:
                case AppNodeType.ListQuery:
                    var cut = GetQueryBaseName(action, trimGet: true, trimList: true);
                    return (cut, pluralizer.Singularize(cut), pluralizer.Pluralize(cut));
                case AppNodeType.BoolQuery:
                    // Is... supposed form: Question + Entity + Adjectives
                    // others supposed form: Question + Action + Entity
                    cut = string.Join("", action.NameWords.Skip(2));
                    return (cut, pluralizer.Singularize(cut), pluralizer.Pluralize(cut));
                case AppNodeType.Command:
                case AppNodeType.InsertCommand:
                case AppNodeType.UpdateCommand:
                case AppNodeType.DeleteCommand:
                    // supposed form: Action + Entity
                    cut = string.Join("", action.NameWords.Skip(1));
                    return (cut, pluralizer.Singularize(cut), pluralizer.Pluralize(cut));
                default:
                    return (null, null, null);
            }
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

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
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

            var namespacePlaceholder = $"{_config.Namespace}.{_config.AppSuffix}.{queryNode.GetFullPath(".")}";
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

            var namespacePlaceholder = $"{_config.Namespace}.{_config.AppSuffix}.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryPlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);

            return template;
        }

        private string AddListVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var vm = GetQueryBaseName(queryNode, trimGet: true);
            var path = $"{queryNode.GetFullPath()}/{vm}{_config.VmSuffix}.cs";
            var template = GetListVmTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetListVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppListVm];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.{_config.AppSuffix}.{queryNode.GetFullPath(".")}";
            var dtoPlaceholder = GetDtoPlaceholder(queryNode);
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

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

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
            var namespacePlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var handlerPlaceholder = $"{queryPlaceholder}Handler";
            var dtoPlaceholder = GetDtoPlaceholder(queryNode);
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

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

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
            var namespacePlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var queryPlaceholder = $"{queryNode.Name}Query";
            var handlerPlaceholder = $"{queryPlaceholder}Handler";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

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
            var path = $"{queryNode.GetFullPath()}/{vm}{_config.VmSuffix}.cs";
            var template = GetDetailVmTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetDetailVmTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.AppDetailVm];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
            var domainNsPlaceholder = $"{_config.Namespace}.Domain";
            var namespacePlaceholder = $"{_config.Namespace}.{_config.AppSuffix}.{queryNode.GetFullPath(".")}";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

            template.SetParameter(TemplateParameterType.ApplicationNsPlaceholder, appNsPlaceholder);
            template.SetParameter(TemplateParameterType.DomainNsPlaceholder, domainNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);

            return template;
        }

        private string GetDtoPlaceholder(AppNode queryNode)
        {
            var dtoPlaceholder = GetQueryBaseName(queryNode, trimList: true);
            dtoPlaceholder = new Pluralizer().Singularize(dtoPlaceholder);
            dtoPlaceholder = $"{dtoPlaceholder}{_config.DtoSuffix}";
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

        private string AddQueryUnitTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var path = $"{queryNode.Parent.GetFullPath()}/{queryNode.Name}QueryTests.cs";
            var template = GetQueryTestTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetQueryTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.UnitTestQuery];
            template.ResetParameters();

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
            var persistNsPlaceholder = $"{_config.Namespace}.Persistence";
            var queryNsPlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var namespacePlaceholder = $"{appNsPlaceholder}.UnitTests.{queryNode.Parent.GetFullPath(".")}";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

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

        private string AddQueryIntegrationTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            var path = $"Controllers/{queryNode.Parent.GetFullPath()}/{queryNode.Name}QueryIntegrationTest.cs";
            var template = GetQueryIntegrationTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private ResourceTemplate GetQueryIntegrationTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode queryNode)
        {
            var template = templates[ResourceTemplateType.IntegrationTestQuery];
            template.ResetParameters();

            var urlPlaceholder = GetQueryApiUrl(queryNode);
            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
            var webNsPlaceholder = $"{_config.Namespace}.{_config.UiSuffix}";
            var queryNsPlaceholder = $"{appNsPlaceholder}.{queryNode.GetFullPath(".")}";
            var queriesNsPlaceholder = $"{appNsPlaceholder}.{queryNode.Parent.GetFullPath(".")}";
            var namespacePlaceholder = $"{webNsPlaceholder}.IntegrationTests.Controllers.{queryNode.GetDomainName()}";
            var vmPlaceholder = $"{GetQueryBaseName(queryNode, trimGet: true)}{_config.VmSuffix}";

            template.SetParameter(TemplateParameterType.QueryNsPlaceholder, queryNsPlaceholder);
            template.SetParameter(TemplateParameterType.QueriesNsPlaceholder, queriesNsPlaceholder);
            template.SetParameter(TemplateParameterType.WebNsPlaceholder, webNsPlaceholder);
            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);

            template.SetParameter(TemplateParameterType.QueryPlaceholder, queryNode.Name);// special case here without "Query" suffix
            template.SetParameter(TemplateParameterType.FixturePlaceholder, $"{queryNode.Name}QueryTests");
            template.SetParameter(TemplateParameterType.VmPlaceholder, vmPlaceholder);
            template.SetParameter(TemplateParameterType.UrlPlaceholder, urlPlaceholder);

            return template;
        }

        private string GetQueryApiUrl(AppNode queryNode)
        {
            var domain = queryNode.GetDomainName();
            var pluralizer = new Pluralizer();
            var route = GetQueryRoutePlaceholder(queryNode, pluralizer.Pluralize(domain), pluralizer.Singularize(domain));
            var suffix = queryNode.IsListQuery ? "" : "1";
            var url = $"{_config.ApiUrlPrefix}/{domain.ToLower()}/{route}/{suffix}"
                        .TrimEnd('/')
                        .Replace("//", "/");

            return url;
        }

        private string AddCommandUnitTestTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
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

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
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


        private void AddExampleTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode subDomain)
        {
            foreach (var command in subDomain.FindCommands())
                AddCommandExampleTemplate(templates, result, command);

            foreach (var query in subDomain.FindQueries())
                AddQueryExampleTemplate(templates, result, query);
        }

        private string AddCommandExampleTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode cmdNode)
        {
            if (_config.TemplatesVersion == "v1")
                return null;

            var path = $"{cmdNode.Name}CommandExample.cs";
            var template = GetExampleTemplate(templates, cmdNode);
            result[path] = template.GetReplacedContent();
            return path;
        }

        private void AddQueryExampleTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, Dictionary<string, string> result, AppNode queryNode)
        {
            if (_config.TemplatesVersion == "v1")
                return;

            var path = $"{queryNode.Name}QueryExample.cs";
            var template = GetExampleTemplate(templates, queryNode);
            result[path] = template.GetReplacedContent();

            var vmPlaceholder = GetVmPlaceholder(queryNode);
            var vmPath = $"{vmPlaceholder}Example.cs";
            template = GetResponseExampleTemplate(templates, queryNode);
            result[vmPath] = template.GetReplacedContent();
        }


        private ResourceTemplate GetExampleTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode node)
        {
            var template = templates[ResourceTemplateType.WebExampleRequest];
            template.ResetParameters();

            var namespacePlaceholder = GetExamplesNs();
            var usingPlaceholder = $"using {_config.Namespace}.{_config.AppSuffix}.{node.GetFullPath(".")};";
            var suffix = node.IsCommand ? "Command" : "Query";
            var title = string.Join(' ', node.Name.SplitIntoWords());

            template.SetParameter(TemplateParameterType.QueriesNsPlaceholder, node.IsQuery ? usingPlaceholder : "");
            template.SetParameter(TemplateParameterType.CommandsNsPlaceholder, node.IsCommand ? usingPlaceholder : "");

            template.SetParameter(TemplateParameterType.RequestPlaceholder, $"{node.Name}{suffix}");
            template.SetParameter(TemplateParameterType.TitlePlaceholder, $"{title} Example");

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);

            return template;
        }

        private ResourceTemplate GetResponseExampleTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode node)
        {
            var template = templates[ResourceTemplateType.WebExampleResponse];
            template.ResetParameters();

            var namespacePlaceholder = GetExamplesNs();
            var usingPlaceholder = $"using {_config.Namespace}.{_config.AppSuffix}.{node.GetFullPath(".")};";
            var vm = GetVmPlaceholder(node);
            var title = string.Join(' ', vm.SplitIntoWords());

            template.SetParameter(TemplateParameterType.QueriesNsPlaceholder, node.IsQuery ? usingPlaceholder : "");
            template.SetParameter(TemplateParameterType.CommandsNsPlaceholder, node.IsCommand ? usingPlaceholder : "");

            template.SetParameter(TemplateParameterType.ResponsePlaceholder, vm);
            template.SetParameter(TemplateParameterType.TitlePlaceholder, $"{title} Example");

            template.SetParameter(TemplateParameterType.NamespacePlaceholder, namespacePlaceholder);

            return template;
        }


        private string GetExamplesNs() => $"{_config.UiSuffix}.Controllers.Examples";

        private ResourceTemplate GetCommandTemplate(IDictionary<ResourceTemplateType, ResourceTemplate> templates, AppNode cmdNode)
        {
            var template = templates[ResourceTemplateType.AppCommand];
            template.ResetParameters();

            var namespacePlaceholder = $"{_config.Namespace}.{_config.AppSuffix}.{cmdNode.GetFullPath(".")}";
            template.SetParameter(TemplateParameterType.ResultPlaceholder, cmdNode.IsInsertCommand ? "string" : "Unit");
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

            var namespacePlaceholder = $"{_config.Namespace}.{_config.AppSuffix}.{cmdNode.GetFullPath(".")}";
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

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
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

            var appNsPlaceholder = $"{_config.Namespace}.{_config.AppSuffix}";
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
            var saveType = GetSavePathType(relativePath);
            string subFolder;
            string path;
            switch (saveType)
            {
                case SavePathType.Application:
                    subFolder = _config.AppPath;
                    path = _config.SrcPath;
                    break;
                case SavePathType.Controller:
                    subFolder = Path.Combine(_config.UiPath, "Controllers");
                    path = _config.SrcPath;
                    break;
                case SavePathType.UnitTest:
                    subFolder = $"{_config.Namespace}.{_config.AppSuffix}.UnitTests";
                    path = _config.TestsPath;
                    break;
                case SavePathType.IntegrationTest:
                    subFolder = $"{_config.Namespace}.{_config.UiSuffix}.IntegrationTests";
                    path = _config.TestsPath;
                    break;
                case SavePathType.Example:
                    subFolder = Path.Combine(_config.UiPath, "Controllers", "Examples");
                    path = _config.SrcPath;
                    break;
                default:
                    throw new NotSupportedException(saveType.ToString());
            }

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

        private static readonly string[] _integrationSuffixes =
        {
            "TestsIntegration.cs",
            "TestIntegration.cs",
            "IntegrationTest.cs",
            "IntegrationTests.cs"
        };
        private static readonly string[] _unitSuffixes =
        {
            "Tests.cs",
            "Test.cs",
        };

        private SavePathType GetSavePathType(string path)
        {
            if (path.EndsWithAny(_integrationSuffixes))
                return SavePathType.IntegrationTest;

            if (path.EndsWithAny(_unitSuffixes))
                return SavePathType.UnitTest;

            if (path.EndsWith("Controller.cs"))
                return SavePathType.Controller;

            if (path.EndsWith("Example.cs"))
                return SavePathType.Example;

            return SavePathType.Application;
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
            if (dbContext?.Length > 2)
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
                using (var reader = new StreamReader(s))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private enum SavePathType
        {
            Application,
            Controller,
            UnitTest,
            IntegrationTest,
            Example
        }

    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class Scaffolder
    {
        private readonly ScaffolderOptions _options;

        public Scaffolder(ScaffolderOptions options)
        {
            _options = options;
        }

        public void Run(YamlConfig config)
        {
            var templates = LoadTemplates();

        }

        private IDictionary<string, string> LoadTemplates() => GetType()
                .Assembly
                .GetManifestResourceNames()
                .Where(r => r.Contains(".Templates."))
                .ToDictionary(r => r, LoadTemplateResource);

        private string LoadTemplateResource(string name)
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

    public class ResourceTemplate
    {
        private static readonly IDictionary<string, ResourceTemplateType> Types =
            new Dictionary<string, ResourceTemplateType>()
        {
            {"App.Command.Command.cs", ResourceTemplateType.AppCommand },
            {"App.Command.Event.cs", ResourceTemplateType.AppEvent },
            {"App.Command.Handler.cs", ResourceTemplateType.AppHandler },
            {"App.Command.Vaidator.cs", ResourceTemplateType.AppValidator },

            {"App.Query.Detail.Query.cs", ResourceTemplateType.AppDetailQuery },
            {"App.Query.Detail.Handler.cs", ResourceTemplateType.AppDetailHandler },
            {"App.Query.Detail.Validator.cs", ResourceTemplateType.AppDetailValidator },
            {"App.Query.Detail.Vm.cs", ResourceTemplateType.AppDetailVm },

            {"App.Query.List.Query.cs", ResourceTemplateType.AppListQuery },
            {"App.Query.List.Handler.cs", ResourceTemplateType.AppListHandler },
            {"App.Query.List.Validator.cs", ResourceTemplateType.AppListValidator },
            {"App.Query.List.Vm.cs", ResourceTemplateType.AppListVm },
            {"App.Query.List.Dto.cs", ResourceTemplateType.AppListDto },

            {"UnitTest.Command.CommandTests.cs", ResourceTemplateType.UnitTestCommand },
            {"UnitTest.Query.QueryTests.cs", ResourceTemplateType.UnitTestQuery },
        };

        private readonly IDictionary<TemplateParameterType, string> Parameters;

        public ResourceTemplate(string name, string content)
        {
            Name = name;
            Content = content;
            Type = DetectType(name);
            Parameters = LoadParameters(content);
        }

        public void SetParameter(TemplateParameterType type, string value)
        {
            if (Parameters.ContainsKey(type))
                Parameters[type] = value;
        }

        private IDictionary<TemplateParameterType, string> LoadParameters(string content)
        {
            return Enum.GetValues(typeof(TemplateParameterType))
                .Cast<TemplateParameterType>()
                .Where(p => content.Contains(p.ToString()))
                .ToDictionary(p => p, p => string.Empty);
        }

        private ResourceTemplateType DetectType(string name)
        {
            return Types.First(s => name.Contains(s.Key)).Value;
        }

        public string Content { get; }
        public string Name { get; }
        public ResourceTemplateType Type { get; }
        
        
    }

    public enum ResourceTemplateType
    {
        AppCommand,
        AppEvent,
        AppHandler,
        AppValidator,

        AppDetailQuery,
        AppDetailHandler,
        AppDetailValidator,
        AppDetailVm,

        AppListDto,
        AppListHandler,
        AppListQuery,
        AppListValidator,
        AppListVm,

        UnitTestCommand,
        UnitTestQuery
    }

    public enum TemplateParameterType 
    {
        PersistenceNsPlaceholder,
        CommandNsPlaceholder,
        QueryNsPlaceholder,
        DomainNsPlaceholder,
        ApplicationNsPlaceholder,
        NamespacePlaceholder,

        CommandPlaceholder,
        QueryPlaceholder,
        VmPlaceholder,
        DtoPlaceholder,
        DbContextPlaceholder,
        EventPlaceholder,
        HandlerPlaceholder,
        ValidatorPlaceholder,
        FixturePlaceholder,

    }
}

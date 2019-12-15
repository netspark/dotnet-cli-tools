using System;
using System.Collections.Generic;
using System.Linq;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class ResourceTemplate
    {
        private static readonly IDictionary<string, ResourceTemplateType> Types =
            new Dictionary<string, ResourceTemplateType>()
        {
            {"App.Command.Command.cs", ResourceTemplateType.AppCommand },
            {"App.Command.Event.cs", ResourceTemplateType.AppEvent },
            {"App.Command.Handler.cs", ResourceTemplateType.AppHandler },
            {"App.Command.Validator.cs", ResourceTemplateType.AppValidator },

            {"App.Query.Query.cs", ResourceTemplateType.AppQuery },
            {"App.Query.Validator.cs", ResourceTemplateType.AppQueryValidator },

            {"App.Query.Detail.Handler.cs", ResourceTemplateType.AppDetailHandler },
            {"App.Query.Detail.Vm.cs", ResourceTemplateType.AppDetailVm },

            {"App.Query.List.Handler.cs", ResourceTemplateType.AppListHandler },
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

        public void ResetParameters()
        {
            foreach (var key in Parameters.Keys.ToArray())
                Parameters[key] = null;
        }

        public string GetReplacedContent()
        {
            EnsureHasParameters();

            var content = Content;
            foreach (var param in Parameters)
                content = content.Replace(param.Key.ToString(), param.Value);

            return content;
        }

        public IEnumerable<TemplateParameterType> GetMissingParameters() => Parameters
            .Where(p => p.Value == null)
            .Select(p => p.Key);

        public string Content { get; }
        public string Name { get; }
        public ResourceTemplateType Type { get; }
        public bool HasMissingParameters => GetMissingParameters().Any();

        private ResourceTemplateType DetectType(string name)
        {
            return Types.First(s => name.Contains(s.Key)).Value;
        }

        private IDictionary<TemplateParameterType, string> LoadParameters(string content)
        {
            return Enum.GetValues(typeof(TemplateParameterType))
                .Cast<TemplateParameterType>()
                .Where(p => content.Contains(p.ToString()))
                .ToDictionary(p => p, p => string.Empty);
        }

        private void EnsureHasParameters()
        {
            if (HasMissingParameters)
            {
                var missing = string.Join(",", GetMissingParameters().Select(s => s.ToString()));
                throw new Exception($"Paramter values should be specified: {missing}");
            }
        }
    }
}

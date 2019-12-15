using System.Collections.Generic;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class AppNode
    {
        public string Name { get; set; }
        public YamlNodeType Type { get; set; }
        public IList<AppNode> Children { get; set; } = new List<AppNode>();
        public AppNode Parent { get; set; }

        public string GetFullPath(string separator = "/")
        {
            var sb = new Stack<string>();
            sb.Push(Name);

            var node = this;
            while (node.Parent != null)
            {
                node = node.Parent;
                sb.Push(node.Name);
            }

            return string.Join(separator, sb.ToArray());
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

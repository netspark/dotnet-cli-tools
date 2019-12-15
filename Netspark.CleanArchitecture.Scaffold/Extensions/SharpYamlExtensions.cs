using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Netspark.CleanArchitecture.Scaffold.Extensions
{
    public static class SharpYamlExtensions
    {
        public static YamlConfig DeserializeYaml(string yamlDoc)
        {
            // Setup the input
            var input = new StringReader(yamlDoc);

            // Load the stream
            var yaml = new YamlStream();
            yaml.Load(input);

            // Examine the stream
            var domainsIndex = yaml.Documents.Count - 1;
            var config = domainsIndex == 1 
                ? new Serializer().Deserialize<YamlConfig>(yamlDoc.Split("---")[1])
                : new YamlConfig();

            var root = (YamlMappingNode)yaml.Documents[domainsIndex].RootNode;
            config.Domains = GetNodeChildren(root, null);

            return config;
        }

        private static IList<AppNode> GetNodeChildren(this YamlNode yamlParent, AppNode appParent)
        {
            var nodes = new List<AppNode>();
            switch (yamlParent)
            {
                case YamlMappingNode map:
                    foreach (var mapNode in map)
                    {
                        var mNode = new AppNode
                        {
                            Name = mapNode.Key.ToString(),
                            Parent = appParent,
                        };

                        mNode.Children = GetNodeChildren(mapNode.Value, mNode);
                        nodes.Add(mNode);
                    }
                break;
                case YamlSequenceNode seq:
                    foreach (var seqNode in seq)
                    {
                        var ssNode = new AppNode
                        {
                            Name = seqNode.ToString(),
                            Parent = appParent,
                        };

                        ssNode.Children = GetNodeChildren(seqNode, ssNode);
                        nodes.Add(ssNode);
                    }
                break;
                case YamlScalarNode scalar:
                    //var scNode = new AppNode
                    //{
                    //    Name = scalar.Value,
                    //    Parent = appParent,
                    //};
                    //appParent.Children.Add(scNode);
                break;
            }

            return nodes;
        }
    }
}

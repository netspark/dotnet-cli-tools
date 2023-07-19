using Netspark.CleanArchitecture.Scaffold.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class AppNode
    {
        public static string[] ListQueryPrefixes = { "GetAll" };
        public static string[] BoolQueryPrefixes = { 
            "Can", "Could", "Is", "Are", 
            "Should", "Would", "Will", 
            "Has", "Have", "Had",
            "May", "Might",

        };
        public static string[] InsertCommandPrefixes = { "Add", "Insert", "Upsert", "Create" };
        public static string[] UpdateCommandPrefixes = { "Update", "Upsert", "Save" };
        public static string[] DeleteCommandPrefixes = { "Delete", "Remove", "Drop", "Destroy", "Terminate" };

        private string _name;
        private string[] _nameWords = new string[] { };

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                _nameWords = value.SplitIntoWords();
            }
        }

        public string[] NameWords => _nameWords;
        public IList<AppNode> Children { get; set; } = new List<AppNode>();
        public AppNode Parent { get; set; }

        public AppNode Root 
        { 
            get 
            {
                var root = this;
                while(root.Parent != null)
                {
                    root = root.Parent;
                }
                return root;
            } 
        }

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

        public IList<AppNode> FindCommands()
        {
            var result = new List<AppNode>();
            if (GetFullPath().Contains("Commands"))
            {
                foreach(var child in Children)
                {
                    if (child.Children.Count == 0)
                        result.Add(child);
                    else
                        result.AddRange(child.FindCommands());
                }
            }
            else
            {
                foreach (var child in Children)
                {
                    result.AddRange(child.FindCommands());
                }
            }

            return result;
        }

        public IList<AppNode> FindQueries()
        {
            var result = new List<AppNode>();
            if (GetFullPath().Contains("Queries"))
            {
                foreach (var child in Children)
                {
                    if (child.Children.Count == 0)
                        result.Add(child);
                    else
                        result.AddRange(child.FindQueries());
                }
            }
            else
            {
                foreach (var child in Children)
                {
                    result.AddRange(child.FindQueries());
                }
            }

            return result;
        }

        public string GetDomainName()
        {
            var node = this;
            while (node.Parent != null)
            {
                node = node.Parent;
            }

            return node.Name;
        }

        public IList<AppNode> FindSubDomains()
        {
            var result = new List<AppNode>();
            foreach (var child in Children)
            {
                if (!child.Name.Contains("Queries") && !child.Name.Contains("Commands"))
                {
                    result.Add(child);
                }
            }
            return result;
        }

        public bool IsQuery => (Root?.GetFullPath().Contains("Queries") ?? false) && Children.Count == 0;
        public bool IsListQuery => IsQuery
            && (Name.EndsWith("List")
                || ListQueryPrefixes.Any(s => Name.StartsWith(s)));

        public bool IsBoolQuery => IsQuery && BoolQueryPrefixes.Contains(_nameWords.First());

        public bool IsCommand => (Root?.GetFullPath().Contains("Commands") ?? false) && Children.Count == 0;

        public bool IsInsertCommand => IsCommand && InsertCommandPrefixes.Contains(_nameWords.First());
        public bool IsUpdateCommand => IsCommand && UpdateCommandPrefixes.Contains(_nameWords.First());
        public bool IsDeleteCommand => IsCommand && DeleteCommandPrefixes.Contains(_nameWords.First());

        public AppNodeType NodeType => DetectNodeType();

        public override string ToString()
        {
            return Name;
        }

        private AppNodeType DetectNodeType()
        {
            if (IsBoolQuery)
                return AppNodeType.BoolQuery;

            if (IsListQuery)
                return AppNodeType.ListQuery;

            if (IsQuery)
                return AppNodeType.Query;

            if (IsInsertCommand)
                return AppNodeType.InsertCommand;

            if (IsUpdateCommand)
                return AppNodeType.UpdateCommand;

            if (IsDeleteCommand)
                return AppNodeType.DeleteCommand;

            if (IsCommand)
                return AppNodeType.Command;


            return AppNodeType.Other;
        }
    }
}

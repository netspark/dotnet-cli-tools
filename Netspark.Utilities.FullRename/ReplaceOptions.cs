﻿using System.IO;
using System.Linq;

namespace Netspark.Utilities.FullRename
{
    public class ReplaceOptions
    {
        public string Target { get; set; }
        public string Search { get; set; }
        public string Replace { get; set; }
        public bool IgnoreCase { get; set; }
        public bool Multiterm { get; set; }

        public string Extensions { get; set; } = "cs csproj sln md ps1 sh json cshtml";

        public string[] ReplaceExtensions => Extensions?
            .Replace(".", "")
            .Split(',', ' ', ';')
            .Select(e => $".{e}")
            .ToArray(); 

        public bool Verbose { get; set; }

        public TargetType? GetTargetType() => File.Exists(Target)
            ? FullRename.TargetType.File
            : Directory.Exists(Target) 
                ? FullRename.TargetType.Directory 
                : (TargetType?)null;
    }

    public enum TargetType
    {
        File, 
        Directory
    }
}

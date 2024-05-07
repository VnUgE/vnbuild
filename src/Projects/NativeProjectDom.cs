using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Projects
{
    internal sealed class NativeProjectDom : IProjectData
    {
        private Dictionary<string, string> _properties;

        internal NativeProjectDom()
        {
            _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string? this[string index] => _properties.GetValueOrDefault(index);

        public string? Description => this["description"];
        public string? Authors => this["author"];
        public string? Copyright => this["copyright"];
        public string? VersionString => this["version"];
        public string? CompanyName => this["company"];
        public string? Product => this["name"];
        public string? RepoUrl => this["repository"];
        public string? OutputDir => this["output_dir"];

        public string[] GetProjectRefs()
        {
            return Array.Empty<string>();
        }

        public void Load(Stream stream)
        {
            //Read the json file
            using JsonDocument doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            //Clear old properties
            _properties.Clear();

            //Load new properties that are strings only
            foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
            {
                if(prop.Value.ValueKind == JsonValueKind.String)
                {
                    _properties[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }
    }
}
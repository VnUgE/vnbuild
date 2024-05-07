
using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Projects
{

    internal sealed class DotnetProjectDom : IProjectData
    {
        private readonly XmlDocument _dom;

        public DotnetProjectDom()
        {
            _dom = new();
        }

        public void Load(Stream stream)
        {
            _dom.Load(stream);
        }

        private XmlNode? project => _dom["Project"];

        public string? Description => GetProperty("Description");
        public string? Authors => GetProperty("Authors");
        public string? Copyright => GetProperty("Copyright");
        public string? VersionString => GetProperty("Version");
        public string? CompanyName => GetProperty("Company");
        public string? Product => GetProperty("Product");
        public string? RepoUrl => GetProperty("RepositoryUrl");

        public string? this[string index] => GetProperty(index);

        public string? GetProperty(string name) => GetItemAtPath($"PropertyGroup/{name}");

        public string? GetItemAtPath(string path) => project!.SelectSingleNode(path)?.InnerText;

        public string[] GetProjectRefs()
        {
            //Get the item group attr
            XmlNodeList? projectRefs = project?.SelectNodes("ItemGroup/ProjectReference");

            if (projectRefs != null)
            {
                List<string> refs = new();
                foreach (XmlNode projRef in projectRefs)
                {
                    //Get the project ref via its include attribute
                    refs.Add(projRef.Attributes["Include"].Value!);
                }
                return refs.ToArray();
            }
            return Array.Empty<string>();
        }
    }
}
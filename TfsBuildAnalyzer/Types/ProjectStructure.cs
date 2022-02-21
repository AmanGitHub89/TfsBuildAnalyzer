using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using TfsBuildAnalyzer.Utilities;


namespace TfsBuildAnalyzer.Types
{
    /*
     * Do not remove private set on properties and blank constructors here. These are used to set values by newtonsoft.
     */
    public class ProjectStructure
    {
        public List<ProjectCatalogNode> ProjectCatalogNodeList { get; set; } = new List<ProjectCatalogNode>();

        public void Save()
        {
            TfsBuildAnalyzerConfiguration.ProjectStructure = this;
        }
    }

    public class ProjectCatalogNode
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Guid Id { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Name { get; private set; }

        public ProjectCatalogNode()
        {

        }
        public ProjectCatalogNode(CatalogNode catalogNode)
        {
            Id = new Guid(catalogNode.Resource.Properties["InstanceId"]);
            Name = catalogNode.Resource.DisplayName;
        }

        public List<ProjectInfoNode> ProjectInfoNodes { get; set; } = new List<ProjectInfoNode>();

        /// <summary>
        /// Not to be used anywhere, except for debugging as it cannot be serialized
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public CatalogNode TfsCatalogNode { get; }
    }

    public class ProjectInfoNode
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Guid Id { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public Guid CatalogId { get; private set; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string Name { get; private set; }

        public List<BuildType> BuildTypes { get; set; } = new List<BuildType>();

        public ProjectInfoNode()
        {

        }
        public ProjectInfoNode(Guid id, Guid catalogId, TeamProject tfsProjectInfo)
        {
            Id = id;
            CatalogId = catalogId;
            Name = tfsProjectInfo.Name;
            TfsProjectInfo = tfsProjectInfo;
        }

        public ProjectCatalogNode GetCatalogNode(ProjectStructure projectStructure)
        {
            return projectStructure.ProjectCatalogNodeList.First(x => x.Id == CatalogId);
        }

        /// <summary>
        /// Not to be used anywhere, except for debugging as it cannot be serialized
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public TeamProject TfsProjectInfo { get; }
    }

    internal class ProjectStructureJsonContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);

            if (member.DeclaringType == typeof(ProjectCatalogNode))
            {
                switch (prop.PropertyName)
                {
                    case "Id":
                    case "Name":
                        prop.Writable = true;
                        break;
                }
            }
            else if (member.DeclaringType == typeof(ProjectInfoNode))
            {
                switch (prop.PropertyName)
                {
                    case "Id":
                    case "CatalogId":
                    case "Name":
                        prop.Writable = true;
                        break;
                }
            }

            return prop;
        }
    }
}

using System;

using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.WebApi;

using Newtonsoft.Json;


namespace TfsBuildAnalyzer.Types
{
    public class BuildType : IEquatable<BuildType>
    {
        public Guid CollectionId { get; set; }
        public string CatalogName { get; set; }
        public string TeamProjectName { get; set; }
        public string BuildName { get; set; }
        public bool IsFavorite { get; set; }
        public string Url { get; set; }

        public int VNextBuildDefinitionId { get; set; }
        public Guid VNextBuildDefinitionProjectId { get; set; }

        /// <summary>
        /// Do not use anywhere in code, except for debugging as this cannot be serialized
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public BuildDefinitionReference VNextBuildDefinition { get; set; }

        public bool Equals(BuildType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CollectionId.Equals(other.CollectionId) && TeamProjectName == other.TeamProjectName && BuildName == other.BuildName;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BuildType) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CollectionId.GetHashCode();
                hashCode = (hashCode * 397) ^ (TeamProjectName != null ? TeamProjectName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (BuildName != null ? BuildName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    internal class CompletedBuild
    {
        public string BuildName { get; set; }
        public string BuildNumber { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateCompleted { get; set; }
        public DateTime? QueueTime { get; set; }
        public string RequestedBy { get; set; }
        public string BuildStatus { get; set; }
        public string DropLocation { get; set; }
        public string Url { get; set; }

        public Build VNextBuild { get; set; }
    }
}

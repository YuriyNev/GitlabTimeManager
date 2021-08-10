using System;
using GitLabApiClient.Models.Projects.Responses;
using Newtonsoft.Json;

namespace GitLabTimeManager.Services
{
    public class LabelEvent
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("label")]
        public Label Label { get; set; }
    }
}

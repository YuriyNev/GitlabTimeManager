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
        [JsonProperty("user")]
        public EventUser User { get; set; }
        [JsonProperty("action")]
        public EventAction Action { get; set; }
    }

    /*    "user": {
      "id": 1,
      "name": "Administrator",
      "username": "root",
      "state": "active",
      "avatar_url": "https://www.gravatar.com/avatar/e64c7d89f26bd1972efa854d13d7dd61?s=80&d=identicon",
      "web_url": "http://gitlab.example.com/root"
    }*/

    public enum EventAction
    {
        [JsonProperty("add")]
        Add,
        [JsonProperty("remove")]
        Remove
    }

    public class EventUser
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("username")]
        public string UserName { get; set; }
    }
}

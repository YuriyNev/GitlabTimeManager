using System.Collections.Generic;
using Newtonsoft.Json;

namespace GitLabTimeManager.Services
{
    public static class SdkJsonSettings
    {
        public static JsonSerializerSettings Create()
        {
            return new()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                Converters = new List<JsonConverter>
                {
                    new PointFConverter(),
                }
            };
        }
    }
}
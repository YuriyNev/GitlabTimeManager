using System;
using System.Drawing;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitLabTimeManager.Services
{
    public class PointFConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(PointF);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var pointF = (PointF)value;

            JValue jv = new JValue($"{pointF.X.ToString(CultureInfo.InvariantCulture)}, {pointF.Y.ToString(CultureInfo.InvariantCulture)}");
            jv.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jTokenString = JToken.Load(reader).ToString();
            return Converters.ConvertToPointF(jTokenString);
        }
    }
}
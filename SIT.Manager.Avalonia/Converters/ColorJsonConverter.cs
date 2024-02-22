using Avalonia.Media;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SIT.Manager.Avalonia.Converters
{
    public class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            string colorString = reader.GetString() ?? Colors.White.ToString();
            Color color = Color.Parse(colorString);
            return color;
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
            string colorString = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
            writer.WriteStringValue(colorString);
        }
    }
}

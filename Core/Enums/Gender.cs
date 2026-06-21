using System.Text.Json.Serialization;

namespace MIO.Core.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Gender
{
    Male = 1,
    Female = 2
}

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MIO.API.Json;

/// <summary>
/// Serializes every <see cref="DateTime"/> as ISO 8601 UTC with a trailing 'Z'
/// (e.g. "2026-06-19T12:12:00.0000000Z"), and parses incoming values as UTC.
///
/// Why: the app persists UTC wall-clock times, but EF Core reads them back with
/// <see cref="DateTimeKind.Unspecified"/>. Without this converter they serialize
/// with no timezone marker, so JavaScript's <c>new Date(...)</c> treats them as
/// local time and contest status (Ended/Ongoing) is computed against the wrong
/// instant. Registered globally in Program.cs, so no DTO or service changes.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return default;

        return DateTime.Parse(
            raw,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        writer.WriteStringValue(utc.ToString("O", CultureInfo.InvariantCulture));
    }
}

/// <summary>Nullable counterpart of <see cref="UtcDateTimeConverter"/>.</summary>
public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return Inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        Inner.Write(writer, value.Value, options);
    }
}

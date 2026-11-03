using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paypal;

/// <summary>
/// Shared JsonSerializerOptions for the entire test suite.
/// Ensures consistent serialization behaviour and avoids allocating
/// a new options instance on every Serialize/Deserialize call.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Text.Json.Serialization;

namespace HostsParser;

[JsonSerializable(typeof(Settings))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCortex {
  public class DCEvent {
    [JsonPropertyName("event_datetime")]
    public DateTime EventDateTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "event";

    [JsonPropertyName("kingdom")]
    public string? Kingdom { get; set; }

    [JsonPropertyName("phylum")]
    public string? Phylum { get; set; }

    [JsonPropertyName("class")]
    public string? Class { get; set; }

    [JsonPropertyName("order")]
    public string? Order { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("genus")]
    public string? Genus { get; set; }

    [JsonPropertyName("species")]
    public string? Species { get; set; }

    [JsonPropertyName("float1")]
    public double? Float1 { get; set; }

    [JsonPropertyName("float2")]
    public double? Float2 { get; set; }

    [JsonPropertyName("float3")]
    public double? Float3 { get; set; }

    [JsonPropertyName("float4")]
    public double? Float4 { get; set; }

    [JsonPropertyName("spend_curreny")]
    public string? SpendCurrency { get; set; }

    [JsonPropertyName("spend_amount")]
    public double? SpendAmount { get; set; }

    [JsonPropertyName("spend_type")]
    public string? SpendType { get; set; }

    [JsonPropertyName("event_index")]
    public int EventIndex { get; set; }
  }
  public class DCLogEvent {
    [JsonPropertyName("event_datetime")]
    public DateTime EventDateTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("log_line")]
    public string? LogLine { get; set; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("log_level")]
    public string? LogLevel { get; set; }

    [JsonPropertyName("remote_address")]
    public string? RemoteAddress { get; set; }

    [JsonPropertyName("response_bytes")]
    public double? ResponseBytes { get; set; }

    [JsonPropertyName("response_ms")]
    public double? ResponseMs { get; set; }
  }
}

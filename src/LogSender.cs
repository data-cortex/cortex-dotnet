using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataCortex;

internal class LogSender : Sender<DCLogEvent> {
  internal readonly JsonSerializerOptions JSON_OPTIONS =
      new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = {
          new CustomDateTimeConverter(),
          new CustomLogEventConverter(),
        }
      };

  public LogSender(DCClient dc) : base("LogSender", "/1/app_log", dc) { }
  internal override string MakePayload(ImmutableList<DCLogEvent> list) {
    var r =
        new Dictionary<string, object> {
          ["api_key"] = _dc._apiKey,
          ["app_ver"] = _dc._appVersion,
          ["device_tag"] = _dc._deviceTag,
          ["os"] = _dc._os,
          ["os_ver"] = _dc._osVersion,
          ["device_family"] = _dc._deviceFamily,
          ["device_type"] = _dc._deviceType,
          ["language"] = _dc._language,
          ["country"] = _dc._country,
        };
    if (_dc._userTag != null) {
      r["user_tag"] = _dc._userTag;
    }
    r["events"] = list;
    var json = JsonSerializer.Serialize(r, JSON_OPTIONS);
    return json;
  }
  public class CustomLogEventConverter : JsonConverter<DCLogEvent> {
    public override void Write(Utf8JsonWriter writer, DCLogEvent value,
                               JsonSerializerOptions options) {
      writer.WriteStartObject();
      writer.WritePropertyName("event_datetime");
      JsonSerializer.Serialize(writer, value.EventDateTime, options);
      if (value.Hostname != null) {
        writer.WritePropertyName("hostname");
        JsonSerializer.Serialize(writer, value.Hostname, options);
      }
      if (value.Filename != null) {
        writer.WritePropertyName("filename");
        JsonSerializer.Serialize(writer, value.Filename, options);
      }
      if (value.LogLevel != null) {
        writer.WritePropertyName("log_level");
        JsonSerializer.Serialize(writer, value.LogLevel, options);
      }
      if (value.DeviceTag != null) {
        writer.WritePropertyName("device_tag");
        JsonSerializer.Serialize(writer, value.DeviceTag, options);
      }
      if (value.UserTag != null) {
        writer.WritePropertyName("user_tag");
        JsonSerializer.Serialize(writer, value.UserTag, options);
      }
      if (value.RemoteAddress != null) {
        writer.WritePropertyName("remote_address");
        JsonSerializer.Serialize(writer, value.RemoteAddress, options);
      }
      if (value.ResponseBytes != null) {
        writer.WritePropertyName("response_bytes");
        JsonSerializer.Serialize(writer, value.ResponseBytes, options);
      }
      if (value.ResponseMs != null) {
        writer.WritePropertyName("response_ms");
        JsonSerializer.Serialize(writer, value.ResponseMs, options);
      }
      if (value.LogLine != null) {
        writer.WritePropertyName("log_line");
        JsonSerializer.Serialize(writer, value.LogLine, options);
      }
      writer.WriteEndObject();
    }
    public override DCLogEvent Read(ref Utf8JsonReader reader, Type typeToConvert,
                                  JsonSerializerOptions options) {
      throw new NotImplementedException("Deserialization not needed");
    }
  }
}

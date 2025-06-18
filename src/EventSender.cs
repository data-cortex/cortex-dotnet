using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataCortex;

internal class EventSender : Sender<DCAllEvent> {
  internal readonly JsonSerializerOptions JSON_OPTIONS =
      new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new CustomDateTimeConverter(), new CustomEventConverter() }
      };

  private const int DAU_INTERVAL = 10 * 60;
  private int _eventIndex = 1;

  public EventSender(DCClient dc) : base("EventSender", "/1/track", dc) { }
  public override void AddEventQueue(DCAllEvent e) {
    if (e.EventIndex == null) {
      e.EventIndex = _eventIndex++;
    }
    base.AddEventQueue(e);
  }
  internal override string MakePayload(ImmutableList<DCAllEvent> list) {
    var r =
        new Dictionary<string, object> {
          ["api_key"] = _dc._apiKey,
          ["app_ver"] = _dc._appVersion,
          ["device_family"] = _dc._deviceFamily,
          ["os_ver"] = _dc._osVersion,
          ["device_tag"] = _dc._deviceTag,
          ["device_type"] = _dc._deviceType,
          ["language"] = _dc._language,
          ["country"] = _dc._country,
          ["os"] = _dc._os
        };
    if (_dc._userTag != null) {
      r["user_tag"] = _dc._userTag;
    }
    if (_dc._facebookTag != null) {
      r["facebook_tag"] = _dc._facebookTag;
    }
    if (_dc._twitterTag != null) {
      r["twitter_tag"] = _dc._twitterTag;
    }
    if (_dc._googleTag != null) {
      r["google_tag"] = _dc._googleTag;
    }
    if (_dc._gameCenterTag != null) {
      r["game_center_tag"] = _dc._gameCenterTag;
    }
    if (_dc._serverVersion != null) {
      r["server_ver"] = _dc._serverVersion;
    }
    if (_dc._configVersion != null) {
      r["config_ver"] = _dc._configVersion;
    }
    r["events"] = list;
    var json = JsonSerializer.Serialize(r, JSON_OPTIONS);
    return json;
  }
  public class CustomEventConverter : JsonConverter<DCAllEvent> {
    public override void Write(Utf8JsonWriter writer, DCAllEvent value,
                               JsonSerializerOptions options) {
      writer.WriteStartObject();
      writer.WritePropertyName("event_datetime");
      JsonSerializer.Serialize(writer, value.EventDateTime, options);
      writer.WritePropertyName("type");
      JsonSerializer.Serialize(writer, value.Type, options);
      if (value.EventIndex != null) {
        writer.WritePropertyName("event_index");
        JsonSerializer.Serialize(writer, value.EventIndex, options);
      }
      if (value.GroupTag != null) {
        writer.WritePropertyName("group_tag");
        JsonSerializer.Serialize(writer, value.GroupTag, options);
      }
      if (value.Kingdom != null) {
        writer.WritePropertyName("kingdom");
        JsonSerializer.Serialize(writer, value.Kingdom, options);
      }
      if (value.Phylum != null) {
        writer.WritePropertyName("phylum");
        JsonSerializer.Serialize(writer, value.Phylum, options);
      }
      if (value.Class != null) {
        writer.WritePropertyName("class");
        JsonSerializer.Serialize(writer, value.Class, options);
      }
      if (value.Order != null) {
        writer.WritePropertyName("order");
        JsonSerializer.Serialize(writer, value.Order, options);
      }
      if (value.Family != null) {
        writer.WritePropertyName("family");
        JsonSerializer.Serialize(writer, value.Family, options);
      }
      if (value.Genus != null) {
        writer.WritePropertyName("genus");
        JsonSerializer.Serialize(writer, value.Genus, options);
      }
      if (value.Species != null) {
        writer.WritePropertyName("species");
        JsonSerializer.Serialize(writer, value.Species, options);
      }
      if (value.Float1 != null) {
        writer.WritePropertyName("float1");
        JsonSerializer.Serialize(writer, value.Float1, options);
      }
      if (value.Float2 != null) {
        writer.WritePropertyName("float2");
        JsonSerializer.Serialize(writer, value.Float2, options);
      }
      if (value.Float3 != null) {
        writer.WritePropertyName("float3");
        JsonSerializer.Serialize(writer, value.Float3, options);
      }
      if (value.Float4 != null) {
        writer.WritePropertyName("float4");
        JsonSerializer.Serialize(writer, value.Float4, options);
      }
      if (value.SpendType != null) {
        writer.WritePropertyName("spend_type");
        JsonSerializer.Serialize(writer, value.SpendType, options);
      }
      if (value.SpendCurrency != null) {
        writer.WritePropertyName("spend_currency");
        JsonSerializer.Serialize(writer, value.SpendCurrency, options);
      }
      if (value.SpendAmount != null) {
        writer.WritePropertyName("spend_amount");
        JsonSerializer.Serialize(writer, value.SpendAmount, options);
      }
      if (value.Network != null) {
        writer.WritePropertyName("network");
        JsonSerializer.Serialize(writer, value.Network, options);
      }
      if (value.Channel != null) {
        writer.WritePropertyName("channel");
        JsonSerializer.Serialize(writer, value.Channel, options);
      }
      if (value.FromTag != null) {
        writer.WritePropertyName("from_tag");
        JsonSerializer.Serialize(writer, value.FromTag, options);
      }
      if (value.ToTag != null) {
        writer.WritePropertyName("to_tag");
        JsonSerializer.Serialize(writer, value.FromTag, options);
      }
      if (value.ToList != null) {
        writer.WritePropertyName("to_list");
        JsonSerializer.Serialize(writer, value.ToList, options);
      }
      writer.WriteEndObject();
    }
    public override DCAllEvent Read(ref Utf8JsonReader reader, Type typeToConvert,
                                  JsonSerializerOptions options) {
      throw new NotImplementedException("Deserialization not needed");
    }
  }
}

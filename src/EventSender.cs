using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataCortex;

internal class EventSender : Sender<DCEvent> {
  private const int DAU_INTERVAL = 10 * 60;
  private int _eventIndex = 1;

  public EventSender(DCClient dc) : base("EventSender", "/1/track", dc) { }
  public override void AddEventQueue(DCEvent e) {
    e.EventIndex = _eventIndex++;
    base.AddEventQueue(e);
  }

  internal override string MakePayload(ImmutableList<DCEvent> list) {
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
}

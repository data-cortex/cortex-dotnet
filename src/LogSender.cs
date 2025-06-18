using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataCortex;

internal class LogSender : Sender<DCLogEvent> {
  public LogSender(DCClient dc) : base("LogSender", "/1/app_log", dc) {}
  internal override string MakePayload(ImmutableList<DCLogEvent> list) {
    var jsonPayload =
        new Dictionary<string, object> { ["api_key"] = _dc._apiKey,
                                         ["app_ver"] = _dc._appVersion,
                                         ["device_tag"] = _dc._deviceTag,
                                         ["os"] = _dc._os,
                                         ["os_ver"] = _dc._osVersion,
                                         ["device_family"] = _dc._deviceFamily,
                                         ["device_type"] = _dc._deviceType,
                                         ["language"] = _dc._language,
                                         ["country"] = _dc._country,
                                         ["events"] = list };
    var json = JsonSerializer.Serialize(jsonPayload, JSON_OPTIONS);
    return json;
  }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataCortex;

internal abstract class Sender<T> {
  private const double DELAY_MAX_INTERVAL = 30.0;
  private const double DELAY_MIN_INTERVAL = 1.0;
  private readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromSeconds(1.0);
  private const int HTTP_TIMEOUT = 60;
  private const int BATCH_COUNT = 10;

  internal readonly JsonSerializerOptions JSON_OPTIONS =
      new JsonSerializerOptions {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new CustomDateTimeConverter() }
      };

  private readonly string _name;
  private readonly string _apiPath;
  internal readonly DCClient _dc;
  private readonly List<T> _list;
  private readonly SerialTaskQueue _queue;
  private DateTime _lastSendAttemptTime = DateTime.MinValue;
  private bool _isRunning = false;
  private bool _isDirty = false;
  private bool _isSaving = false;
  private int _errorCount = 0;
  private double _sendInterval = DELAY_MIN_INTERVAL;

  public Sender(string name, string apiPath, DCClient dc) {
    _name = name;
    _apiPath = apiPath;
    _dc = dc;
    _list = Load();
    _queue = new SerialTaskQueue(name);
  }
  public void AddEvent(T e) {
    _queue.Run(() => {
      AddEventQueue(e);
      _isDirty = true;
      MaybeSave();
      CheckAndSend();
    });
  }
  public virtual void AddEventQueue(T e) { _list.Add(e); }

  private string GetISO8601Date() => DateTime.UtcNow.ToString(
      "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture.DateTimeFormat);

  private void CheckAndSend() {
    if (_list.Any() && (DateTime.UtcNow - _lastSendAttemptTime).TotalSeconds >=
                           _sendInterval) {
      MaybeSendInternal();
    } else {
      Task.Run(async () => {
        await Task.Delay(CHECK_INTERVAL);
        _queue.Run(() => CheckAndSend());
      });
    }
  }

  private void MaybeSendInternal() {
    if (!_isRunning && _list.Count > 0) {
      _isRunning = true;
      _lastSendAttemptTime = DateTime.UtcNow;
      var listToSend =
          _list.Take(Math.Min(_list.Count, BATCH_COUNT)).ToImmutableList();
      _list.RemoveRange(0, listToSend.Count);
      Task.Run(async () => await SendInternal(listToSend));
    }
  }

  internal abstract string MakePayload(ImmutableList<T> list);

  private async Task SendInternal(ImmutableList<T> list) {
    var urlString = $"{_dc._baseURL}{_apiPath}?current_time={GetISO8601Date()}";
    Logger.Info("urlString: {0}", urlString);
    var url = new Uri(urlString);
    using (var client = new HttpClient {
      Timeout = TimeSpan.FromSeconds(
                                             HTTP_TIMEOUT)
    }) {
      var request = new HttpRequestMessage(HttpMethod.Post, url);
      request.Headers.Add("Accept", "application/json");
      var json = MakePayload(list);
      Logger.Info("json: {0}", json);
      request.Content =
          new StringContent(json, Encoding.UTF8, "application/json");
      bool resend = false;
      bool is_error = true;
      try {
        var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode) {
          is_error = false;
        } else {
          if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) {
            var body = await response.Content.ReadAsStringAsync();
            Logger.Error($"Bad send 400: Body: {body}");
          } else if (response.StatusCode ==
                       System.Net.HttpStatusCode.Forbidden) {
            Logger.Error("Bad authentication (403), please check your API Key");
          } else if (response.StatusCode ==
                       System.Net.HttpStatusCode.Conflict) {
            Logger.Error("Conflict (409), dup send?");
          } else {
            var body = await response.Content.ReadAsStringAsync();
            Logger.Error(
                $"Send: Unknown error: {response.StatusCode}, Body: {body}");
            resend = true;
          }
        }
      } catch (HttpRequestException ex) {
        Logger.Error("Error from URL: {0}", ex.Message);
        resend = true;
      } catch (TaskCanceledException ex) {
        Logger.Error("HTTP Request Timeout: {0}", ex.Message);
        resend = true;
      } catch (Exception ex) {
        Logger.Error("An unexpected error occurred during send: {0}",
                     ex.Message);
        resend = true;
      }
      if (resend) {
        _queue.Run(() => RequeueList(list));
      } else {
        _queue.Run(() => SendComplete(is_error));
      }
    }
  }
  private void SendComplete(bool is_error) {
    if (is_error) {
      _errorCount++;
    } else {
      _errorCount = 0;
    }
    CalcInterval();
    _isRunning = false;
    _isDirty = true;
    MaybeSave();
    CheckAndSend();
  }
  private void RequeueList(ImmutableList<T> listToResend) {
    _errorCount++;
    _list.InsertRange(0, listToResend);
    CalcInterval();
    _isRunning = false;
    _isDirty = true;
    MaybeSave();
    CheckAndSend();
  }
  private void CalcInterval() {
    if (_errorCount <= 0) {
      _sendInterval = DELAY_MIN_INTERVAL;
    } else {
      double factor = Math.Pow(2, _errorCount - 1);
      _sendInterval = Math.Min(DELAY_MAX_INTERVAL, DELAY_MIN_INTERVAL * factor);
    }
    Logger.Info("_sendInterval: {0}", _sendInterval);
  }
  private void MaybeSave() {
    if (_dc._storageRoot != null && !_isSaving && _isDirty) {
      _isSaving = true;
      var save_list = _list.ToImmutableList();
      _isDirty = false;
      Task.Run(async () => {
        await Save(save_list);
        _queue.Run(SaveDone);
      });
    }
  }
  private void SaveDone() {
    _isSaving = false;
    MaybeSave();
  }
  private async Task Save(ImmutableList<T> save_list) {
    try {
      string path = Path.Combine(_dc._storageRoot, $"{_name}.json");
      Logger.Info("path: {0}", path);
      if (save_list.Any()) {
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true)) {
          await JsonSerializer.SerializeAsync(stream, save_list);
          await stream.FlushAsync();
        }
        if (File.Exists(path)) {
          File.Replace(temp, path, null);
        } else {
          File.Move(temp, path);
        }
      } else {
        File.Delete(path);
      }
    } catch (Exception ex) {
      Logger.Error("Sender.Save error: {0}", ex);
    }
  }
  private List<T> Load() {
    try {
      if (_dc._storageRoot == null) {
        return new List<T>();
      }
      string path = Path.Combine(_dc._storageRoot, $"{_name}.json");
      if (!File.Exists(path)) {
        return new List<T>();
      }
      string json = File.ReadAllText(path);
      var ret = JsonSerializer.Deserialize<List<T>>(json);
      if (ret != null) {
        return ret;
      }
    } catch (Exception ex) {
      Logger.Error("Sender.Load error: {0}", ex);
    }
    return new List<T>();
  }

  public class CustomDateTimeConverter : JsonConverter<DateTime> {
    private const string FORMAT = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override void Write(Utf8JsonWriter writer, DateTime value,
                               JsonSerializerOptions options) {
      writer.WriteStringValue(value.ToUniversalTime().ToString(FORMAT));
    }
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert,
                                  JsonSerializerOptions options) {
      throw new NotImplementedException("Deserialization not needed");
    }
  }
}

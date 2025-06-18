using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace DataCortex {
  public static class DCShared {
    public static DCClient? Instance;

    public static void Init(string apiKey, string org,
                            TimeZoneInfo? dauTimeZone = null) {
      if (Instance == null) {
        Instance = new DCClient(apiKey, org, dauTimeZone);
      } else {
        throw new Exception("Already initialized");
      }
    }
    public static void Event(DCEvent e) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.Event(e);
    }
    public static void Economy(DCEvent e) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.Economy(e);
    }
    public static void Log(DCLogEvent e) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.Log(e);
    }
    public static void Log(string format, params object[] args) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.Log(format, args);
    }
    public static void LogError(string format, params object[] args) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.LogError(format, args);
    }
  }
  public class DCClient {
    private const int DAU_INTERVAL = 10 * 60;
    private const double DELAY_RETRY_INTERVAL = 30.0;
    private const double DELAY_SEND_INTERVAL = 1.0;
    private const int HTTP_TIMEOUT = 60;
    private const string API_BASE_URL = "https://api.data-cortex.com";
    private const int TAG_MAX_LENGTH = 62;
    private const int CONFIG_VER_MAX_LENGTH = 16;
    private const int SERVER_VER_MAX_LENGTH = 16;
    private const int GROUP_TAG_MAX_LENGTH = 32;
    private const int TAXONOMY_MAX_LENGTH = 32;
    private const int BATCH_COUNT = 10;
    private const string USER_TAG_PREFIX_KEY = "UserTag";
    private const string EVENT_LIST_KEY = "data_cortex_eventList";
    private const string DEVICE_TAG_KEY = "DeviceTag";
    private const string USER_TAG_KEY = "UserTag";
    private const string INSTALL_SENT_KEY = "InstallSent";
    private const string LAST_DAU_SEND_KEY = "LastDAUSend";

    internal readonly string _apiKey;
    internal readonly string _organization;
    internal readonly TimeZoneInfo _dauTimeZone;
    private readonly Settings _settings;
    internal readonly string? _storageRoot;
    internal readonly string _baseURL;

    private readonly EventSender _eventSender;
    private readonly LogSender _logSender;

    internal string _deviceTag;
    internal string _appVersion;
    internal string _os;
    internal string _osVersion;
    internal string _deviceFamily;
    internal string _deviceType;
    internal string _language;
    internal string _country;

    internal string? _userTag;
    internal string? _facebookTag;
    internal string? _twitterTag;
    internal string? _googleTag;
    internal string? _gameCenterTag;
    internal string? _serverVersion;
    internal string? _configVersion;

    internal DateTime _lastDAUSend;
    internal DateTime _lastDauCheckTime;
    internal DateTime _lastEventSendAttemptTime;
    internal DateTime _lastLogSendAttemptTime;

    public string? UserTag {
      get { return _userTag; }
      set { _userTag = TagSave(value, ""); }
    }
    public string? FacebookTag {
      get { return _facebookTag; }
      set { _facebookTag = TagSave(value, "Facebook"); }
    }
    public string? TwitterTag {
      get { return _twitterTag; }
      set { _twitterTag = TagSave(value, "Twitter"); }
    }
    public string? GoogleTag {
      get { return _googleTag; }
      set { _googleTag = TagSave(value, "Google"); }
    }
    public string? GameCenterTag {
      get { return _gameCenterTag; }
      set { _gameCenterTag = TagSave(value, "GameCenter"); }
    }
    public string? ServerVersion {
      get { return _serverVersion; }
      set { _serverVersion = ValueCopy(value, SERVER_VER_MAX_LENGTH); }
    }
    public string? ConfigVersion {
      get { return _configVersion; }
      set { _configVersion = ValueCopy(value, CONFIG_VER_MAX_LENGTH); }
    }

    public DCClient(string apiKey, string org, TimeZoneInfo? dauTZ = null,
                    string? registryRoot = null, string? storageRoot = null) {
      _apiKey = apiKey;
      _organization = org.ToLower().Replace(" ", "_");
      _dauTimeZone = dauTZ ?? TimeZoneInfo.Utc;
      _settings = new Settings(registryRoot);
      _storageRoot = _storageRoot ?? MachineTools.GetLocalAppDataPath();

      _lastDAUSend = _settings.Load<DateTime>(LAST_DAU_SEND_KEY);

      _userTag = GetSavedUserTagWithName("");
      _facebookTag = GetSavedUserTagWithName("Facebook");
      _twitterTag = GetSavedUserTagWithName("Twitter");
      _googleTag = GetSavedUserTagWithName("Google");
      _gameCenterTag = GetSavedUserTagWithName("GameCenter");

      var found_device_tag = _settings.Load<string>(DEVICE_TAG_KEY);
      if (found_device_tag != null && found_device_tag.Length > 0) {
        _deviceTag = found_device_tag;
      } else {
        _deviceTag =
            MachineTools.GetMachineIdentifier() ?? Guid.NewGuid().ToString();
        _settings.Save(DEVICE_TAG_KEY, _deviceTag);
      }

      _appVersion =
          Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
      if (_appVersion.Split('.').Length > 3) {
        _appVersion = string.Join(".", _appVersion.Split('.').Take(3));
      }

      _os = GetOSType();
      Version osVersion = Environment.OSVersion.Version;
      _osVersion = $"{osVersion.Major}.{osVersion.Minor}.{osVersion.Build}";
      _deviceType = MachineTools.GetModel() ?? "unknown";
      _deviceFamily = GetDeviceFamily();
      _country = RegionInfo.CurrentRegion.TwoLetterISORegionName;
      _language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

      _baseURL = $"{API_BASE_URL}/{_organization}";

      _lastDauCheckTime = DateTime.MinValue;
      _lastEventSendAttemptTime = DateTime.MinValue;
      _lastLogSendAttemptTime = DateTime.MinValue;

      if (!_settings.Load<bool>(INSTALL_SENT_KEY)) {
        Event(new DCEvent { Kingdom = "organic" });
        _settings.Save(INSTALL_SENT_KEY, true);
      }
      _eventSender = new EventSender(this);
      _logSender = new LogSender(this);
    }
    public void Event(DCEvent e) {
      ValidateEvent(e);
      _eventSender.AddEvent(e);
    }
    public void Economy(DCEvent e) {
      e.Type = "economy";
      ValidateEvent(e);
      _eventSender.AddEvent(e);
    }
    public void Log(DCLogEvent e) {
      ValidateLogEvent(e);
      _logSender.AddEvent(e);
    }
    public void Log(string format, params object[] args) {
      var s = string.Format(CultureInfo.InvariantCulture, format, args);
      _logSender.AddEvent(new DCLogEvent { LogLine = s });
    }
    public void LogError(string format, params object[] args) {
      var s = string.Format(CultureInfo.InvariantCulture, format, args);
      _logSender.AddEvent(new DCLogEvent { LogLine = s });
    }

    private string? GetSavedUserTagWithName(string name = "") {
      string key = USER_TAG_PREFIX_KEY + name;
      return _settings.Load<string>(key);
    }
    private string? ValueCopy(string? value, int max_len) {
      return value != null && value.Length > max_len ? value.Substring(0, max_len)
                                                     : value;
    }
    private string? TagSave(string? v, string name) {
      var value = ValueCopy(v, TAG_MAX_LENGTH);
      var key = USER_TAG_PREFIX_KEY + name;
      _settings.Save(key, value);
      return value;
    }
    private string? TrimString(string s, int max_len) {
      return s != null && s.Length > max_len ? s.Substring(0, max_len) : s;
    }
    private void ValidateEvent(DCEvent e) {
      if (e.Type == "economy") {
        if (e.SpendCurrency == null) {
          throw new ArgumentException(
              "spendCurrency is required for economy events");
        }
        if (!e.SpendAmount.HasValue) {
          throw new ArgumentException(
              "Missing required value spendAmount for economy event");
        }
      }
    }
    private void ValidateLogEvent(DCLogEvent e) {
      if (e.LogLine == null || e.LogLine.Length == 0) {
        throw new ArgumentException("LogLine is required");
      }
    }
    private string GetOSType() {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return "win32";
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return "darwin";
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        return "linux";
      return "unknown";
    }
    private string GetDeviceFamily() {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"))) {
        return "android";
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"))) {
        return "ios";
      }
      string desc = RuntimeInformation.OSDescription.ToLowerInvariant();
      if (desc.Contains("android")) {
        return "android";
      } else if (desc.Contains("iphone")) {
        return "iphone";
      } else if (desc.Contains("ipad")) {
        return "ipad";
      }
      return "desktop";
    }
  }
}

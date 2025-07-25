using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
    public static void Economy(DCEconomy e) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.Economy(e);
    }
    public static void MessageSend(DCMessageSend e) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.MessageSend(e);
    }
    public static void MessageClick(DCMessageClick e) {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      Instance.MessageClick(e);
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

    public static async Task Flush() {
      if (Instance == null) {
        throw new Exception("Not initialized");
      }
      await Instance.Flush();
    }
  }
  public class DCClient {
    private const string DATE_FORMAT = "yyyy-MM-dd";
    private const string API_BASE_URL = "https://api.data-cortex.com";
    private const int TAG_MAX_LENGTH = 62;
    private const int CONFIG_VER_MAX_LENGTH = 16;
    private const int SERVER_VER_MAX_LENGTH = 16;
    private const int GROUP_TAG_MAX_LENGTH = 32;
    private const int TAXONOMY_MAX_LENGTH = 32;
    private const string USER_TAG_PREFIX_KEY = "UserTag";
    private const string DEVICE_TAG_KEY = "DeviceTag";
    private const string INSTALL_SENT_KEY = "InstallSent";
    private const string LAST_DAU_SEND_KEY = "LastDAUSend";

    private readonly TimeSpan DAU_CHECK_INTERVAL = TimeSpan.FromSeconds(10 * 60);

    internal readonly string _apiKey;
    internal readonly string _organization;
    internal readonly TimeZoneInfo _dauTimeZone;
    private readonly Settings _settings;
    internal readonly string? _storageRoot;
    internal readonly string _baseURL;

    private readonly EventSender _eventSender;
    private readonly LogSender _logSender;

    internal string _appVersion;
    internal string _os;
    internal string _osVersion;
    internal string _deviceFamily;
    internal string _deviceType;
    internal string _language;
    internal string _country;

    internal string _deviceTag;
    internal string? _userTag;
    internal string? _facebookTag;
    internal string? _twitterTag;
    internal string? _googleTag;
    internal string? _gameCenterTag;
    internal string? _serverVersion;
    internal string? _configVersion;

    internal string _lastDAUSend;

    public string DeviceTag {
      get { return _deviceTag; }
      set {
        var temp = ValueCopy(value, TAG_MAX_LENGTH);
        if (temp == null) {
          throw new ArgumentException("Device Tag cant be null");
        }
        _deviceTag = temp;
      }
    }
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

      var last_dau_date = _settings.Load<DateTime>(LAST_DAU_SEND_KEY);
      if (last_dau_date != null) {
        _lastDAUSend = TimeZoneInfo.ConvertTimeFromUtc(last_dau_date,
            _dauTimeZone).ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
      } else {
        _lastDAUSend = "";
      }

      _userTag = GetSavedUserTagWithName("");
      _facebookTag = GetSavedUserTagWithName("Facebook");
      _twitterTag = GetSavedUserTagWithName("Twitter");
      _googleTag = GetSavedUserTagWithName("Google");
      _gameCenterTag = GetSavedUserTagWithName("GameCenter");

      string? found_device_tag = _settings.Load<string>(DEVICE_TAG_KEY);
      if (found_device_tag != null && found_device_tag.Length > 0) {
        _deviceTag = found_device_tag;
      } else {
        _deviceTag =
            MachineTools.GetMachineIdentifier() ?? Guid.NewGuid().ToString();
        _settings.Save(DEVICE_TAG_KEY, _deviceTag);
      }

      _appVersion =
          Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
      _os = GetOSType();
      var os_ver = MachineTools.GetOSVersion();
      _osVersion = $"{os_ver.Major}.{os_ver.Minor}.{os_ver.Build}";
      _deviceType = MachineTools.GetModel() ?? "unknown";
      _deviceFamily = GetDeviceFamily();
      _country = RegionInfo.CurrentRegion.TwoLetterISORegionName;
      _language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

      _baseURL = $"{API_BASE_URL}/{_organization}";

      _eventSender = new EventSender(this);
      _logSender = new LogSender(this);
      if (!_settings.Load<bool>(INSTALL_SENT_KEY)) {
        Event(new DCEvent { Type = "install", Kingdom = "organic" });
        _settings.Save(INSTALL_SENT_KEY, true);
      }
      Task.Run(DauLoop);
    }
    public void Event(DCEvent e) {
      ValidateEvent(e);
      _eventSender.AddEvent(e);
    }
    public void Economy(DCEconomy e) {
      e.Type = "economy";
      ValidateEvent(e);
      _eventSender.AddEvent(e);
    }
    public void MessageSend(DCMessageSend e) {
      e.Type = "message_send";
      ValidateEvent(e);
      _eventSender.AddEvent(e);
    }
    public void MessageClick(DCMessageClick e) {
      e.Type = "message_click";
      ValidateEvent(e);
      _eventSender.AddEvent(e);
    }
    public void Log(DCLogEvent e) {
      ValidateLogEvent(e);
      _logSender.AddEvent(e);
    }
    public void Log(string format, params object[] args) {
      var s = LogToString(format, args);
      _logSender.AddEvent(new DCLogEvent { LogLine = s });
    }
    public void LogError(string format, params object[] args) {
      var s = LogToString(format, args);
      _logSender.AddEvent(new DCLogEvent { LogLine = s, LogLevel = "error" });
    }
    public async Task Flush() {
      while (!await _eventSender.IsEmpty() && !await _logSender.IsEmpty()) {
        await Task.Delay(100);
      }
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
    private void ValidateEvent(DCAllEvent e) {
      if (e is DCEconomy econ) {
        if (econ.SpendCurrency == null) {
          throw new ArgumentException(
              "spendCurrency is required for economy events");
        }
        if (!econ.SpendAmount.HasValue) {
          throw new ArgumentException(
              "Missing required value spendAmount for economy event");
        }
      }
    }
    private string LogToString(string format, params object[] args) {
      var ret = format;
      if (args != null && args.Length > 0) {
        ret = string.Format(CultureInfo.InvariantCulture, format, args);
      }
      return ret;
    }
    private void ValidateLogEvent(DCLogEvent e) {
      if (e.LogLine == null || e.LogLine.Length == 0) {
        throw new ArgumentException("LogLine is required");
      }
    }
    public async Task DauLoop() {
      while (true) {
        try {
          DateTime now = DateTime.UtcNow;
          string nowString = TimeZoneInfo.ConvertTimeFromUtc(now, _dauTimeZone)
            .ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
          if (!_lastDAUSend.Equals(nowString, StringComparison.Ordinal)) {
            _lastDAUSend = nowString;
            _settings.Save(LAST_DAU_SEND_KEY, now);
            Logger.Info("DauLoop: Sending DAU event for date: {0}", nowString);
            Event(new DCEvent { Type = "dau" });
          }
          await Task.Delay(DAU_CHECK_INTERVAL);
        } catch (TaskCanceledException) {
          break;
        } catch (Exception ex) {
          Logger.Error("DauLoop threw: {0}", ex);
        }
      }
    }
    private string GetOSType() {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        return "win32";
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        return "darwin";
      } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
        return "linux";
      }
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

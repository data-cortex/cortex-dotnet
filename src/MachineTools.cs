using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;

internal static class MachineTools {
  public static string GetMachineIdentifier() {
    var adid = TryGetAdvertisingId();
    Logger.Info("adid {0}", adid);
    if (adid != null) {
      return adid;
    }
    var smbios = TryGetWmi("Win32_ComputerSystemProduct", "UUID");
    Logger.Info("smbios {0}", smbios);
    var board = TryGetWmi("Win32_BaseBoard", "SerialNumber");
    Logger.Info("board {0}", board);
    var bios = TryGetWmi("Win32_BIOS", "SerialNumber");
    Logger.Info("bios {0}", bios);

    if (smbios != null) {
      return Normalize(smbios);
    }
    if (board != null) {
      return HashHex(board);
    }
    if (bios != null) {
      return HashHex(bios);
    }
    Logger.Info("guid");
    return Guid.NewGuid().ToString();
  }
  public static string? TryGetAdvertisingId() {
    try {
      var advertisingManagerType =
          Type.GetType("Windows.System.UserProfile.AdvertisingManager, " +
                       "Windows, ContentType=WindowsRuntime");

      if (advertisingManagerType == null) {
        return null;
      }
      var property = advertisingManagerType.GetProperty("AdvertisingId");
      if (property == null) {
        return null;
      }
      var value = property.GetValue(null);
      if (value is string ad_id) {
        if (!string.IsNullOrEmpty(ad_id)) {
          return ad_id;
        }
      }
    } catch {
    }
    return null;
  }
  private static string? TryGetWmi(string className, string property) {
    try {
      using (var searcher = new ManagementClass(className)) {
        foreach (ManagementObject mo in searcher.GetInstances()) {
          var value = mo[property]?.ToString()?.Trim();
          if (!string.IsNullOrEmpty(value)) {
            return ValidHardwareId(value);
          }
        }
      }
    } catch { }
    return null;
  }
  private static string? ValidHardwareId(string? id) {
    Logger.Info("ValidHardwareId: {0}", id);
    if (id == null) {
      return null;
    } else if (string.IsNullOrWhiteSpace(id)) {
      return null;
    }
    id = id.ToLowerInvariant();
    if (id == "ffffffff-ffff-ffff-ffff-ffffffffffff") {
      return null;
    } else if (id == "unknown") {
      return null;
    } else if (id.Contains("to be filled")) {
      return null;
    } else if (id.Length < 10) {
      return null;
    }
    return id;
  }
  private static string Normalize(string uuid) {
    return uuid.Trim().ToLowerInvariant();
  }

  private static string HashHex(string input) {
    using (var sha256 = System.Security.Cryptography.SHA256.Create()) {
      var bytes = System.Text.Encoding.UTF8.GetBytes(input);
      var hash = sha256.ComputeHash(bytes);
      return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
  }
  public static string? GetModel() {
    try {
      using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem")) {
        foreach (ManagementObject mo in searcher.Get()) {
          var manufacturer = mo["Manufacturer"]?.ToString()?.Trim();
          var model = mo["Model"]?.ToString()?.Trim();
          string? ret = "";
          if (manufacturer != null && manufacturer.Length > 0) {
            ret += manufacturer;
          }
          if (model != null && model.Length > 0) {
            if (ret.Length > 0) {
              ret += "_";
            }
            ret += model;
          }
          if (ret.Length == 0) {
            ret = null;
          }
          return ret;
        }
      }
    } catch (Exception ex) {
      Logger.Error("GetModel: err: {0}", ex);
    }
    return null;
  }
  public static string? GetLocalAppDataPath() {
    string baseDir = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData);

    string? mainAssemblyName =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
    string fallbackName = AppDomain.CurrentDomain.FriendlyName;
    string safeAppName =
        Sanitize(mainAssemblyName ?? fallbackName ?? "DataCortex");
    string? ret = Path.Combine(baseDir, safeAppName, "DataCortex");
    try {
      Directory.CreateDirectory(ret);
    } catch (Exception ex) {
      Logger.Error($"GetLocalAppDataPath failed: {0}", ex);
      ret = null;
    }
    return ret;
  }
  private static string Sanitize(string name) {
    foreach (char c in Path.GetInvalidFileNameChars()) {
      name = name.Replace(c, '_');
    }
    return name;
  }
  [DllImport("ntdll.dll", SetLastError = true)]
  private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

  [StructLayout(LayoutKind.Sequential)]
  struct OSVERSIONINFOEX {
    public int dwOSVersionInfoSize;
    public int dwMajorVersion;
    public int dwMinorVersion;
    public int dwBuildNumber;
    public int dwPlatformId;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szCSDVersion;
  }
  public static Version GetOSVersion() {
    try {
      var osvi = new OSVERSIONINFOEX();
      osvi.dwOSVersionInfoSize = Marshal.SizeOf(osvi);
      int status = RtlGetVersion(ref osvi);
      if (status == 0) // STATUS_SUCCESS
      {
        return new Version(osvi.dwMajorVersion, osvi.dwMinorVersion, osvi.dwBuildNumber);
      }
    } catch (Exception ex) {
      Logger.Error("GetOSVersion threw: {0}", ex);
    }
    return Environment.OSVersion.Version; // fallback
  }
}

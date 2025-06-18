using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

internal static class MachineTools {
  public static string GetMachineIdentifier() {
    var adid = TryGetAdvertisingId();
    Logger.Info("adid {0}", adid);
    if (adid != null) {
      return adid;
    }
    var smbios = TryGetWmiDynamic("Win32_ComputerSystemProduct", "UUID");
    Logger.Info("smbios {0}", smbios);
    if (smbios != null) {
      return Normalize(smbios);
    }
    var board = TryGetWmiDynamic("Win32_BaseBoard", "SerialNumber");
    Logger.Info("board {0}", board);
    if (board != null) {
      return HashHex(board);
    }
    var bios = TryGetWmiDynamic("Win32_BIOS", "SerialNumber");
    Logger.Info("bios {0}", bios);
    if (bios != null) {
      return HashHex(bios);
    }
    var volume = TryGetVolumeSerialDynamic("C");
    Logger.Info("volume {0}", volume);
    if (volume != null) {
      return HashHex(volume);
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
  private static string? TryGetWmiDynamic(string className,
                                          string propertyName) {
    try {
      var sysMgmt = Assembly.Load("System.Management");
      if (sysMgmt == null) {
        return null;
      }
      var mgmtClassType = sysMgmt.GetType("System.Management.ManagementClass");
      if (mgmtClassType == null) {
        return null;
      }
      var mgmtClassInstance =
          Activator.CreateInstance(mgmtClassType, new object[] { className });
      if (mgmtClassInstance == null) {
        return null;
      }
      var getInstancesMethod = mgmtClassType.GetMethod("GetInstances");
      var instances =
          getInstancesMethod?.Invoke(mgmtClassInstance, null)
              as System.Collections.IEnumerable;
      if (instances == null) {
        return null;
      }
      foreach (var instance in instances) {
        var mgmtObjType = sysMgmt.GetType("System.Management.ManagementObject");
        if (mgmtObjType == null) {
          return null;
        }
        var propertiesProp = mgmtObjType.GetProperty("Properties");
        var properties = propertiesProp?.GetValue(instance);
        if (properties == null) {
          return null;
        }
        var propDataCollectionType =
            sysMgmt.GetType("System.Management.PropertyDataCollection");
        if (propDataCollectionType == null) {
          return null;
        }
        var itemProp = propDataCollectionType.GetProperty("Item");
        var propertyData =
            itemProp?.GetValue(properties, new object[] { propertyName });
        if (propertyData == null) {
          return null;
        }
        var propertyDataType =
            sysMgmt.GetType("System.Management.PropertyData");
        var valueProp = propertyDataType.GetProperty("Value");
        var val = valueProp?.GetValue(propertyData);

        if (val != null) {
          return ValidHardwareId(val.ToString());
        }
      }
    } catch {
    }

    return null;
  }

  private static string? TryGetVolumeSerialDynamic(string driveLetter) {
    try {
      var sysMgmt = Assembly.Load("System.Management");
      if (sysMgmt == null) {
        return null;
      }
      var mgmtObjType = sysMgmt.GetType("System.Management.ManagementObject");
      if (mgmtObjType == null) {
        return null;
      }
      var mgmtObjInstance = Activator.CreateInstance(
          mgmtObjType,
          new object[] { $"Win32_LogicalDisk.DeviceID=\"{driveLetter}:\"" });
      if (mgmtObjInstance == null) {
        return null;
      }

      var getMethod = mgmtObjType.GetMethod("Get");
      getMethod?.Invoke(mgmtObjInstance, null);

      var propertiesProp = mgmtObjType.GetProperty("Properties");
      var properties = propertiesProp?.GetValue(mgmtObjInstance);
      if (properties == null) {
        return null;
      }
      var propDataCollectionType =
          sysMgmt.GetType("System.Management.PropertyDataCollection");
      if (propDataCollectionType == null) {
        return null;
      }
      var itemProp = propDataCollectionType.GetProperty("Item");
      var propertyData =
          itemProp?.GetValue(properties, new object[] { "VolumeSerialNumber" });
      if (propertyData == null) {
        return null;
      }
      var propertyDataType = sysMgmt.GetType("System.Management.PropertyData");
      var valueProp = propertyDataType.GetProperty("Value");
      var val = valueProp?.GetValue(propertyData);
      if (val != null) {
        return ValidHardwareId(val.ToString());
      }
    } catch {
    }

    return null;
  }
  private static string? ValidHardwareId(string? id) {
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
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      return null;
    }

    try {
      var managementAssembly =
          AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
              a => a.GetName().Name == "System.Management") ??
          Assembly.Load("System.Management");

      if (managementAssembly == null) {
        return null;
      }

      var searcherType = managementAssembly.GetType(
          "System.Management.ManagementObjectSearcher");
      var objectType =
          managementAssembly.GetType("System.Management.ManagementObject");

      if (searcherType == null || objectType == null) {
        return null;
      }

      using var searcher = (IDisposable)Activator.CreateInstance(
          searcherType, "SELECT * FROM Win32_ComputerSystem");

      var getMethod = searcherType.GetMethod("Get");
      var results = getMethod.Invoke(searcher, null) as IEnumerable;

      if (results == null) {
        return null;
      }

      foreach (var mo in results) {
        var itemProperty = mo.GetType().GetProperty("Item");
        var manufacturer =
            itemProperty?.GetValue(mo, new object[] { "Manufacturer" })
                ?.ToString()
                ?.Trim();
        var model = itemProperty?.GetValue(mo, new object[] { "Model" })
                        ?.ToString()
                        ?.Trim();
        return $"{manufacturer} {model}".Trim();
      }
    } catch {
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
}

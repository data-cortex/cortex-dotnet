using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

internal class Settings {
  private readonly string _regRoot;
  public Settings(string? root = null) {
    if (root == null) {
      var app_name = Assembly.GetEntryAssembly()?.GetName().Name;
      if (app_name == null || app_name.Length == 0) {
        _regRoot = @"Software\DataCortex";
      } else {
        _regRoot = @$"Software\{app_name}\DataCortex";
      }
    } else {
      _regRoot = root;
    }
    Logger.Info("_regRoot: {0}", _regRoot);
  }
  public void Save<T>(string name, T value) {
    using (RegistryKey? key = Registry.CurrentUser?.CreateSubKey(_regRoot)) {
      if (key != null) {
        if (value == null) {
          throw new ArgumentException("Cant set to null");
        } else if (value is long l) {
          key.SetValue(name, l, RegistryValueKind.QWord);
        } else if (value is int i) {
          key.SetValue(name, i, RegistryValueKind.DWord);
        } else if (value is bool b) {
          key.SetValue(name, b ? 1 : 0, RegistryValueKind.DWord);
        } else {
          key.SetValue(name, value.ToString(), RegistryValueKind.String);
        }
      }
    }
  }
  public T? Load<T>(string name) {
    using (RegistryKey? key = Registry.CurrentUser?.OpenSubKey(_regRoot)) {
      if (key == null) {
        return default;
      }
      object? value = key?.GetValue(name);
      if (value == null) {
        return default;
      }
      if (value is T tValue) {
        return tValue;
      }

      try {
        if (typeof(T) == typeof(string)) {
          return (T)(object)value.ToString();
        }
        if (typeof(T) == typeof(bool)) {
          return (T)(object)(value.ToString() == "1" ||
                             value.ToString().ToLower() == "true");
        }
        if (typeof(T) == typeof(DateTime) &&
            DateTime.TryParse(value.ToString(), out var dt)) {
          return (T)(object)dt;
        }
        return (T)Convert.ChangeType(value, typeof(T));
      } catch {
        return default;
      }
    }
  }
}

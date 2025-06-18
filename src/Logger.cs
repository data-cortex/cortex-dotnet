using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

internal static class Logger {
  private const string SOURCE = "DataCortex";
  private const string LOG_NAME = "Application";

  static Logger() {
    try {
      if (!EventLog.SourceExists(SOURCE)) {
        EventLog.CreateEventSource(SOURCE, LOG_NAME);
      }
    } catch (Exception ex) {
      Console.WriteLine($"Failed to initialize event log source: {ex}");
    }
  }

  public static void Info(string format, params object[] args) {
    Write(EventLogEntryType.Information, format, args);
  }
  public static void Warn(string format, params object[] args) {
    Write(EventLogEntryType.Warning, format, args);
  }
  public static void Error(string format, params object[] args) {
    Write(EventLogEntryType.Error, format, args);
  }
  private static void Write(EventLogEntryType type, string format,
                            params object[] args) {
    string message;
    try {
      object[] fixed_args = new object[args.Length];
      for (int i = 0; i < args.Length; i++) {
        object arg = args[i];
        fixed_args[i] = CustomToString(arg);
      }
      message = string.Format(format, fixed_args);
    } catch (FormatException) {
      message = format + " [Format error in arguments]";
    }

    try {
      EventLog.WriteEntry(SOURCE, message, type);
    } catch {
      if (type == EventLogEntryType.Error) {
        Console.Error.WriteLine($"{DateTime.UtcNow}: {message}");
      } else {
        Console.WriteLine($"{DateTime.UtcNow}: {message}");
      }
    }
  }
  private static object CustomToString(object obj, bool quote = false) {
    object ret;
    var dict = obj as IDictionary;
    if (dict != null) {
      var entries = new System.Text.StringBuilder();
      entries.Append("{ ");
      foreach (DictionaryEntry entry in dict) {
        entries.AppendFormat("{0} = {1}, ", entry.Key,
                             CustomToString(entry.Value, true));
      }
      entries.Append("} ");
      ret = entries.ToString();
    } else if (obj == null) {
      ret = "(null)";
    } else if (quote) {
      ret = "\"" + obj.ToString() + "\"";
    } else {
      ret = obj;
    }
    return ret;
  }
}

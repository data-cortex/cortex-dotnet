using System;
using DataCortex;

internal static class Logger {
  private static DCLogger _logger = new DefaultLogger();

  public static void SetLogger(DCLogger logger) {
    _logger = logger;
  }

  public static void Info(string format, params object[] args) {
    _logger.Info(format, args);
  }
  public static void Warn(string format, params object[] args) {
    _logger.Warn(format, args);
  }
  public static void Error(string format, params object[] args) {
    _logger.Error(format, args);
  }

  public class DefaultLogger : DCLogger {
    public override void Info(string format, params object[] args) {
      Console.WriteLine(format, args);
    }
    public override void Warn(string format, params object[] args) {
      Console.WriteLine(format, args);
    }
    public override void Error(string format, params object[] args) {
      Console.WriteLine(format, args);
    }
  }
}

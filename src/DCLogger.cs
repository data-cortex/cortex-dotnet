namespace DataCortex {
  public abstract class DCLogger {
    public static void SetLogger(DCLogger logger) {
      Logger.SetLogger(logger);
    }
    public abstract void Info(string format, params object[] args);
    public abstract void Warn(string format, params object[] args);
    public abstract void Error(string format, params object[] args);
  }
}

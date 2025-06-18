using DataCortex;

class Example {
  static void Main(string[] args) {
    try {
      if (args.Length < 1) {
        Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} <api_key>");
        return;
      }
      string api_key = args[0];
      Console.WriteLine($"Init: api_key: {api_key}");
      DCShared.Init(apiKey: api_key, org: "test");

      DCShared.Log("Test 123");
      Console.WriteLine("Wrote Test 123");

      Console.WriteLine("Sleep 1...");
      Thread.Sleep(1);

      DCShared.Log(
          new DCLogEvent { LogLine = "Test 456", Hostname = "hostname" });
      DCShared.Log("Test 789: {0}", 444);
      Console.WriteLine("2 more logs");

      DCShared.Event(new DCEvent { Kingdom = "kingdom" });
      DCShared.Event(new DCEvent { Phylum = "phylum" });
      Console.WriteLine("2 events");

      DCShared.Log(new DCLogEvent { LogLine = "foo", Hostname = "foo" });
      Console.WriteLine("1 log event");

      Console.WriteLine("Wait forever");
      Thread.Sleep(100000000);
    } catch (Exception ex) {
      Console.Error.WriteLine("Threw: {0}", ex);
    }
  }
}

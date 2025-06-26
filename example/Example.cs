using DataCortex;

class Example {
  static string api_key = "";
  static void Main(string[] args) {
    if (args.Length < 1) {
      Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} <api_key>");
      return;
    }
    api_key = args[0];
    MainAsync().GetAwaiter().GetResult();
  }
  static async Task MainAsync() {
    try {
      Console.WriteLine($"Init: api_key: {api_key}");
      DCShared.Init(apiKey: api_key, org: "test");
      if (DCShared.Instance == null) {
        throw new Exception("Init failed");
      }
      DCShared.Log("Test 123");
      Console.WriteLine("Wrote Test 123");
      DCShared.LogError("Test 456");
      Console.WriteLine("Wrote Test 456 Error");

      DCShared.Log(
          new DCLogEvent { LogLine = "Test 456", Hostname = "hostname" });
      DCShared.Log("Test 789: {0}", 444);
      Console.WriteLine("2 more logs");

      DCShared.Event(new DCEvent { Kingdom = "kingdom" });
      DCShared.Event(new DCEvent { Phylum = "phylum" });
      Console.WriteLine("2 events");

      DCShared.Log(new DCLogEvent { LogLine = "foo", Hostname = "foo" });
      Console.WriteLine("1 log event");

      Console.WriteLine("before user tag set: {0}", DCShared.Instance.UserTag);

      DCShared.Log("This is invalid {1357176} but should work");
      DCShared.LogError("This is error invalid {44444} but should work");
      try {
        DCShared.Log("This will throw {1357176} this one", 123);
        Console.Error.WriteLine("!!!!!!!!!!!!!! Should have thrown!!!!!!!!!");
      } catch {
        Console.WriteLine($"Threw as expected");
      }
      try {
        DCShared.LogError("This will throw {1357176} this one", 123);
        Console.Error.WriteLine("!!!!!!!!!!!!!! Should have thrown!!!!!!!!!");
      } catch {
        Console.WriteLine($"Threw as expected");
      }

      await DCShared.Flush();
      DCShared.Instance.UserTag = "user" + (new Random()).Next(1000, 9000);
      Console.WriteLine("after user tag set: {0}", DCShared.Instance.UserTag);
      DCShared.Event(new DCEvent { Kingdom = "after usertag" });
      DCShared.Log(new DCLogEvent { LogLine = "after usertag", Hostname = "foo" });

      DCShared.Event(new DCEvent { Kingdom = "event index", EventIndex = 42 });


      DCShared.Event(new DCEvent { EventDateTime = DateTime.UtcNow, Kingdom = "date" });

      DCShared.Event(new DCEvent { Kingdom = "\"quotes\"\"middle\"\"" });

      DCShared.Event(new DCEvent { Kingdom = "newline" });

      DCShared.Economy(new DCEconomy {
        Kingdom = "economy",
        SpendType = "cash_purchase",
        SpendCurrency = "USD",
        SpendAmount = 123.45
      });

      DCShared.Event(new DCEvent { Type = "dau", Kingdom = "dau" });

      DCShared.Event(new DCEvent { Type = "install", Kingdom = "organic" });

      DCShared.Log(new DCLogEvent { LogLine = "This is a log line" });
      DCShared.Log(new DCLogEvent {
        LogLine = $"This is a log line with args 1 foo {DateTime.UtcNow} {new Exception()}"
      });
      DCShared.Log(new DCLogEvent {
        Hostname = "host",
        Filename = AppDomain.CurrentDomain.BaseDirectory,
        LogLevel = "crit",
        UserTag = "user444",
        RemoteAddress = "1.2.3.4",
        ResponseBytes = 22,
        ResponseMs = 55.44,
        LogLine = "Log line from log event"
      });

      DCShared.Log(new DCLogEvent {
        Hostname = "hostname",
        Filename = AppDomain.CurrentDomain.BaseDirectory,
        LogLevel = "crit",
        UserTag = "user444",
        RemoteAddress = "1.2.3.4",
        ResponseBytes = 22,
        ResponseMs = 55.44,
        LogLine = "Second event with fewer overrides"
      });


      DCShared.Instance.ServerVersion = "s123";
      DCShared.Instance.ConfigVersion = "c345";
      Console.WriteLine("set server, config");

      DCShared.Event(new DCEvent {
        GroupTag = "group",
        Kingdom = "event",
        Phylum = "phylum",
        Class = "class",
        Order = "order",
        Family = "family",
        Genus = "genus",
        Species = "species",
        Float1 = 1.0,
        Float2 = 2.0,
        Float3 = 3.0,
        Float4 = 4.0,
      });
      DCShared.Economy(new DCEconomy {
        GroupTag = "group",
        Kingdom = "economy",
        Phylum = "phylum",
        Class = "class",
        Order = "order",
        Family = "family",
        Genus = "genus",
        Species = "species",
        Float1 = 1.0,
        Float2 = 2.0,
        Float3 = 3.0,
        Float4 = 4.0,
        SpendType = "cash_purchase",
        SpendCurrency = "USD",
        SpendAmount = 123.45
      });
      DCShared.MessageSend(new DCMessageSend {
        GroupTag = "group",
        Kingdom = "message_send",
        Phylum = "phylum",
        Class = "class",
        Order = "order",
        Family = "family",
        Genus = "genus",
        Species = "species",
        Float1 = 1.0,
        Float2 = 2.0,
        Float3 = 3.0,
        Float4 = 4.0,
        Network = "email",
        Channel = "ses",
        FromTag = "server",
        ToList = new List<string> { "123" }
      });

      DCShared.MessageClick(new DCMessageClick {
        GroupTag = "group",
        Kingdom = "message_click",
        Phylum = "phylum",
        Class = "class",
        Order = "order",
        Family = "family",
        Genus = "genus",
        Species = "species",
        Float1 = 1.0,
        Float2 = 2.0,
        Float3 = 3.0,
        Float4 = 4.0,
        Network = "facebook",
        FromTag = "456",
        ToTag = "123"
      });
      DCShared.Log(new DCLogEvent {
        Hostname = "host",
        Filename = "filename",
        LogLevel = "level",
        DeviceTag = "device123",
        UserTag = "user444",
        RemoteAddress = "1.2.3.4",
        ResponseBytes = 22,
        ResponseMs = 55.44,
        LogLine = "Log line from log event"
      });

      Console.WriteLine("done full events");
      Console.WriteLine("Sleep 1...");
      Thread.Sleep(1);
      await DCShared.Flush();

      DCShared.Event(new DCEvent { Kingdom = "before device and user tag", Species = "species" });
      DCShared.Log(new DCLogEvent { LogLine = "before device and user tag" });
      Console.WriteLine("pre-device and user tag reset");
      await DCShared.Flush();
      DCShared.Instance.DeviceTag = "device999";
      DCShared.Instance.UserTag = "user999";
      DCShared.Log(new DCLogEvent { LogLine = "after device tag" });
      DCShared.Log(new DCLogEvent { LogLine = "Another thingy" });
      DCShared.Event(new DCEvent { Kingdom = "after device and user tag", Species = "species" });
      Console.WriteLine("adter-device and user tag reset");

      await DCShared.Flush();
      Console.WriteLine("done done");
    } catch (Exception ex) {
      Console.Error.WriteLine("Threw: {0}", ex);
    }
  }
}

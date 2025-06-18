using System;
using System.Collections.Concurrent;
using System.Threading;

internal class SerialTaskQueue : IDisposable {
  private readonly BlockingCollection<Action> _taskQueue =
      new BlockingCollection<Action>();
  private readonly Thread _workerThread;

  public string Name { get; }

  public SerialTaskQueue(string name) {
    Name = name;
    _workerThread =
        new Thread(ProcessQueue) { IsBackground = true, Name = name };
    _workerThread.Start();
  }

  private void ProcessQueue() {
    foreach (var task in _taskQueue.GetConsumingEnumerable()) {
      try {
        task();
      } catch (Exception ex) {
        Logger.Error($"Error in queue '{Name}': {ex}");
      }
    }
  }

  public void Run(Action action) {
    if (!_taskQueue.IsAddingCompleted) {
      _taskQueue.Add(action);
    }
  }

  public void Dispose() {
    _taskQueue.CompleteAdding();
    _workerThread.Join();
    _taskQueue.Dispose();
  }
}

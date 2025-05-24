namespace WpfLiftr.Business;

public class Simulation
{
  private readonly PriorityQueue<SimulationEvent, double> eventQueue = new();
  private double currentTime = 0d;
  
  public double CurrentTime => currentTime;

  public void ScheduleEvent(double time, EventType type, Action handler)
  {
    eventQueue.Enqueue(new SimulationEvent(time, type, handler), time);
  }

  public async Task RunAsync(double duration, double delay, CancellationToken cancellationToken)
  {
    while (eventQueue.Count > 0 && eventQueue.TryDequeue(out var e, out var time) && time <= duration)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }
      
      currentTime = time;
      e.Handler();
      
      await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
    }
  }
}

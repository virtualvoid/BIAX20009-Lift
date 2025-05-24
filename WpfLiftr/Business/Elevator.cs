using WpfLiftr.Common;

namespace WpfLiftr.Business;

public class Elevator
{
  private enum ElevatorState
  {
    AtFloor,
    InTransit,
    Returning
  }

  private readonly Simulation simulation;
  private readonly Queue<Person> hallQueue;
  private readonly int capacity;
  private readonly double rideTime;
  private readonly double returnTime;
  private readonly double retryDelay = 5.0d;
  private readonly double maxWaitTime = 90.0d; // maximum wait time before person gives up
  private readonly Action<LogEntry> log;

  private ElevatorState state = ElevatorState.AtFloor;
  private bool departureScheduled = false;

  public Elevator(Simulation simulation, Queue<Person> hallQueue, Action<LogEntry> log, int capacity = 4, double rideTime = 20, double returnTime = 15 /* slightly lower time when returning empty */)
  {
    this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
    this.hallQueue = hallQueue ?? throw new ArgumentNullException(nameof(hallQueue));
    this.log = log ?? throw new ArgumentNullException(nameof(log));
    this.capacity = capacity;
    this.rideTime = rideTime;
    this.returnTime = returnTime;
  }

  public void Start()
  {
    TryScheduleDeparture();
    ScheduleAbandonCheck();
  }

  public void NotifyPersonArrived()
  {
    if (state != ElevatorState.AtFloor || departureScheduled)
    {
      return;
    }

    log(new LogEntry("Passenger arrived, scheduling elevator departure.", LogEventType.PersonArrival));
    departureScheduled = true;
    simulation.ScheduleEvent(simulation.CurrentTime + retryDelay, EventType.ElevatorDeparture, TryDeparture);
  }

  private void TryScheduleDeparture()
  {
    if (state != ElevatorState.AtFloor || departureScheduled)
    {
      return;
    }

    departureScheduled = true;
    simulation.ScheduleEvent(simulation.CurrentTime + rideTime, EventType.ElevatorDeparture, TryDeparture);
  }

  private void TryDeparture()
  {
    if (state != ElevatorState.AtFloor)
    {
      log(new LogEntry("Elevator is busy, cannot depart.", LogEventType.Info));
      return;
    }

    departureScheduled = false;

    if (hallQueue.Count == 0)
    {
      log(new LogEntry("Elevator waiting – no passengers.", LogEventType.Info));
      TryScheduleDeparture();
      return;
    }

    var names = new List<string>();
    var count = Math.Min(capacity, hallQueue.Count);
    for (var i = 0; i < count; i++)
    {
      var person = hallQueue.Dequeue();
      person.DepartureTime = simulation.CurrentTime;
      names.Add(person.Name);
    }

    log(new LogEntry($"Elevator departed with {count} person(s): {string.Join(", ", names)} at {simulation.CurrentTime:F2}", LogEventType.ElevatorDeparture));
    state = ElevatorState.InTransit;

    simulation.ScheduleEvent(simulation.CurrentTime + rideTime, EventType.ElevatorDeparture, ReturnToFloor);
  }

  private void ReturnToFloor()
  {
    log((LogEntry)"Elevator returning to floor...");
    state = ElevatorState.Returning;

    simulation.ScheduleEvent(simulation.CurrentTime + returnTime, EventType.ElevatorDeparture, () =>
    {
      state = ElevatorState.AtFloor;
      log(new LogEntry("Elevator is back at the floor.", LogEventType.ElevatorReturn));
      
      TryScheduleDeparture();
    });
  }

  private void ScheduleAbandonCheck()
  {
    simulation.ScheduleEvent(simulation.CurrentTime + 5.0, EventType.PersonDeparture, () =>
    {
      var abandoned = 0;

      while (hallQueue.Count > 0 && simulation.CurrentTime - hallQueue.Peek().ArrivalTime > maxWaitTime)
      {
        var left = hallQueue.Dequeue();
        log(new LogEntry($"{left.Name} left the building after waiting too long ({simulation.CurrentTime - left.ArrivalTime:F2}s).", LogEventType.PersonAbandoned));
        abandoned++;
      }

      if (abandoned > 0)
      {
        NotifyPersonArrived(); // maybe more people arrived
      }

      ScheduleAbandonCheck(); // continue checking for abandoned persons
    });
  }
}

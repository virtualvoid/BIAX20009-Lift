using Bogus;
using WpfLiftr.Common;

namespace WpfLiftr.Business;

public class PersonGenerator(Simulation simulation, Elevator elevator, Queue<Person> hallQueue, Action<LogEntry> log)
{
  private readonly Random random = Random.Shared;
  private readonly Faker faker = new();

  private readonly Simulation simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
  private readonly Elevator elevator = elevator ?? throw new ArgumentNullException(nameof(elevator));
  private readonly Queue<Person> hallQueue = hallQueue ?? throw new ArgumentNullException(nameof(hallQueue));
  private readonly Action<LogEntry> log = log ?? throw new ArgumentNullException(nameof(log));

  public void Start() => ScheduleNextArrival();

  private void ScheduleNextArrival()
  {
    var interval = 5 + random.NextDouble() * 5;
    simulation.ScheduleEvent(
      simulation.CurrentTime + interval, 
      EventType.PersonArrival,
      () =>
      {
        var newArriveesCount = random.Next(1, 7); 
        for (var i = 0; i < newArriveesCount; i++)
        {
          var person = new Person
          {
            ArrivalTime = simulation.CurrentTime,
            Name = faker.Name.FullName(),
          };
        
          hallQueue.Enqueue(person);
          log(new LogEntry($"{person.Name} arrived at {simulation.CurrentTime:F2}", LogEventType.PersonArrival));
        }
        
        elevator.NotifyPersonArrived();
        ScheduleNextArrival();
      }
    );
  }
}

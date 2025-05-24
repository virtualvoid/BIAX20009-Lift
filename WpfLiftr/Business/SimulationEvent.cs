namespace WpfLiftr.Business;

public record SimulationEvent(double Time, EventType Type, Action Handler);

namespace WpfLiftr.Common;

public class LogEntry
{
  public string Message { get; set; }
  
  public LogEventType Type { get; set; } = LogEventType.Info;

  public LogEntry(string message)
  {
    Message = message;
  }
  
  public LogEntry(string message, LogEventType type)
  {
    Message = message;
    Type = type;
  }

  public static explicit operator LogEntry(string message) => new(message);
}

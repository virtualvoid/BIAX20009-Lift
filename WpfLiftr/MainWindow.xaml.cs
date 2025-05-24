using System.Collections.ObjectModel;
using System.Windows;
using WpfLiftr.Business;
using WpfLiftr.Common;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace WpfLiftr;

public partial class MainWindow : Window
{
  private CancellationTokenSource? cancellationTokenSource;

  public ObservableCollection<LogEntry> LogEntries { get; } = [];
  public ObservableCollection<ObservablePoint> queuePoints = new();

  public ISeries[] QueueSeries { get; set; }
  public Axis[] XAxes { get; set; }
  public Axis[] YAxes { get; set; }
  private readonly LineSeries<ObservablePoint> queueLineSeries;

  private Simulation simulation = null!;
  private Queue<Person> queue = null!;

  public int QueueCount => queue?.Count ?? 0;

  public MainWindow()
  {
    InitializeComponent();

    queueLineSeries = new LineSeries<ObservablePoint>
    {
      Values = queuePoints,
      GeometrySize = 4,
      Fill = null,
      Name = "Queue Size"
    };

    QueueSeries = new ISeries[] { queueLineSeries };
    
    XAxes = new[]
    {
      new Axis
      {
        LabelsPaint = new SolidColorPaint(SKColors.Black),
        Name = "Time (s)"
      }
    };
    YAxes = new[]
    {
      new Axis
      {
        LabelsPaint = new SolidColorPaint(SKColors.Black),
        Name = "Queue Size"
      }
    };

    DataContext = this;
  }

  private void MainWindow_OnClosed(object? sender, EventArgs e)
  {
    cancellationTokenSource?.Cancel();
  }

  private async void StartSimulation_Click(object sender, RoutedEventArgs e)
  {
    cancellationTokenSource = new CancellationTokenSource();
    LogEntries.Clear();

    var duration = DurationSlider.Value;
    var delay = (int)DelaySlider.Value;

    simulation = new Simulation();
    queue = new Queue<Person>();

    var elevator = new Elevator(simulation, queue, AddLog);
    var personGenerator = new PersonGenerator(simulation, elevator, queue, AddLog);

    personGenerator.Start();
    elevator.Start();

    try
    {
      await simulation.RunAsync(duration, delay, cancellationTokenSource.Token);

      AddLog((LogEntry)"Simulation finished.");
    }
    catch
    {
      AddLog((LogEntry)"Simulation stopped.");
    }
  }

  private void StopSimulation_Click(object sender, RoutedEventArgs e)
  {
    cancellationTokenSource?.Cancel();
  }

  private void AddLog(LogEntry logEntry)
  {
    ArgumentNullException.ThrowIfNull(logEntry, nameof(logEntry));

    Application.Current.Dispatcher.Invoke(() =>
    {
      LogEntries.Add(logEntry);
      LogListBox.ScrollIntoView(LogListBox.Items[^1]);
      
      queuePoints.Add(new ObservablePoint(simulation.CurrentTime, QueueCount));
    });
  }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using NodeHiveCenter.Models;
using NodeHiveCenter.Services;

namespace NodeHiveCenter.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    // ── Services ─────────────────────────────────────────────────────────────
    private readonly SupabaseRealtimeService _realtimeService;
    private readonly SupabaseRestService     _restService;
    private readonly GeminiApiService        _geminiService;

    // ── State guard ──────────────────────────────────────────────────────────
    private bool _isProcessing = false;
    private DispatcherTimer? _progressTimer;

    // ── Observable Properties ────────────────────────────────────────────────
    private string _promptText = "Awaiting incoming request...";
    public string PromptText { get => _promptText; set => Set(ref _promptText, value); }

    private string _currentPhase = "";
    public string CurrentPhase { get => _currentPhase; set => Set(ref _currentPhase, value); }

    private string _nodeAStatus = "IDLE";
    public string NodeAStatus { get => _nodeAStatus; set => Set(ref _nodeAStatus, value); }

    private string _nodeBStatus = "IDLE";
    public string NodeBStatus { get => _nodeBStatus; set => Set(ref _nodeBStatus, value); }

    private double _nodeAProgress = 0;
    public double NodeAProgress { get => _nodeAProgress; set => Set(ref _nodeAProgress, value); }

    private double _nodeBProgress = 0;
    public double NodeBProgress { get => _nodeBProgress; set => Set(ref _nodeBProgress, value); }

    private bool _isLinesActive = false;
    public bool IsLinesActive { get => _isLinesActive; set => Set(ref _isLinesActive, value); }

    private bool _isConnected = false;
    public bool IsConnected { get => _isConnected; set => Set(ref _isConnected, value); }

    private string _resultText = "";
    public string ResultText { get => _resultText; set => Set(ref _resultText, value); }

    public ObservableCollection<string> NodeALog { get; } = new();
    public ObservableCollection<string> NodeBLog { get; } = new();

    // ── Constructor ──────────────────────────────────────────────────────────
    public DashboardViewModel()
    {
        _restService    = new SupabaseRestService();
        _geminiService  = new GeminiApiService();
        _realtimeService = new SupabaseRealtimeService();

        _realtimeService.OnConnected    += () => Dispatch(() => IsConnected = true);
        _realtimeService.OnDisconnected += () => Dispatch(() => IsConnected = false);
        _realtimeService.OnSessionUpdated += session =>
            Dispatch(async () => await HandleSessionUpdated(session));

        _ = _realtimeService.StartAsync();
    }

    // ── Session handler ──────────────────────────────────────────────────────
    private async Task HandleSessionUpdated(SessionState session)
    {
        if (session.Status == "received" && !_isProcessing)
        {
            _isProcessing = true;
            try   { await RunOrchestration(session.Prompt ?? ""); }
            finally { _isProcessing = false; }
        }
    }

    // ── Main orchestration ───────────────────────────────────────────────────
    private async Task RunOrchestration(string prompt)
    {
        // ─ RECEIVED ─
        PromptText   = prompt;
        CurrentPhase = "received";
        NodeAStatus  = "IDLE";
        NodeBStatus  = "IDLE";
        NodeAProgress = 0;
        NodeBProgress = 0;
        IsLinesActive = false;
        ResultText    = "";

        NodeALog.Clear();
        NodeBLog.Clear();
        Log(NodeALog, "Session received. Standby...");
        Log(NodeBLog, "Session received. Standby...");

        await Task.Delay(600);

        // ─ SPLITTING ─
        CurrentPhase = "splitting";
        NodeAStatus  = "IDLE";
        NodeBStatus  = "IDLE";
        Log(NodeALog, "Initializing node worker...");
        Log(NodeBLog, "Initializing node worker...");

        await _restService.UpdateSessionAsync(new Dictionary<string, object?>
        {
            { "status",        "splitting" },
            { "node_a_status", "idle"      },
            { "node_b_status", "idle"      }
        });

        await Task.Delay(800);

        // ─ PROCESSING ─
        CurrentPhase  = "processing";
        NodeAStatus   = "PROCESSING";
        NodeBStatus   = "PROCESSING";
        IsLinesActive = true;
        Log(NodeALog, "Task assigned. Processing prompt...");
        Log(NodeBLog, "Task assigned. Processing prompt...");

        await _restService.UpdateSessionAsync(new Dictionary<string, object?>
        {
            { "status",        "processing" },
            { "node_a_status", "processing" },
            { "node_b_status", "processing" }
        });

        // Animate progress bars while Gemini is running
        StartProgressAnimation();

        var geminiResponse = await _geminiService.GenerateAsync(prompt);

        StopProgressAnimation();
        NodeAProgress = 100;
        NodeBProgress = 100;
        Log(NodeALog, "Processing complete.");
        Log(NodeBLog, "Processing complete.");

        // ─ AGGREGATING ─
        CurrentPhase = "aggregating";
        await _restService.UpdateSessionAsync(new Dictionary<string, object?>
        {
            { "status", "aggregating" }
        });

        Log(NodeALog, "Aggregating result...");
        Log(NodeBLog, "Aggregating result...");

        await Task.Delay(700);

        // ─ DONE ─
        CurrentPhase  = "done";
        NodeAStatus   = "DONE";
        NodeBStatus   = "DONE";
        IsLinesActive = false;
        ResultText    = geminiResponse;
        Log(NodeALog, "Done.");
        Log(NodeBLog, "Done.");

        await _restService.UpdateSessionAsync(new Dictionary<string, object?>
        {
            { "status",        "done"         },
            { "result",        geminiResponse  },
            { "node_a_status", "done"          },
            { "node_b_status", "done"          }
        });
    }

    // ── Progress animation ───────────────────────────────────────────────────
    private void StartProgressAnimation()
    {
        _progressTimer?.Stop();
        NodeAProgress = 0;
        NodeBProgress = 0;

        double elapsed  = 0;
        double duration = 8000; // advance to ~90% over 8 seconds

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _progressTimer.Tick += (_, _) =>
        {
            elapsed += 80;
            double pct = Math.Min(90, (elapsed / duration) * 90);
            NodeAProgress = pct;
            NodeBProgress = pct;
        };
        _progressTimer.Start();
    }

    private void StopProgressAnimation()
    {
        _progressTimer?.Stop();
        _progressTimer = null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static void Log(ObservableCollection<string> col, string message)
    {
        string ts = DateTime.Now.ToString("HH:mm:ss");
        col.Add($"[{ts}] {message}");
        // Keep last 20 lines
        while (col.Count > 20) col.RemoveAt(0);
    }

    private static void Dispatch(Action action)
        => Application.Current?.Dispatcher.Invoke(action);

    private static void Dispatch(Func<Task> action)
        => Application.Current?.Dispatcher.InvokeAsync(action);

    // ── INotifyPropertyChanged ───────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

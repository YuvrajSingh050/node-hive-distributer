# NodeHive Center

> **Orchestration dashboard for NodeHive** — a distributed computing platform that turns idle devices into a shared AI compute network.

NodeHive Center is a Windows desktop application built with **C# WPF (.NET 8)**. It acts as the real-time command hub: it listens for incoming AI tasks from a Supabase backend, animates the work distribution across compute nodes, calls the Gemini API to process the prompt, and streams status updates back — all visualised live in a dark-themed dashboard.

---

## Screenshots

> _Add screenshots here once the app is running._

| Dashboard idle | Task in progress |
|---|---|
| _(screenshot)_ | _(screenshot)_ |

---

## How It Works

```
Supabase Realtime (WebSocket)
        │
        ▼  task arrives on session table (row id = 1)
┌───────────────────┐
│   CENTER node     │  ← animates task distribution
│  ┌────┐  ┌────┐  │
│  │ A  │  │ B  │  │  ← NODE A and NODE B receive sub-tasks
│  └────┘  └────┘  │
└───────────────────┘
        │
        ▼  Gemini API processes the prompt
        │
        ▼  status phases
   received → splitting → processing → aggregating → done
        │
        ▼  result written back to Supabase via REST
```

1. **Supabase Realtime** — a persistent WebSocket connection subscribes to `postgres_changes` on the `session` table. When a new prompt lands (column `status = 'received'`), the dashboard wakes up.
2. **Network diagram animation** — `NetworkDiagramControl` renders and animates the CENTER → NODE A / NODE B topology using WPF Canvas drawing, pulsing nodes and travelling packets.
3. **Gemini API call** — `GeminiApiService` sends the prompt via `HttpClient` and awaits the generated text response.
4. **Phase progression** — `DashboardViewModel` drives the UI through each task phase, updating node status labels and the phase tracker strip at the bottom of the window.
5. **REST write-back** — `SupabaseRestService` patches the `session` row with the final response and `status = 'done'`.

---

## Project Structure

```
NodeHiveCenter/
├── NodeHiveCenter.sln
├── NodeHiveCenter.csproj          # net8.0-windows, UseWPF=true, no NuGet deps
├── App.xaml                       # Resource dictionary wiring
├── App.xaml.cs
├── MainWindow.xaml                # Root shell: header, network diagram, phase bar
├── MainWindow.xaml.cs
│
├── Controls/
│   ├── NetworkDiagramControl.xaml      # Custom Canvas-based node topology view
│   └── NetworkDiagramControl.xaml.cs   # Animation logic (pulses, data packets)
│
├── Converters/
│   └── StatusConverters.cs        # IValueConverters: phase → brush, opacity, text
│
├── Models/
│   └── SessionState.cs            # POCO representing a session table row
│
├── Services/
│   ├── SupabaseRealtimeService.cs  # WebSocket client (System.Net.WebSockets)
│   ├── SupabaseRestService.cs      # HTTP PATCH/GET via System.Net.Http
│   └── GeminiApiService.cs         # Gemini generateContent REST call
│
├── Themes/
│   └── DarkTheme.xaml             # Full resource dictionary: colors, styles, templates
│
└── ViewModels/
    └── DashboardViewModel.cs       # MVVM brain: INotifyPropertyChanged, phase logic
```

---

## Requirements

| Requirement | Version |
|---|---|
| .NET SDK | **8.0** or later |
| OS | Windows 10 / 11 (WPF is Windows-only) |
| IDE _(optional)_ | Visual Studio 2022+ or Rider |

> **No NuGet packages required.** The project relies entirely on BCL types:
> - `System.Net.Http` — `HttpClient` for REST calls to Supabase and Gemini
> - `System.Net.WebSockets` — `ClientWebSocket` for Supabase Realtime

---

## Environment Setup

Connection details are currently hardcoded inside the service files. Before running, open the relevant files and set your credentials:

**`Services/SupabaseRealtimeService.cs`**
```csharp
private const string SupabaseUrl  = "https://<your-project>.supabase.co";
private const string SupabaseKey  = "<your-anon-key>";
```

**`Services/SupabaseRestService.cs`**
```csharp
private const string SupabaseUrl  = "https://<your-project>.supabase.co";
private const string SupabaseKey  = "<your-anon-key>";
```

**`Services/GeminiApiService.cs`**
```csharp
private const string GeminiApiKey = "<your-gemini-api-key>";
```

> ⚠️ Do **not** commit real credentials. These constants should be moved to environment variables or a local config file (excluded via `.gitignore`) before sharing the repo.

---

## Running the App

```powershell
# From the repo root
dotnet run --project NodeHiveCenter.csproj
```

Or open `NodeHiveCenter.sln` in Visual Studio and press **F5**.

---

## Architecture Overview

The app follows the **MVVM** pattern:

```
View (XAML)  ──binds──▶  DashboardViewModel  ──calls──▶  Services
                 ▲                │
                 └── INotifyPropertyChanged ──────────────┘
```

- **Views** (`MainWindow`, `NetworkDiagramControl`) bind to observable properties and commands — zero business logic in code-behind.
- **ViewModel** (`DashboardViewModel`) owns the phase state machine, orchestrates service calls, and raises `PropertyChanged` to drive UI updates.
- **Services** are injected as constructor parameters and contain no UI references.
- **Converters** (`StatusConverters.cs`) translate phase strings into brushes, foregrounds, and opacity values directly in XAML bindings.

---

## Task Phase Reference

| Phase | Node A | Node B | Description |
|---|---|---|---|
| `received` | idle | idle | Prompt detected on Supabase |
| `splitting` | active | active | CENTER distributes sub-tasks |
| `processing` | working | working | Nodes call Gemini in parallel |
| `aggregating` | done | done | Results merged at CENTER |
| `done` | done | done | Response written back to Supabase |

---

## License

MIT — see `LICENSE` for details.

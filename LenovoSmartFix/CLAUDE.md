# Lenovo SmartFix

Pre-support resolution extension for Lenovo Windows 11 laptops.

## Projects

| Project | Type | Purpose |
|---------|------|---------|
| `LenovoSmartFix.Core` | .NET 8 class library | Domain models, interfaces, rule types — no Windows dependencies |
| `LenovoSmartFix.Service` | .NET 8 Windows Worker Service | Device/health collection, rules engine, remediation, SQLite persistence, named pipe IPC |
| `LenovoSmartFix.UI` | WinUI 3 (Windows App SDK) | Five-page UI; talks to Service via named pipe |

## Build requirements

- Windows 11 with Visual Studio 2022 17.8+
- Windows App SDK 1.5
- .NET 8 SDK
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Run

```
# 1. Apply EF migrations (first time)
cd LenovoSmartFix.Service
dotnet ef migrations add InitialCreate
dotnet ef database update

# 2. Start the service (run as Administrator for WMI access)
dotnet run --project LenovoSmartFix.Service

# 3. Start the UI
dotnet run --project LenovoSmartFix.UI
```

## Architecture

```
UI (WinUI 3)
  └─ SmartFixServiceProxy (named pipe client)
       └─ NamedPipeServer ──► IpcMessageHandler ──► SmartFixCoreService
                                                          │
                              ┌───────────────────────────┤
                              │                           │
                         Collectors                  RulesEngine
                    DeviceCollector              (5 rule families)
                    HealthCollector                       │
                    UpdateCollector                RemediationExecutor
                              │                           │
                          SmartFixDbContext    EscalationPacketBuilder
                          (SQLite via EF)         (JSON + QuestPDF)
```

## Key design decisions

- **Rules-first**: No AI in V1. Every decision is explainable from concrete signals.
- **Local-first privacy**: Nothing leaves the device unless the user explicitly exports a packet.
- **Narrow remediation**: Only Safe-level actions run automatically. Consent-level actions require explicit user approval.
- **Vantage integration**: Update flows defer to Lenovo Vantage rather than re-implementing BIOS/driver orchestration.
- **Thresholds in config**: All numeric thresholds live in `appsettings.json` under `SmartFix:Thresholds` for easy tuning without code changes.

## Rule IDs

| ID | Family | Condition |
|---|---|---|
| PERF-001 | Performance | High startup count + high CPU |
| PERF-002 | Performance | Sustained high CPU (non-thermal) |
| PERF-003 | Performance | Low free storage |
| PERF-004 | Performance | High RAM pressure |
| BATT-001 | Battery | Poor battery health |
| BATT-002 | Battery | High background load on battery |
| BATT-003 | Battery | Non-recommended power plan |
| NET-001 | Network | Unstable Wi-Fi (reconnect count) |
| NET-002 | Network | Outdated network adapter driver |
| STAB-001 | Stability | Repeated app crashes |
| STAB-002 | Stability | System crashes / unexpected restarts |
| STAB-003 | Stability | Thermal throttling |
| STAB-004 | Stability | Recurring issue after prior escalation |
| UPD-001 | Updates | Outdated BIOS |
| UPD-002 | Updates | Outdated critical drivers |
| UPD-003 | Updates | Outdated Lenovo utilities |
| UPD-004 | Updates | Pending Windows updates |

## Data retention

SQLite at `%LOCALAPPDATA%\LenovoSmartFix\smartfix.db`. Records older than 30 days are purged automatically on each service start.

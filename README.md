# wmux

AI-friendly terminal for Windows — a native WinUI 3 port of [cmux](https://github.com/manaflow-ai/cmux).

Built for developers running multiple AI coding agents simultaneously.

## Features

- **Vertical sidebar** with workspace tabs — shows git branch, PR status, listening ports, and latest notification per workspace
- **Split panes** — horizontal and vertical splits (like tmux)
- **Notification system** — panes get a blue ring when agents need attention; parses OSC 9/99/777 sequences
- **Integrated browser** — WebView2 panel alongside your terminal
- **CLI tool** — `wmux new`, `wmux notify`, `wmux split`, `wmux open` — communicates via Named Pipe
- **Session persistence** — workspaces restored on restart
- **Mica backdrop** — native Windows 11 material design via WinUI 3

## Tech stack

| Layer | Technology |
|-------|-----------|
| UI framework | WinUI 3 (Windows App SDK) |
| Terminal engine | ConPTY (Windows Pseudo Console) |
| Terminal renderer | xterm.js via WebView2 |
| Browser | WebView2 |
| Git info | LibGit2Sharp |
| CLI | System.CommandLine |
| MVVM | CommunityToolkit.Mvvm |

## Requirements

- Windows 10 1809+ / Windows 11
- Visual Studio 2022 with **Windows App SDK** workload
- .NET 9 SDK

## Build

```
git clone https://github.com/aasm3535/wmux
cd wmux
# Open wmux.sln in Visual Studio 2022
# Set wmux as startup project, build and run
```

## CLI usage

```bash
wmux new              # new workspace
wmux new --cwd C:\projects\foo

wmux split -v         # vertical split
wmux split -h         # horizontal split

wmux notify "Claude" "Task complete"
wmux open "https://localhost:3000"
```

## License

AGPL-3.0 — same as cmux.

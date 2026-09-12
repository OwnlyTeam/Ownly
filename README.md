# Ownly

**Free, reversible tweaking and debloat tool for Windows 10 and 11.** Every change is explained
before it runs, carries a risk label, and — where Windows allows it — can be undone with one click.

> **Ownly 1.1.0.** Ownly changes Windows settings on your PC. Read the disclaimer it shows
> on first run. You are responsible for changes you make to your own system.

## Download

Grab `Ownly.exe` from the [latest release](../../releases/latest). It's a single self-contained
file — no installer. Put it anywhere and double-click. Delete the file to remove it.

- Windows 10 (build 1809+) or Windows 11, 64-bit (x64)
- ~470 MB (bundles its own runtime so nothing else has to be installed)
- No account, no sign-in, no network connection required

Ownly is **not code-signed yet**, so SmartScreen may warn you the first time you run
it — click **More info → Run anyway**.

## What it does

| Section | Examples |
|---|---|
| **Clean** | Remove web results from Start search, turn off Copilot, stop auto-installing suggested apps, scan for temp files / startup items / optional apps |
| **Customize** | Classic right-click menu, left-align taskbar, show file extensions, seconds in the clock, This PC on the desktop |
| **Optimize** | Best-performance visual effects, Game Mode, remove menu/startup delays, turn off Fast Startup, disable hibernation, power modes, startup manager |
| **Privacy** | Minimum diagnostic data, limit advertising ID / activity history / cloud search, turn off online speech recognition, deny camera / mic / location |
| **Tools** | Flush DNS, restart Explorer, clear the Update cache, create a restore point, run SFC and DISM |

Around 40+ reversible options in total.

## How it stays safe

- **First run:** a disclaimer you scroll through and accept before the app opens.
- **Every change** lands in the **Changes** tab with a one-click **Restore**. User-level settings
  apply instantly; machine-level ones ask for a Windows permission prompt (and so does undoing them).
- **The Activity tab** logs every action — and every undo — with the exact registry write or command
  it ran, and whether it needed administrator rights.

Ownly keeps its data in `%LocalAppData%\Ownly` (the change log, activity log, and any file backups).

## Build from source

Requires the .NET 8 SDK and the Windows App SDK workload.

```powershell
dotnet publish .\Ownly.csproj -c Release -r win-x64 -p:Platform=x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true
```

## Not affiliated with Microsoft

Ownly is an independent project. It is not created by, affiliated with, or endorsed by Microsoft
Corporation. "Windows" is a trademark of Microsoft Corporation, used here only to describe
compatibility.

## License

[MIT](LICENSE) — provided as-is, with no warranty. See the full terms inside the app.

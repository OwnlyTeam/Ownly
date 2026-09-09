using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace Ownly.Core.Tweaks;

public sealed record ElevatedResult(bool Success, bool Cancelled, int ExitCode, string Output);

/// <summary>
/// Runs a PowerShell script with a UAC elevation prompt. Output is captured by having the script
/// transcript itself to a file (stdout cannot be redirected while UseShellExecute is required for
/// the "runas" verb).
/// </summary>
public static class ElevatedRunner
{
    public static bool IsProcessElevated()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string WorkDir
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Ownly", "run");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>
    /// Runs <paramref name="script"/> in a hidden PowerShell process. If elevation is needed a UAC
    /// prompt appears; there is no visible console. Output is read back from a transcript file.
    /// When <paramref name="wait"/> is false the call returns as soon as the process is launched.
    /// </summary>
    public static ElevatedResult Run(string script, bool elevated, bool showWindow, bool wait, int timeoutSeconds = 90)
    {
        _ = showWindow; // the console is always hidden now
        var id = Guid.NewGuid().ToString("N");
        var scriptPath = Path.Combine(WorkDir, id + ".ps1");
        var transcriptPath = Path.Combine(WorkDir, id + ".log");

        var wrapped = new StringBuilder();
        wrapped.AppendLine("$ErrorActionPreference = 'Continue'");
        wrapped.AppendLine($"try {{ Start-Transcript -Path '{transcriptPath}' -Force | Out-Null }} catch {{ }}");
        wrapped.AppendLine("try {");
        wrapped.AppendLine(script);
        wrapped.AppendLine("} catch { Write-Output ('Ownly: ' + $_.Exception.Message) }");
        wrapped.AppendLine("try { Stop-Transcript | Out-Null } catch { }");
        File.WriteAllText(scriptPath, wrapped.ToString());

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };
        if (elevated && !IsProcessElevated())
        {
            psi.Verb = "runas";
        }

        try
        {
            var process = Process.Start(psi);
            if (process is null)
            {
                return new ElevatedResult(false, false, -1, "PowerShell could not be started.");
            }

            if (!wait)
            {
                return new ElevatedResult(true, false, 0, "Started — this one runs in the background.");
            }

            if (!process.WaitForExit(timeoutSeconds * 1000))
            {
                return new ElevatedResult(false, false, -1, "This is taking longer than expected. It may still be running in the background.");
            }

            var output = ReadTranscript(transcriptPath);
            TryDelete(scriptPath);
            return new ElevatedResult(process.ExitCode == 0, false, process.ExitCode, output);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            TryDelete(scriptPath);
            return new ElevatedResult(false, true, 1223, "You cancelled the Windows permission prompt, so nothing was changed.");
        }
        catch (Exception ex)
        {
            return new ElevatedResult(false, false, -1, ex.Message);
        }
    }

    private static string ReadTranscript(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            var text = File.ReadAllText(path);
            TryDelete(path);

            // A PowerShell transcript is: ***header***  <output>  ***footer***. Keep only <output>.
            const string bar = "**********************";
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var barLines = new List<int>();
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(bar, StringComparison.Ordinal))
                {
                    barLines.Add(i);
                }
            }

            int bodyStart, bodyEnd;
            if (barLines.Count >= 3)
            {
                bodyStart = barLines[1] + 1;             // line after the 2nd bar (end of header)
                bodyEnd = barLines[barLines.Count - 2];  // the bar that begins the footer
            }
            else
            {
                bodyStart = 0;
                bodyEnd = lines.Length;
            }

            var body = string.Join("\n", lines[bodyStart..Math.Max(bodyStart, bodyEnd)]);
            // drop the leftover "Transcript started, output file is …" line if present
            body = string.Join("\n", body.Split('\n')
                .Where(l => !l.StartsWith("Transcript started", StringComparison.Ordinal)));
            return body.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { }
    }
}

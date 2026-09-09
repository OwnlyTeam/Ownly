using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Ownly.Core.Optimize;

public sealed record PowerModeDefinition(string Id, string Name, string Description, string Guid);

public sealed record ActivePowerMode(string Guid, string Name);

public sealed class PowerModeService
{
    public static IReadOnlyList<PowerModeDefinition> Modes { get; } = new[]
    {
        new PowerModeDefinition("balanced", "Balanced", "A sensible default for everyday use.", "381b4222-f694-41f0-9685-ff5bb260df2e"),
        new PowerModeDefinition("performance", "Best Performance", "Prioritize responsiveness and sustained performance.", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
        new PowerModeDefinition("saver", "Power Saver", "Prioritize efficiency and longer battery life.", "a1841308-3541-4fab-bc81-f71556f20b4a")
    };

    public ActivePowerMode ReadActive()
    {
        var result = RunPowerCfg("/getactivescheme");
        var match = Regex.Match(result.Output, @"([0-9a-fA-F-]{36})\s+\(([^)]+)\)");
        return match.Success
            ? new ActivePowerMode(match.Groups[1].Value, match.Groups[2].Value)
            : new ActivePowerMode(string.Empty, "Unknown");
    }

    public bool ApplyGuid(string schemeGuid)
    {
        if (!Guid.TryParse(schemeGuid, out _))
        {
            return false;
        }

        return RunPowerCfg($"/setactive {schemeGuid}").ExitCode == 0;
    }

    private static (int ExitCode, string Output) RunPowerCfg(string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, string.Empty);
        }
    }
}

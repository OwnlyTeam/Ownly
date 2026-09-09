using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Ownly.Core.Clean;

public sealed record TemporaryFileSummary(int FileCount, long TotalBytes, string TempRoot);

public sealed record TemporaryQuarantineResult(
    bool Success,
    string Message,
    string TempRoot,
    string? BackupPath,
    int MovedCount,
    long MovedBytes);

public sealed record TemporaryRestoreResult(bool Success, string Message, int RestoredCount);

/// <summary>
/// Moves eligible user temporary files into an Ownly-owned backup instead of deleting them.
/// The manifest keeps each original relative path so the operation can be restored.
/// </summary>
public sealed class TemporaryFileService
{
    private const string BackupRootName = "CleanupBackups";
    private const string ManifestName = "manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public TemporaryFileSummary Preview()
    {
        var tempRoot = GetTempRoot();
        var files = EnumerateEligibleFiles(tempRoot).ToList();
        return new TemporaryFileSummary(files.Count, files.Sum(file => file.Size), tempRoot);
    }

    public TemporaryQuarantineResult Quarantine()
    {
        var tempRoot = GetTempRoot();
        var candidates = EnumerateEligibleFiles(tempRoot).ToList();
        if (candidates.Count == 0)
        {
            return new TemporaryQuarantineResult(true, "No eligible temporary files were found.", tempRoot, null, 0, 0);
        }

        var backupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ownly",
            BackupRootName,
            Guid.NewGuid().ToString("N"));
        var moved = new List<TemporaryMovedFile>();
        long movedBytes = 0;

        try
        {
            Directory.CreateDirectory(backupPath);
            foreach (var candidate in candidates)
            {
                var relativePath = Path.GetRelativePath(tempRoot, candidate.Path);
                var destination = Path.Combine(backupPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(candidate.Path, destination);
                moved.Add(new TemporaryMovedFile(relativePath, candidate.Size));
                movedBytes += candidate.Size;
            }

            var manifest = new TemporaryBackupManifest(tempRoot, moved);
            var manifestTemporaryPath = Path.Combine(backupPath, ManifestName + ".tmp");
            var manifestPath = Path.Combine(backupPath, ManifestName);
            File.WriteAllText(manifestTemporaryPath, JsonSerializer.Serialize(manifest, JsonOptions));
            File.Move(manifestTemporaryPath, manifestPath, overwrite: true);

            return new TemporaryQuarantineResult(
                true,
                $"Moved {moved.Count} temporary file{(moved.Count == 1 ? "" : "s")} to an Ownly backup. You can restore them from Changes.",
                tempRoot,
                backupPath,
                moved.Count,
                movedBytes);
        }
        catch (Exception exception)
        {
            RestoreMovedFiles(backupPath, tempRoot, moved);
            TryDeleteDirectory(backupPath);
            return new TemporaryQuarantineResult(
                false,
                $"Ownly could not finish the temporary-file cleanup. No completed change was logged. {exception.Message}",
                tempRoot,
                null,
                0,
                0);
        }
    }

    public TemporaryRestoreResult Restore(string backupPath, string tempRoot)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || string.IsNullOrWhiteSpace(tempRoot))
        {
            return new TemporaryRestoreResult(false, "The Ownly cleanup backup is missing its original location.", 0);
        }

        try
        {
            var manifestPath = Path.Combine(backupPath, ManifestName);
            if (!File.Exists(manifestPath))
            {
                return new TemporaryRestoreResult(false, "The Ownly cleanup manifest could not be found.", 0);
            }

            var manifest = JsonSerializer.Deserialize<TemporaryBackupManifest>(File.ReadAllText(manifestPath));
            if (manifest is null || manifest.Files is null || !string.Equals(manifest.TempRoot, tempRoot, StringComparison.OrdinalIgnoreCase))
            {
                return new TemporaryRestoreResult(false, "The Ownly cleanup backup did not match the original temporary folder.", 0);
            }

            var restored = 0;
            foreach (var entry in manifest.Files)
            {
                var source = GetSafeChildPath(backupPath, entry.RelativePath);
                var destination = GetSafeChildPath(tempRoot, entry.RelativePath);
                if (!File.Exists(source))
                {
                    return new TemporaryRestoreResult(false, "One or more files in the Ownly cleanup backup are missing.", restored);
                }
                if (File.Exists(destination))
                {
                    return new TemporaryRestoreResult(false, $"Restore stopped because a file already exists at {destination}.", restored);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(source, destination);
                restored++;
            }

            File.Delete(manifestPath);
            TryDeleteDirectory(backupPath);
            return new TemporaryRestoreResult(true, $"Restored {restored} temporary file{(restored == 1 ? "" : "s")} to the original location.", restored);
        }
        catch (Exception exception)
        {
            return new TemporaryRestoreResult(false, $"Ownly could not restore the temporary-file backup: {exception.Message}", 0);
        }
    }

    private static IEnumerable<TemporaryFileCandidate> EnumerateEligibleFiles(string tempRoot)
    {
        if (!Directory.Exists(tempRoot))
        {
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(tempRoot);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            List<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory).ToList();
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                TemporaryFileCandidate? candidate = null;
                try
                {
                    if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }
                    candidate = new TemporaryFileCandidate(Path.GetFullPath(file), new FileInfo(file).Length);
                }
                catch
                {
                    // A file can disappear or be locked while a scan is in progress.
                }
                if (candidate is not null)
                {
                    yield return candidate;
                }
            }

            List<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(directory).ToList();
            }
            catch
            {
                continue;
            }

            foreach (var child in directories)
            {
                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                    {
                        pending.Push(child);
                    }
                }
                catch
                {
                    // Ignore directories that disappear or cannot be inspected.
                }
            }
        }
    }

    private static string GetTempRoot()
    {
        var root = Path.GetFullPath(Path.GetTempPath());
        if (string.IsNullOrWhiteSpace(root) || Path.GetPathRoot(root)!.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Windows returned an unsafe temporary-folder path.");
        }
        return root.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static string GetSafeChildPath(string root, string relativePath)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The cleanup manifest contains a path outside its approved folder.");
        }
        return fullPath;
    }

    private static void RestoreMovedFiles(string backupPath, string tempRoot, IEnumerable<TemporaryMovedFile> moved)
    {
        foreach (var entry in moved.Reverse())
        {
            try
            {
                var source = GetSafeChildPath(backupPath, entry.RelativePath);
                var destination = GetSafeChildPath(tempRoot, entry.RelativePath);
                if (File.Exists(source) && !File.Exists(destination))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Move(source, destination);
                }
            }
            catch
            {
                // Preserve the backup if an individual rollback cannot complete.
            }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // A failed cleanup must not hide the original action error.
        }
    }

    private sealed record TemporaryFileCandidate(string Path, long Size);
    public sealed record TemporaryMovedFile(string RelativePath, long Size);
    public sealed record TemporaryBackupManifest(string TempRoot, IReadOnlyList<TemporaryMovedFile> Files);
}

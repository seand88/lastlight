using System;
using System.IO;

namespace LastLight.Tools.Commands;

public static class GenerateSoundsCommand
{
    public static void Execute(string[] args)
    {
        string rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        string sourcePath = Path.Combine(rootPath, "LastLight.Client.Core/Assets/Audio");
        string targetPath = Path.Combine(rootPath, "LastLight.Client.Core/Content/Audio");

        if (!Directory.Exists(sourcePath))
        {
            Console.WriteLine($"Error: Source audio path not found: {sourcePath}");
            return;
        }

        // Ensure target base directory exists
        Directory.CreateDirectory(targetPath);

        Console.WriteLine("Processing Audio assets...");
        ProcessDirectory(sourcePath, sourcePath, targetPath);
    }

    private static void ProcessDirectory(string rootSourcePath, string currentSourcePath, string targetBasePath)
    {
        // 1. Process files in current directory
        var files = Directory.GetFiles(currentSourcePath, "*.*");
        
        // Determine relative path to maintain structure
        string relativePath = Path.GetRelativePath(rootSourcePath, currentSourcePath);
        string targetDir = relativePath == "." ? targetBasePath : Path.Combine(targetBasePath, relativePath);
        
        if (files.Length > 0)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                // Supported audio formats
                if (ext != ".wav" && ext != ".mp3" && ext != ".ogg") continue;

                string fileName = Path.GetFileName(file);
                string destPath = Path.Combine(targetDir, fileName);

                // Copy exactly as-is (overwrites existing)
                File.Copy(file, destPath, true);
                Console.WriteLine($"  Copied: {relativePath}/{fileName}");
            }
        }

        // 2. Recursively process subdirectories
        var subDirs = Directory.GetDirectories(currentSourcePath);
        foreach (var dir in subDirs)
        {
            ProcessDirectory(rootSourcePath, dir, targetBasePath);
        }
    }
}

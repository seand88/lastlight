using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LastLight.Tools.Commands;

public static class PackAssetsCommand
{
    private static string _rootPath = "";
    private static string _assetsPath = "";
    private static string _contentPath = "";
    private static string _mgcbPath = "";
    private static readonly HashSet<string> _writtenFiles = new();

    public static void Execute(string[] args)
    {
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        _assetsPath = Path.Combine(_rootPath, "LastLight.Client.Core/Assets");
        _contentPath = Path.Combine(_rootPath, "LastLight.Client.Core/Content");
        _mgcbPath = Path.Combine(_contentPath, "Content.mgcb");
        _writtenFiles.Clear();

        if (!Directory.Exists(_assetsPath))
        {
            Console.WriteLine($"Error: Assets directory not found at {_assetsPath}");
            return;
        }

        Console.WriteLine("--- LastLight Asset Packer ---");

        try 
        {
            // 1. Clean Stage
            CleanContentFolder();

            // 2. Process Assets
            ProcessAudio();
            ProcessGraphicsPack();
            ProcessGraphicsStatic();
            ProcessFonts();

            // 3. Generate fresh MGCB
            GenerateMgcbFile();

            Console.WriteLine("\nDone! Assets staged and Content.mgcb regenerated.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
            Console.WriteLine("Packing aborted to prevent data corruption.");
            Environment.Exit(1);
        }
    }

    private static void TrackFileWrite(string targetPath, string processName)
    {
        string relative = Path.GetRelativePath(_contentPath, targetPath).Replace("\\", "/");
        if (_writtenFiles.Contains(relative))
        {
            throw new Exception($"Conflict Detected! The file '{relative}' is being written to by '{processName}', but it was already created by a previous process. Please ensure static filenames do not collide with atlas names (atlas.png / atlas_map.json).");
        }
        _writtenFiles.Add(relative);
    }

    private static void CleanContentFolder()
    {
        Console.WriteLine("Cleaning Content folder...");
        if (Directory.Exists(_contentPath))
        {
            foreach (var dir in Directory.GetDirectories(_contentPath))
            {
                string name = Path.GetFileName(dir).ToLower();
                if (name == "bin" || name == "obj") continue;
                Directory.Delete(dir, true);
            }

            foreach (var file in Directory.GetFiles(_contentPath))
            {
                // We now wipe the MGCB file too as we regenerate it from scratch
                File.Delete(file);
            }
        }
        else
        {
            Directory.CreateDirectory(_contentPath);
        }
    }

    private static void ProcessAudio()
    {
        string source = Path.Combine(_assetsPath, "Audio");
        string target = Path.Combine(_contentPath, "Audio");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Audio...");
        CopyDirectoryRecursive(source, target, "Audio Copy", ".wav", ".mp3", ".ogg");
    }

    private static void ProcessGraphicsPack()
    {
        string source = Path.Combine(_assetsPath, "Graphics/Pack");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Graphics Atlases...");
        foreach (var dir in Directory.GetDirectories(source))
        {
            string atlasName = Path.GetFileName(dir);
            PackAtlas(dir, atlasName);
        }
    }

    private static void ProcessGraphicsStatic()
    {
        string source = Path.Combine(_assetsPath, "Graphics/Static");
        string target = Path.Combine(_contentPath, "Graphics");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Static Graphics...");
        CopyDirectoryRecursive(source, target, "Static Copy", ".png", ".jpg", ".jpeg");
    }

    private static void ProcessFonts()
    {
        string source = Path.Combine(_assetsPath, "Fonts");
        string target = Path.Combine(_contentPath, "Fonts");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Fonts...");
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source, "*.spritefont"))
        {
            string dest = Path.Combine(target, Path.GetFileName(file));
            TrackFileWrite(dest, "Font Copy");
            File.Copy(file, dest, true);
            Console.WriteLine($"  Copied font: Fonts/{Path.GetFileName(file)}");
        }
    }

    private static void PackAtlas(string inputDir, string atlasName)
    {
        var files = Directory.GetFiles(inputDir, "*.png").OrderBy(f => f).ToList();
        if (files.Count == 0) return;

        int cellW, cellH;
        using (var firstImage = Image.Load(files[0]))
        {
            cellW = firstImage.Width;
            cellH = firstImage.Height;
        }

        int columns = (int)Math.Ceiling(Math.Sqrt(files.Count));
        int rows = (int)Math.Ceiling((double)files.Count / columns);
        
        using var atlas = new Image<Rgba32>(columns * cellW, rows * cellH);
        var map = new Dictionary<string, object>();

        for (int i = 0; i < files.Count; i++)
        {
            string filePath = files[i];
            string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            string entryName = fileName;
            
            using var img = Image.Load<Rgba32>(filePath);
            if (img.Width != cellW || img.Height != cellH)
            {
                Console.WriteLine($"  Warning: Resizing {fileName} to {cellW}x{cellH}");
                img.Mutate(x => x.Resize(cellW, cellH));
            }

            int x = (i % columns) * cellW;
            int y = (i / columns) * cellH;

            atlas.Mutate(ctx => ctx.DrawImage(img, new Point(x, y), 1f));
            map[entryName] = new { X = x, Y = y, W = cellW, H = cellH };
        }

        string outputDir = Path.Combine(_contentPath, "Graphics", atlasName);
        Directory.CreateDirectory(outputDir);

        string pngPath = Path.Combine(outputDir, "atlas.png");
        string jsonPath = Path.Combine(outputDir, "atlas_map.json");

        TrackFileWrite(pngPath, $"Atlas Packing ({atlasName})");
        TrackFileWrite(jsonPath, $"Atlas Packing ({atlasName})");

        atlas.Save(pngPath);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"  Generated Atlas: {atlasName}");
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir, string processName, params string[] extensions)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            if (extensions.Length > 0 && !extensions.Contains(Path.GetExtension(file).ToLower())) continue;
            
            string dest = Path.Combine(targetDir, Path.GetFileName(file));
            TrackFileWrite(dest, processName);
            File.Copy(file, dest, true);
            Console.WriteLine($"  Copied: {Path.GetRelativePath(_contentPath, dest).Replace("\\", "/")}");
        }

        foreach (var sub in Directory.GetDirectories(sourceDir))
        {
            CopyDirectoryRecursive(sub, Path.Combine(targetDir, Path.GetFileName(sub)), processName, extensions);
        }
    }

    private static void GenerateMgcbFile()
    {
        Console.WriteLine("Generating Content.mgcb...");
        var sb = new StringBuilder();

        sb.AppendLine("#----------------------------- Global Properties ----------------------------#");
        sb.AppendLine("");
        sb.AppendLine("/outputDir:bin/$(Platform)");
        sb.AppendLine("/intermediateDir:obj/$(Platform)");
        sb.AppendLine("/platform:DesktopGL");
        sb.AppendLine("/config:");
        sb.AppendLine("/profile:Reach");
        sb.AppendLine("/compress:False");
        sb.AppendLine("");
        sb.AppendLine("#-------------------------------- References --------------------------------#");
        sb.AppendLine("");
        sb.AppendLine("");
        sb.AppendLine("#---------------------------------- Content ---------------------------------#");
        sb.AppendLine("");

        foreach (var relativePath in _writtenFiles.OrderBy(f => f))
        {
            string ext = Path.GetExtension(relativePath).ToLower();
            if (ext == ".json") continue; // Back to raw text maps
            
            sb.AppendLine($"#begin {relativePath}");
            
            if (ext == ".spritefont")
            {
                sb.AppendLine("/importer:FontDescriptionImporter");
                sb.AppendLine("/processor:FontDescriptionProcessor");
                sb.AppendLine("/processorParam:PremultiplyAlpha=True");
                sb.AppendLine("/processorParam:TextureFormat=Compressed");
            }
            else if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
            {
                sb.AppendLine("/importer:WavImporter");
                sb.AppendLine("/processor:SoundEffectProcessor");
                sb.AppendLine("/processorParam:Quality=Best");
            }
            else // Graphics
            {
                sb.AppendLine("/importer:TextureImporter");
                sb.AppendLine("/processor:TextureProcessor");
                sb.AppendLine("/processorParam:ColorKeyColor=255,0,255,255");
                sb.AppendLine("/processorParam:ColorKeyEnabled=True");
                sb.AppendLine("/processorParam:GenerateMipmaps=False");
                sb.AppendLine("/processorParam:PremultiplyAlpha=True");
                sb.AppendLine("/processorParam:ResizeToPowerOfTwo=False");
                sb.AppendLine("/processorParam:MakeSquare=False");
                sb.AppendLine("/processorParam:TextureFormat=Color");
            }

            sb.AppendLine($"/build:{relativePath}");
            sb.AppendLine("");
        }

        File.WriteAllText(_mgcbPath, sb.ToString());
    }
}

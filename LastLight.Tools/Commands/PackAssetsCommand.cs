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

public class AssetManifest 
{
    public Dictionary<string, string> sounds { get; set; } = new();
    public Dictionary<string, string> music { get; set; } = new();
    public Dictionary<string, string> staticImages { get; set; } = new();
    public Dictionary<string, string> fonts { get; set; } = new();
    public Dictionary<string, AtlasManifestEntry> atlases { get; set; } = new();
    public Dictionary<string, AnimationManifestEntry> animations { get; set; } = new();
}

public class AtlasManifestEntry { public string texture { get; set; } public string map { get; set; } }
public class AnimationManifestEntry { public string texture { get; set; } public string map { get; set; } }

public static class PackAssetsCommand
{
    private static string _rootPath = "";
    private static string _assetsPath = "";
    private static string _contentPath = "";
    private static string _mgcbPath = "";
    private static readonly HashSet<string> _writtenFiles = new();
    private static AssetManifest _manifest = new();

    public static void Execute(string[] args)
    {
        _rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        _assetsPath = Path.Combine(_rootPath, "LastLight.Client.Core/Assets");
        _contentPath = Path.Combine(_rootPath, "LastLight.Client.Core/Content");
        _mgcbPath = Path.Combine(_contentPath, "Content.mgcb");
        _writtenFiles.Clear();
        _manifest = new AssetManifest();

        if (!Directory.Exists(_assetsPath))
        {
            Console.WriteLine($"Error: Assets directory not found at {_assetsPath}");
            return;
        }

        Console.WriteLine("--- LastLight Asset Packer V1 ---");

        try 
        {
            CleanContentFolder();

            ProcessAudio();
            ProcessMusic();
            ProcessTextureAtlases();
            ProcessStaticImages();
            ProcessAnimations();
            ProcessFonts();

            GenerateMgcbFile();
            WriteManifest();

            Console.WriteLine("\nDone! Assets staged, Content.mgcb regenerated, and asset_manifest.json created.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("Packing aborted to prevent data corruption.");
            Environment.Exit(1);
        }
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
        string source = Path.Combine(_assetsPath, "Audio/SoundEffects");
        string target = Path.Combine(_contentPath, "Audio/SoundEffects");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Sound Effects...");
        CopyFiles(source, target, "SoundEffect Copy", _manifest.sounds, "Audio/SoundEffects", ".wav", ".mp3", ".ogg");
    }

    private static void ProcessMusic()
    {
        string source = Path.Combine(_assetsPath, "Audio/Songs");
        string target = Path.Combine(_contentPath, "Audio/Music");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Music...");
        CopyFiles(source, target, "Music Copy", _manifest.music, "Audio/Music", ".wav", ".mp3", ".ogg");
    }

    private static void ProcessStaticImages()
    {
        string source = Path.Combine(_assetsPath, "Graphics/Static/Images");
        string target = Path.Combine(_contentPath, "Graphics/Static/Images");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Static Images...");
        CopyFiles(source, target, "StaticImage Copy", _manifest.staticImages, "Graphics/Static/Images", ".png", ".jpg", ".jpeg");
    }

    private static void ProcessFonts()
    {
        string source = Path.Combine(_assetsPath, "Fonts");
        string target = Path.Combine(_contentPath, "Fonts");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Fonts...");
        CopyFiles(source, target, "Font Copy", _manifest.fonts, "Fonts", ".spritefont");
    }

    private static void ProcessTextureAtlases()
    {
        string source = Path.Combine(_assetsPath, "Graphics/Pack/TextureAtlases");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Texture Atlases...");
        foreach (var dir in Directory.GetDirectories(source))
        {
            string atlasName = Path.GetFileName(dir);
            PackAtlas(dir, atlasName);
        }
    }

    private static void ProcessAnimations()
    {
        string source = Path.Combine(_assetsPath, "Graphics/Static/Animations");
        if (!Directory.Exists(source)) return;

        Console.WriteLine("Processing Animations...");
        foreach (var dir in Directory.GetDirectories(source))
        {
            string entityName = Path.GetFileName(dir);
            string targetDir = Path.Combine(_contentPath, "Graphics/Animations", entityName);
            Directory.CreateDirectory(targetDir);

            foreach(var file in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(file).ToLower();
                string fileName = Path.GetFileName(file);
                string dest = Path.Combine(targetDir, fileName);
                TrackFileWrite(dest, "Animation Copy");
                File.Copy(file, dest, true);

                if (ext == ".png" || ext == ".jpg")
                {
                    string contentPath = $"Graphics/Animations/{entityName}/{Path.GetFileNameWithoutExtension(file)}";
                    if (!_manifest.animations.ContainsKey(entityName)) {
                        _manifest.animations[entityName] = new AnimationManifestEntry {
                            texture = contentPath,
                            map = $"Content/Graphics/Animations/{entityName}/animation_map.json" 
                        };
                    }
                }
                Console.WriteLine($"  Copied: {Path.GetRelativePath(_contentPath, dest).Replace("\\\\", "/")}");
            }
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
        var sprites = new Dictionary<string, object>();

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
            sprites[entryName] = new { x = x, y = y, w = cellW, h = cellH };
        }
        
        map["sprites"] = sprites;

        string outputDir = Path.Combine(_contentPath, "Graphics/TextureAtlases", atlasName);
        Directory.CreateDirectory(outputDir);

        string pngPath = Path.Combine(outputDir, "atlas.png");
        string jsonPath = Path.Combine(outputDir, "atlas_map.json");

        TrackFileWrite(pngPath, $"Atlas Packing ({atlasName})");
        TrackFileWrite(jsonPath, $"Atlas Packing ({atlasName})");

        atlas.Save(pngPath);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));

        _manifest.atlases[atlasName] = new AtlasManifestEntry {
            texture = $"Graphics/TextureAtlases/{atlasName}/atlas",
            map = $"Content/Graphics/TextureAtlases/{atlasName}/atlas_map.json"
        };

        Console.WriteLine($"  Generated Atlas: {atlasName}");
    }

    private static void CopyFiles(string sourceDir, string targetDir, string processName, Dictionary<string, string> manifestDict, string contentPrefix, params string[] extensions)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            if (extensions.Length > 0 && !extensions.Contains(Path.GetExtension(file).ToLower())) continue;
            
            string relPath = Path.GetRelativePath(sourceDir, file);
            string dest = Path.Combine(targetDir, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            TrackFileWrite(dest, processName);
            File.Copy(file, dest, true);

            string key = Path.GetFileNameWithoutExtension(file); 
            string manifestVal = $"{contentPrefix}/{relPath.Replace("\\", "/").Replace(Path.GetExtension(file), "")}";
            manifestDict[key] = manifestVal;

            Console.WriteLine($"  Copied: {Path.GetRelativePath(_contentPath, dest).Replace("\\", "/")}");
        }
    }

    private static void TrackFileWrite(string targetPath, string processName)
    {
        string relative = Path.GetRelativePath(_contentPath, targetPath).Replace("\\", "/");
        if (_writtenFiles.Contains(relative))
        {
            throw new Exception($"Conflict Detected! The file '{relative}' is being written to by '{processName}', but it was already created by a previous process.");
        }
        _writtenFiles.Add(relative);
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
            if (ext == ".json") continue; 
            
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
                if (relativePath.StartsWith("Audio/Music/"))
                {
                    sb.AppendLine("/importer:WavImporter");
                    sb.AppendLine("/processor:SongProcessor");
                    sb.AppendLine("/processorParam:Quality=Best");
                }
                else
                {
                    sb.AppendLine("/importer:WavImporter");
                    sb.AppendLine("/processor:SoundEffectProcessor");
                    sb.AppendLine("/processorParam:Quality=Best");
                }
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

    private static void WriteManifest()
    {
        string manifestPath = Path.Combine(_contentPath, "asset_manifest.json");
        var json = JsonSerializer.Serialize(_manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestPath, json);
        Console.WriteLine("Generated asset_manifest.json");
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LastLight.Tools.Commands;

public static class GenerateSpritesCommand
{
    public static void Execute(string[] args)
    {
        string rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        string assetsPath = Path.Combine(rootPath, "LastLight.Client.Core/Assets/Graphics");
        string outputPath = Path.Combine(rootPath, "LastLight.Client.Core/Assets/Output");

        if (!Directory.Exists(assetsPath))
        {
            Console.WriteLine($"Error: Assets path not found: {assetsPath}");
            return;
        }

        // Ensure clean output directory
        if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true);
        Directory.CreateDirectory(outputPath);

        // 1. Process "Final" folder (Direct copy with naming convention)
        string finalPath = Path.Combine(assetsPath, "Final");
        if (Directory.Exists(finalPath))
        {
            ProcessFinalAssets(finalPath, outputPath);
        }

        // 2. Process "Raw" folder (Packing into atlases)
        string rawPath = Path.Combine(assetsPath, "Raw");
        if (Directory.Exists(rawPath))
        {
            ProcessRawAssets(rawPath, outputPath);
        }
    }

    private static void ProcessFinalAssets(string sourceDir, string targetDir)
    {
        Console.WriteLine("Processing Final assets...");
        var subDirs = Directory.GetDirectories(sourceDir);
        foreach (var dir in subDirs)
        {
            string categoryLower = Path.GetFileName(dir).ToLower();
            var files = Directory.GetFiles(dir, "*.*");
            foreach (var file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;

                string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                string newFileName = $"{categoryLower}_{fileName}{ext}";
                string destPath = Path.Combine(targetDir, newFileName);

                File.Copy(file, destPath, true);
                Console.WriteLine($"  Copied: {newFileName}");
            }
        }
    }

    private static void ProcessRawAssets(string sourceDir, string targetDir)
    {
        Console.WriteLine("Processing Raw assets (Packing)...");
        var subDirs = Directory.GetDirectories(sourceDir);
        foreach (var dir in subDirs)
        {
            string category = Path.GetFileName(dir);
            PackCategory(dir, category, targetDir);
        }
    }

    private static void PackCategory(string inputDir, string category, string outputDir)
    {
        var files = Directory.GetFiles(inputDir, "*.png");
        if (files.Length == 0) return;

        int cellW, cellH;
        using (var firstImage = Image.Load(files[0]))
        {
            cellW = firstImage.Width;
            cellH = firstImage.Height;
        }

        int columns = (int)Math.Ceiling(Math.Sqrt(files.Length));
        int rows = (int)Math.Ceiling((double)files.Length / columns);
        
        int atlasWidth = columns * cellW;
        int atlasHeight = rows * cellH;

        using var atlas = new Image<Rgba32>(atlasWidth, atlasHeight);
        var map = new Dictionary<string, AtlasRegion>();

        string categoryLower = category.ToLower();

        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            string entryName = $"{categoryLower}_{fileName}";
            
            int col = i % columns;
            int row = i / columns;

            using var icon = Image.Load<Rgba32>(filePath);
            int x = col * cellW;
            int y = row * cellH;

            atlas.Mutate(ctx => ctx.DrawImage(icon, new Point(x, y), 1f));
            map[entryName] = new AtlasRegion { X = x, Y = y, W = icon.Width, H = icon.Height };
        }

        string pngName = $"{categoryLower}_atlas.png";
        string jsonName = $"{categoryLower}_map.json";
        
        string pngPath = Path.Combine(outputDir, pngName);
        string jsonPath = Path.Combine(outputDir, jsonName);

        atlas.Save(pngPath);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"  Packed: {pngName}");
    }

    public class AtlasRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace LastLight.Tools.Commands;

public static class PackAssetsCommand
{
    public static void Execute(string[] args)
    {
        string rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        string rawPath = Path.Combine(rootPath, "LastLight.Client.Core/Assets/Graphics");
        string outputPath = Path.Combine(rootPath, "LastLight.Client.Core/Content/Graphics");

        if (!Directory.Exists(rawPath))
        {
            Console.WriteLine($"Error: Raw graphics path not found: {rawPath}");
            return;
        }

        Directory.CreateDirectory(outputPath);

        var subDirectories = Directory.GetDirectories(rawPath);
        foreach (var dir in subDirectories)
        {
            string category = Path.GetFileName(dir); // Keep original capitalization (e.g., "Icon", "Login")
            PackCategory(dir, category, outputPath);
        }
    }

    private static void PackCategory(string inputDir, string category, string outputDir)
    {
        Console.WriteLine($"Packing category: {category}...");
        var files = Directory.GetFiles(inputDir, "*.png");
        if (files.Length == 0) return;

        // Load the first image to detect size
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

        // Standardized naming: lowercase filenames inside capitalized folders
        string pngName = $"{categoryLower}_atlas.png";
        string jsonName = $"{categoryLower}_map.json";
        
        string categoryDir = Path.Combine(outputDir, category);
        Directory.CreateDirectory(categoryDir);

        string pngPath = Path.Combine(categoryDir, pngName);
        string jsonPath = Path.Combine(categoryDir, jsonName);

        atlas.Save(pngPath);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Saved {pngPath} and {jsonPath}");
    }

    public class AtlasRegion
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }
}

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
        string outputPath = Path.Combine(rootPath, "LastLight.Client.Core/Content/Graphics/Icons");

        if (!Directory.Exists(rawPath))
        {
            Console.WriteLine($"Error: Raw graphics path not found: {rawPath}");
            return;
        }

        Directory.CreateDirectory(outputPath);

        var subDirectories = Directory.GetDirectories(rawPath);
        foreach (var dir in subDirectories)
        {
            string category = Path.GetFileName(dir);
            PackCategory(dir, category, outputPath);
        }
    }

    private static void PackCategory(string inputDir, string category, string outputDir)
    {
        Console.WriteLine($"Packing category: {category}...");
        var files = Directory.GetFiles(inputDir, "*.png");
        if (files.Length == 0) return;

        // Simple grid packing
        int iconSize = 16;
        int columns = (int)Math.Ceiling(Math.Sqrt(files.Length));
        int rows = (int)Math.Ceiling((double)files.Length / columns);
        
        int atlasWidth = columns * iconSize;
        int atlasHeight = rows * iconSize;

        using var atlas = new Image<Rgba32>(atlasWidth, atlasHeight);
        var map = new Dictionary<string, AtlasRegion>();

        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            string name = Path.GetFileNameWithoutExtension(filePath);
            int col = i % columns;
            int row = i / columns;

            using var icon = Image.Load<Rgba32>(filePath);
            if (icon.Width != iconSize || icon.Height != iconSize)
            {
                icon.Mutate(x => x.Resize(iconSize, iconSize));
            }

            int x = col * iconSize;
            int y = row * iconSize;

            atlas.Mutate(ctx => ctx.DrawImage(icon, new Point(x, y), 1f));
            map[name] = new AtlasRegion { X = x, Y = y, W = iconSize, H = iconSize };
        }

        string pngName = category.ToLower() + "_atlas.png";
        string jsonName = category.ToLower() + "_map.json";
        
        string pngPath = Path.Combine(outputDir, pngName);
        string jsonPath = Path.Combine(outputDir, jsonName);

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

using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace LastLight.Tools.Commands;

public static class ResizeCommand
{
    public static void Execute(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: resize <path_to_image> <target_width>");
            return;
        }

        string filePath = args[0];
        if (!int.TryParse(args[1], out int targetWidth))
        {
            Console.WriteLine("Error: Target width must be an integer.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found: {filePath}");
            return;
        }

        try
        {
            using var image = Image.Load(filePath);
            
            // Calculate height to maintain aspect ratio
            double ratio = (double)targetWidth / image.Width;
            int targetHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(targetWidth, targetHeight));
            image.Save(filePath); // Overwrite original

            Console.WriteLine($"Resized {Path.GetFileName(filePath)} to {targetWidth}x{targetHeight}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resizing image: {ex.Message}");
        }
    }
}

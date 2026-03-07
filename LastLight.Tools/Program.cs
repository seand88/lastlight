using System;
using LastLight.Tools.Commands;

namespace LastLight.Tools;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return;
        }

        string command = args[0].ToLower();
        string[] commandArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

        switch (command)
        {
            case "pack":
            case "pack-assets":
                PackAssetsCommand.Execute(commandArgs);
                break;
            case "resize":
                ResizeCommand.Execute(commandArgs);
                break;
            default:
                Console.WriteLine($"Unknown command: {command}");
                PrintHelp();
                break;
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("LastLight Tools CLI");
        Console.WriteLine("Usage: dotnet run -- <command> [args]");
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  pack-assets    Pack raw graphics into texture atlases");
        Console.WriteLine("  resize         Resize an image (args: <path> <width>)");
    }
}

using System.Diagnostics;
using LastLight.Server;

var server = new ServerNetworking(5000);
server.Start();

Console.WriteLine("[Server] Press Enter to stop.");

var stopwatch = new Stopwatch();
stopwatch.Start();

while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter)
{
    float dt = (float)stopwatch.Elapsed.TotalSeconds;
    stopwatch.Restart();

    server.PollEvents();
    server.Update(dt);
    
    // Prevent 100% CPU usage, but keep loop tight
    Thread.Sleep(5); 
}

server.Stop();
Console.WriteLine("[Server] Stopped.");

using LastLight.Server;

var server = new ServerNetworking(5000);
server.Start();

Console.WriteLine("[Server] Press Enter to stop.");

while (!Console.KeyAvailable || Console.ReadKey(true).Key != ConsoleKey.Enter)
{
    server.PollEvents();
    server.Update(0.015f);
    Thread.Sleep(15); // ~60 ticks/second
}

server.Stop();
Console.WriteLine("[Server] Stopped.");

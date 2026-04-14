using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class FileWatcherHandler(WebSocket ws, string rootPath)
{
    public async Task HandleAsync()
    {
        using var watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) => SendEvent("created", e.FullPath);
        watcher.Deleted += (_, e) => SendEvent("deleted", e.FullPath);
        watcher.Renamed += (_, e) => SendEvent("renamed", e.FullPath, e.OldFullPath);
        watcher.Changed += (_, e) => SendEvent("changed", e.FullPath);

        // Keep alive until client disconnects
        var buffer = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                break;
            }
        }
    }

    private async void SendEvent(string type, string fullPath, string? oldPath = null)
    {
        if (ws.State != WebSocketState.Open) return;

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                type,
                path    = Path.GetRelativePath(rootPath, fullPath),
                oldPath = oldPath != null ? Path.GetRelativePath(rootPath, oldPath) : null,
                name    = Path.GetFileName(fullPath),
                isDir   = Directory.Exists(fullPath)
            });

            var bytes = Encoding.UTF8.GetBytes(payload);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (WebSocketException)
        {
            // Client disconnected mid-send, ignore
        }
    }
}
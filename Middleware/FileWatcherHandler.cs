using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

public class FileWatcherHandler(WebSocket ws, string rootPath)
{
    private readonly Channel<string> _queue = Channel.CreateUnbounded<string>();

    public async Task HandleAsync()
    {
        using var watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) => Enqueue("created", e.FullPath);
        watcher.Deleted += (_, e) => Enqueue("deleted", e.FullPath);
        watcher.Renamed += (_, e) => Enqueue("renamed", e.FullPath, e.OldFullPath);
        watcher.Changed += (_, e) => Enqueue("changed", e.FullPath);

        // Send queued events serially so SendAsync is never called concurrently
        var sender = Task.Run(async () =>
        {
            await foreach (var payload in _queue.Reader.ReadAllAsync())
            {
                if (ws.State != WebSocketState.Open) break;
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(payload);
                    await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (WebSocketException) { break; }
            }
        });

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

        _queue.Writer.Complete();
        await sender;
    }

    private void Enqueue(string type, string fullPath, string? oldPath = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            type,
            path    = Path.GetRelativePath(rootPath, fullPath),
            oldPath = oldPath != null ? Path.GetRelativePath(rootPath, oldPath) : null,
            name    = Path.GetFileName(fullPath),
            isDir   = Directory.Exists(fullPath)
        });

        _queue.Writer.TryWrite(payload);
    }
}

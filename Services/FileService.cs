public class FileService(IConfiguration config)
{
    private readonly string _root = config["RootPath"]!;

    // Always call this before touching the file system
    public string SafePath(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_root, relativePath.TrimStart('/')));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal blocked.");
        return full;
    }

    public object List(string path)
    {
        var full = SafePath(path);
        return Directory.EnumerateFileSystemEntries(full)
            .Where(e => !Path.GetFileName(e).StartsWith('.'))
            .Select(e => new
        {
            name = Path.GetFileName(e),
            isDirectory = Directory.Exists(e),
            size = File.Exists(e) ? new FileInfo(e).Length : (long?)null,
            modified = File.GetLastWriteTime(e)
        });
    }

    public string Read(string path) => File.ReadAllText(SafePath(path));

    public void Write(string path, string content) => File.WriteAllText(SafePath(path), content);

    public void Delete(string path)
    {
        var full = SafePath(path);
        if (Directory.Exists(full)) Directory.Delete(full, recursive: true);
        else File.Delete(full);
    }

    public void Rename(string path, string newPath) =>
        File.Move(SafePath(path), SafePath(newPath));

    public void Mkdir(string path) => Directory.CreateDirectory(SafePath(path));
}
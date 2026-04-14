public record WriteRequest(string Path, string Content);
public record LoginRequest(string Username, string Password);
public record RenameRequest(string Path, string NewPath);
public record MkdirRequest(string Path);
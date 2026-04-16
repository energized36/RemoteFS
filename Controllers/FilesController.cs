using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FilesController(FileService files) : ControllerBase
{
    [HttpGet("list")]
    public IActionResult List([FromQuery] string path = "") =>
        Ok(files.List(path));

    [HttpGet("read")]
    public IActionResult Read([FromQuery] string path) =>
        Ok(files.Read(path));

    [HttpGet("download")]
    public IActionResult Download([FromQuery] string path)
    {
        var full = files.SafePath(path);
        return PhysicalFile(full, "application/octet-stream", Path.GetFileName(full));
    }

    [HttpGet("stream")]
    public IActionResult Stream([FromQuery] string path)
    {
        var full = files.SafePath(path);
        var contentType = Path.GetExtension(path).ToLower() switch
        {
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".ogg"  => "video/ogg",
            ".mov"  => "video/quicktime",
            _       => "application/octet-stream"
        };
        return PhysicalFile(full, contentType, enableRangeProcessing: true);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromQuery] string path = "", IFormFile? file = null)
    {
        if (file == null) return BadRequest("No file provided.");
        var full = files.SafePath($"{path}/{file.FileName}");
        using var stream = System.IO.File.Create(full);
        await file.CopyToAsync(stream);
        return Ok();
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string path)
    {
        files.Delete(path);
        return Ok();
    }

    [HttpPatch("rename")]
    public IActionResult Rename([FromBody] RenameRequest req)
    {
        files.Rename(req.Path, req.NewPath);
        return Ok();
    }

    [HttpPost("mkdir")]
    public IActionResult Mkdir([FromBody] MkdirRequest req)
    {
        files.Mkdir(req.Path);
        return Ok();
    }

    [HttpPut("write")]
    public IActionResult Write([FromBody] WriteRequest req)
    {
        files.Write(req.Path, req.Content);
        return Ok();
    }
}
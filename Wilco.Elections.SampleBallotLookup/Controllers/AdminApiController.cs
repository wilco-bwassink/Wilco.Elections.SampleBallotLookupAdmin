using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin")]
public class AdminApiController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public AdminApiController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("delete-file")]
    public IActionResult DeleteFile([FromForm] string electionName, [FromForm] string fileName)
    {
        Console.WriteLine("MiniAPI delete endpoint hit.");
        Console.WriteLine($"ElectionName: {electionName}");
        Console.WriteLine($"FileName: {fileName}");

        if (string.IsNullOrWhiteSpace(electionName) || string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Missing data.");

        var basePath = Path.Combine(_env.WebRootPath, "uploads", electionName);
        var allSubdirs = new[] { "voterlist", "voteridmap", "sampleballots" };

        foreach (var sub in allSubdirs)
        {
            var path = Path.Combine(basePath, sub, fileName);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return Ok(new { success = true });
            }
        }

        return NotFound();
    }
}

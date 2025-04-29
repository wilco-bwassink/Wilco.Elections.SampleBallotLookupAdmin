using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin")]
public class AdminApiController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AdminApiController(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    [HttpPost("delete-file")]
    public IActionResult DeleteFile([FromForm] string electionName, [FromForm] string fileName)
    {
        if (string.IsNullOrWhiteSpace(electionName) || string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { success = false, error = "Missing parameters." });

        var baseUploadPath = _config["ElectionUploadSettings:BasePath"]
                             ?? Path.Combine(_env.WebRootPath, "uploads");

        var electionPath = Path.Combine(baseUploadPath, electionName);
        var subdirs = new[] { "voterlist", "voteridmap", "ballotstylelinks", "sampleballots" };

        foreach (var sub in subdirs)
        {
            var filePath = Path.Combine(electionPath, sub, fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return Ok(new { success = true, deleted = filePath });
            }
        }

        return NotFound(new { success = false, error = "File not found." });
    }

    [HttpPost("delete-all-sampleballots")]
    public IActionResult DeleteAllSampleBallots([FromForm] string electionName)
    {
        Console.WriteLine($"Deleting all sample ballots for election: {electionName}");

        if (string.IsNullOrWhiteSpace(electionName))
            return BadRequest("Missing election name.");

        var baseUploadPath = _config["ElectionUploadSettings:BasePath"]
                             ?? Path.Combine(_env.WebRootPath, "uploads");

        var path = Path.Combine(baseUploadPath, electionName, "sampleballots");
        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path))
            {
                System.IO.File.Delete(file);
            }

            return Ok(new { success = true });
        }

        return NotFound(new { success = false, error = "Directory not found." });
    }
}

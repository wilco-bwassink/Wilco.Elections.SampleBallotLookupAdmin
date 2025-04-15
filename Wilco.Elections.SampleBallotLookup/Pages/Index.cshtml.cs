using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace Wilco.Elections.SampleBallotLookup.Pages;

public class IndexModel : PageModel
{
    private readonly IWebHostEnvironment _env;

    public IndexModel(IWebHostEnvironment env)
    {
        _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public string? SelectedElection { get; set; }

    [BindProperty]
    public string? ElectionName { get; set; }

    [BindProperty]
    public IFormFile? VoterListFile { get; set; }

    [BindProperty]
    public IFormFile? VoterIdMapFile { get; set; }

    [BindProperty]
    public IFormFile? BallotStyleLinksFile { get; set; }

    [BindProperty]
    public List<IFormFile>? UploadedSampleBallotFiles { get; set; }

    public string? UploadMessage { get; set; }

    public List<string> VoterListFiles { get; set; } = new();
    public List<string> VoterIdMapFiles { get; set; } = new();
    public List<string> BallotStyleLinksFiles { get; set; } = new();
    public List<string> SampleBallotFiles { get; set; } = new();
    public List<string> ExistingElections { get; set; } = new();

    public void OnGet()
    {
        LoadExistingElections();
        if (!string.IsNullOrWhiteSpace(SelectedElection))
        {
            ElectionName = SelectedElection;
            LoadElectionFiles();
        }
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        LoadExistingElections();

        if (string.IsNullOrWhiteSpace(ElectionName))
        {
            UploadMessage = "Election name is required.";
            return Page();
        }

        var basePath = Path.Combine(_env.WebRootPath, "uploads");
        var electionFolder = Path.Combine(basePath, ElectionName);
        Directory.CreateDirectory(electionFolder);

        if (VoterListFile != null)
        {
            var voterListPath = Path.Combine(electionFolder, "voterlist");
            Directory.CreateDirectory(voterListPath);
            var filePath = Path.Combine(voterListPath, VoterListFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await VoterListFile.CopyToAsync(stream);
        }

        if (VoterIdMapFile != null)
        {
            var mapPath = Path.Combine(electionFolder, "voteridmap");
            Directory.CreateDirectory(mapPath);
            var filePath = Path.Combine(mapPath, VoterIdMapFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await VoterIdMapFile.CopyToAsync(stream);
        }

        if (UploadedSampleBallotFiles != null)
        {
            var ballotsPath = Path.Combine(electionFolder, "sampleballots");
            Directory.CreateDirectory(ballotsPath);
            foreach (var file in UploadedSampleBallotFiles)
            {
                var filePath = Path.Combine(ballotsPath, file.FileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
        }

        if (BallotStyleLinksFile != null)
{
    var path = Path.Combine(electionFolder, "ballotstylelinks");
    Directory.CreateDirectory(path);
    var filePath = Path.Combine(path, BallotStyleLinksFile.FileName);
    using var stream = new FileStream(filePath, FileMode.Create);
    await BallotStyleLinksFile.CopyToAsync(stream);
}


        UploadMessage = "Update successful!";
        SelectedElection = ElectionName;
        LoadElectionFiles();

        return Page();
    }

    public IActionResult OnPostDeleteElection()
    {
        if (string.IsNullOrWhiteSpace(ElectionName))
        {
            UploadMessage = "Invalid election deletion request.";
            return Page();
        }

        var path = Path.Combine(_env.WebRootPath, "uploads", ElectionName);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            UploadMessage = $"Election '{ElectionName}' deleted.";
        }

        ElectionName = null;
        SelectedElection = null;
        LoadExistingElections();
        return Page();
    }

    [IgnoreAntiforgeryToken]
	public IActionResult OnPostDeleteFileAjax(string electionName, string fileName)
    {
        Console.WriteLine("AJAX delete handler hit");
        Console.WriteLine($"ElectionName (bound): {electionName ?? "null"}");
        Console.WriteLine($"FileName (bound): {fileName ?? "null"}");

        if (string.IsNullOrWhiteSpace(electionName) || string.IsNullOrWhiteSpace(fileName))
        {
            Console.WriteLine("Returning 400 due to missing data.");
            return BadRequest("Missing data.");
        }

        var basePath = Path.Combine(_env.WebRootPath, "uploads", electionName);
        var allSubdirs = new[] { "voterlist", "voteridmap", "sampleballots" };

        foreach (var sub in allSubdirs)
        {
            var path = Path.Combine(basePath, sub, fileName);
            if (System.IO.File.Exists(path))
            {
                Console.WriteLine($"Deleting file: {path}");
                System.IO.File.Delete(path);
                return new JsonResult(new { success = true });
            }
        }

        Console.WriteLine("File not found — returning 404.");
        return NotFound();
    }

    private void LoadExistingElections()
    {
        var basePath = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(basePath)) return;

        ExistingElections = Directory.GetDirectories(basePath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private void LoadElectionFiles()
    {
        if (string.IsNullOrWhiteSpace(ElectionName)) return;

        var electionPath = Path.Combine(_env.WebRootPath, "uploads", ElectionName);
        VoterListFiles = ListFilesIn(Path.Combine(electionPath, "voterlist"));
        VoterIdMapFiles = ListFilesIn(Path.Combine(electionPath, "voteridmap"));
        SampleBallotFiles = ListFilesIn(Path.Combine(electionPath, "sampleballots"));
        BallotStyleLinksFiles = ListFilesIn(Path.Combine(electionPath, "ballotstylelinks"));

    }

    private List<string> ListFilesIn(string folder)
    {
        if (!Directory.Exists(folder)) return new List<string>();
        return Directory.GetFiles(folder)
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList();
    }
}
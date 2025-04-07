using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
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
    public List<IFormFile>? UploadedSampleBallotFiles { get; set; }

    public string? UploadMessage { get; set; }

    public List<string> VoterListFiles { get; set; } = new();
    public List<string> VoterIdMapFiles { get; set; } = new();
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

    public async Task<IActionResult> OnPostAsync()
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

        // Save Voter List
        if (VoterListFile != null)
        {
            var voterListPath = Path.Combine(electionFolder, "voterlist");
            Directory.CreateDirectory(voterListPath);
            var filePath = Path.Combine(voterListPath, VoterListFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await VoterListFile.CopyToAsync(stream);
        }

        // Save Mapping File
        if (VoterIdMapFile != null)
        {
            var mapPath = Path.Combine(electionFolder, "voteridmap");
            Directory.CreateDirectory(mapPath);
            var filePath = Path.Combine(mapPath, VoterIdMapFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await VoterIdMapFile.CopyToAsync(stream);
        }

        // Save Sample Ballots
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

        UploadMessage = "Upload successful!";
        SelectedElection = ElectionName;
        LoadElectionFiles();

        return Page();
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

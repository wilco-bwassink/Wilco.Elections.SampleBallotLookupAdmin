using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;

namespace Wilco.Elections.SampleBallotLookup.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    // public IndexModel(IConfiguration config, IWebHostEnvironment env)
    // {
    //     _config = config;
    //     _env = env;
    // }

    private readonly ILogger<IndexModel> _logger;

public IndexModel(IConfiguration config, IWebHostEnvironment env, ILogger<IndexModel> logger)
{
    _config = config;
    _env = env;
    _logger = logger;
}


    private readonly string connectionString = "Server=WILCOSQL1;Database=Public_Web;User ID=elections;Password='j$hegiqOjiwl1adisU0E';Encrypt=true;TrustServerCertificate=true;";
    private readonly string BaseUploadPath = "\\\\wilcosql2\\imports\\Elections\\uploads";
    private string StatusFilePath => Path.Combine(_env.WebRootPath, "data", "electionStatus.json");

    [BindProperty(SupportsGet = true)] public string? SelectedElection { get; set; }
    [BindProperty] public string? ElectionName { get; set; }
    [BindProperty] public IFormFile? VoterListFile { get; set; }
    [BindProperty] public IFormFile? VoterIdMapFile { get; set; }
    [BindProperty] public IFormFile? BallotStyleLinksFile { get; set; }
    [BindProperty] public List<IFormFile>? UploadedSampleBallotFiles { get; set; }
    [BindProperty] public bool IsActive { get; set; }

    public string? UploadMessage { get; set; }
    public List<string> VoterListFiles { get; set; } = new();
    public List<string> VoterIdMapFiles { get; set; } = new();
    public List<string> SampleBallotFiles { get; set; } = new();
    public List<string> BallotStyleLinksFiles { get; set; } = new();
    public List<string> ExistingElections { get; set; } = new();

    public class ElectionStatusEntry { public bool IsActive { get; set; } }
    public class ElectionStatusMap : Dictionary<string, ElectionStatusEntry> { }

    private ElectionStatusMap LoadElectionStatus()
    {
        if (!System.IO.File.Exists(StatusFilePath)) return new();
        var json = System.IO.File.ReadAllText(StatusFilePath);
        return System.Text.Json.JsonSerializer.Deserialize<ElectionStatusMap>(json) ?? new();
    }

    private void SaveElectionStatus(ElectionStatusMap map)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(map, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(StatusFilePath, json);
    }

    public void OnGet()
    {
        LoadExistingElections();
        if (!string.IsNullOrWhiteSpace(SelectedElection))
        {
            ElectionName = SelectedElection;
            LoadElectionFiles();

            var statusMap = LoadElectionStatus();
            IsActive = statusMap.TryGetValue(ElectionName!, out var entry) && entry.IsActive;
        }
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        _logger.LogInformation("[DEBUG] POST Upload triggered. IsActive: {IsActive}", IsActive);
        LoadExistingElections();

        if (Request.Form.TryGetValue("IsActive", out var isActiveVal))
        {
            IsActive = isActiveVal.Contains("true", StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation("[DEBUG] Manually parsed IsActive: {IsActive}", IsActive);
        }
        // Manually parse IsActive because Razor posts both false (hidden) and true (checkbox),
        // and model binding only takes the first value.


        if (string.IsNullOrWhiteSpace(ElectionName))
        {
            UploadMessage = "Election name is required.";
            return Page();
        }

        var electionFolder = Path.Combine(BaseUploadPath, ElectionName);
        Directory.CreateDirectory(electionFolder);

        if (VoterListFile != null)
        {
            var voterListPath = Path.Combine(electionFolder, "voterlist");
            Directory.CreateDirectory(voterListPath);
            var filePath = Path.Combine(voterListPath, VoterListFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await VoterListFile.CopyToAsync(stream);
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Elections.ImportVoterList";
                command.Parameters.AddWithValue("@VoterListFile", filePath);
                command.Parameters.AddWithValue("@ElectionId", ElectionName);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                UploadMessage = $"Voter List uploaded, but SQL import failed: {ex.Message}";
                return Page();
            }
        }

        if (VoterIdMapFile != null)
        {
            var mapPath = Path.Combine(electionFolder, "voteridmap");
            Directory.CreateDirectory(mapPath);
            var filePath = Path.Combine(mapPath, VoterIdMapFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await VoterIdMapFile.CopyToAsync(stream);
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Elections.ImportBallotStyles";
                command.Parameters.AddWithValue("@BallotStyleFile", filePath);
                command.Parameters.AddWithValue("@ElectionId", ElectionName);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                UploadMessage = $"Ballot Styles uploaded, but SQL import failed: {ex.Message}";
                return Page();
            }
        }

        if (BallotStyleLinksFile != null)
        {
            var linksPath = Path.Combine(electionFolder, "ballotstylelinks");
            Directory.CreateDirectory(linksPath);
            var filePath = Path.Combine(linksPath, BallotStyleLinksFile.FileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await BallotStyleLinksFile.CopyToAsync(stream);
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Elections.ImportBallotStyles";
                command.Parameters.AddWithValue("@BallotStyleFile", filePath);
                command.Parameters.AddWithValue("@ElectionId", ElectionName);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                UploadMessage = $"Ballot Style Links uploaded, but SQL import failed: {ex.Message}";
                return Page();
            }
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

        // Save IsActive to status map
        // var statusMap = LoadElectionStatus();
        // statusMap[ElectionName] = new ElectionStatusEntry { IsActive = IsActive };
        // SaveElectionStatus(statusMap);

        //Windsurf addition
        var statusMap = LoadElectionStatus();
            if (statusMap.TryGetValue(ElectionName!, out var entry))
            {
                entry.IsActive = IsActive;
            }
            else
            {
               statusMap[ElectionName!] = new ElectionStatusEntry { IsActive = IsActive };
            }
    SaveElectionStatus(statusMap);
        // end Windsurf addition

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

        var path = Path.Combine(BaseUploadPath, ElectionName);
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
    public IActionResult OnPost()
    {
        var electionName = Request.Form["electionName"].ToString();
        var fileName = Request.Form["fileName"].ToString();

       if (string.IsNullOrWhiteSpace(electionName) || string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Missing data.");

        var basePath = Path.Combine(BaseUploadPath, electionName);
     var allSubdirs = new[] { "voterlist", "voteridmap", "ballotstylelinks", "sampleballots" };

        foreach (var sub in allSubdirs)
        {
            var path = Path.Combine(basePath, sub, fileName);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                return new JsonResult(new { success = true });
            }
        }

    return NotFound();
}


    private void LoadExistingElections()
    {
        if (!Directory.Exists(BaseUploadPath)) return;

        ExistingElections = Directory.GetDirectories(BaseUploadPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();
    }

    private void LoadElectionFiles()
    {
        if (string.IsNullOrWhiteSpace(ElectionName)) return;

        var electionPath = Path.Combine(BaseUploadPath, ElectionName);
        VoterListFiles = ListFilesIn(Path.Combine(electionPath, "voterlist"));
        VoterIdMapFiles = ListFilesIn(Path.Combine(electionPath, "voteridmap"));
        BallotStyleLinksFiles = ListFilesIn(Path.Combine(electionPath, "ballotstylelinks"));
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

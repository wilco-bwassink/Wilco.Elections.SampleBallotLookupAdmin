using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace Wilco.Elections.SampleBallotLookup.Pages;

public class NewElectionModel : PageModel
{
    private readonly IConfiguration _config;
    private readonly string _connectionString;
    private readonly string _baseUploadPath;
    private readonly string _statusFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public NewElectionModel(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _connectionString = _config.GetConnectionString("ElectionsDb") ?? string.Empty;
        _baseUploadPath = _config["ElectionUploadSettings:BasePath"] ?? @"\\wilcosql1\elections";
        _statusFilePath = Path.Combine(env.ContentRootPath, "wwwroot", "data", "electionStatus.json");
    }

    [BindProperty(SupportsGet = true)] public string? SelectedElectionId { get; set; }
    [BindProperty] public string? ElectionId { get; set; }
    [BindProperty] public string? ElectionName { get; set; }
    [BindProperty] public string? ElectionNameSpanish { get; set; }
    [BindProperty] public IFormFile? VoterListFile { get; set; }
    [BindProperty] public IFormFile? VoterIdMapFile { get; set; }
    [BindProperty] public IFormFile? BallotStyleLinksFile { get; set; }
    [BindProperty] public List<IFormFile>? UploadedSampleBallotFiles { get; set; }
    [BindProperty] public bool IsActive { get; set; }
    [BindProperty] public bool IsPrimary { get; set; }
    [BindProperty] public string? Announcement { get; set; }
    [BindProperty] public string? AnnouncementSpanish { get; set; }

    public string? UploadMessage { get; set; }
    public List<string> VoterListFiles { get; set; } = new();
    public List<string> VoterIdMapFiles { get; set; } = new();
    public List<string> SampleBallotFiles { get; set; } = new();
    public List<string> BallotStyleLinksFiles { get; set; } = new();
    public List<ElectionListItem> ExistingElections { get; set; } = new();

    public class ElectionListItem
    {
        public string ElectionId { get; set; } = string.Empty;
        public string ElectionName { get; set; } = string.Empty;
    }

    public class ElectionRecord
    {
        public string ElectionId { get; set; } = string.Empty;
        public string ElectionName { get; set; } = string.Empty;
        public string? ElectionNameSpanish { get; set; }
        public bool IsActive { get; set; }
        public bool IsPrimary { get; set; }
        public string? Announcement { get; set; }
        public string? AnnouncementSpanish { get; set; }
    }

    private sealed class LegacyElectionStatusEntry
    {
        public bool IsActive { get; set; }
        public bool IsPrimary { get; set; }
        public string? Announcement { get; set; }
        public string? AnnouncementSpanish { get; set; }
        public string? ElectionNameSpanish { get; set; }
    }

    public void OnGet()
    {
        LoadExistingElections();

        if (string.IsNullOrWhiteSpace(SelectedElectionId))
        {
            return;
        }

        var election = LoadElectionRecords()
            .FirstOrDefault(record => record.ElectionId.Equals(SelectedElectionId, StringComparison.OrdinalIgnoreCase));

        if (election == null)
        {
            UploadMessage = "The selected election could not be found.";
            return;
        }

        PopulateForm(election);
        LoadElectionFiles(election.ElectionId);
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        LoadExistingElections();
        ParseToggleValues();

        var records = LoadElectionRecords();
        ElectionRecord? selectedRecord = ResolveSelectedRecord(records);

        string effectiveElectionId;
        try
        {
            effectiveElectionId = ResolveElectionId(selectedRecord);
        }
        catch (InvalidOperationException)
        {
            UploadMessage = "Election ID is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(ElectionName))
        {
            UploadMessage = "Election name is required.";
            return Page();
        }

        if (selectedRecord != null &&
            !effectiveElectionId.Equals(selectedRecord.ElectionId, StringComparison.OrdinalIgnoreCase))
        {
            UploadMessage = "Election ID cannot be changed for an existing election.";
            return Page();
        }

        if (selectedRecord == null &&
            records.Any(record => record.ElectionId.Equals(effectiveElectionId, StringComparison.OrdinalIgnoreCase)))
        {
            UploadMessage = "That election ID already exists.";
            return Page();
        }

        ElectionId = effectiveElectionId;

        var electionFolder = Path.Combine(_baseUploadPath, effectiveElectionId);
        Directory.CreateDirectory(electionFolder);

        if (VoterListFile != null)
        {
            var voterListPath = Path.Combine(electionFolder, "voterlist");
            Directory.CreateDirectory(voterListPath);
            var filePath = Path.Combine(voterListPath, VoterListFile.FileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await VoterListFile.CopyToAsync(stream);
            }

            var dbPath = filePath.Replace(_baseUploadPath + Path.DirectorySeparatorChar, string.Empty);
            var importError = ImportElectionFile(
                "Elections.ImportVoterList",
                "@VoterListFile",
                dbPath,
                effectiveElectionId);

            if (importError != null)
            {
                UploadMessage = $"Voter List uploaded, but SQL import failed: {importError}";
                return Page();
            }
        }

        if (VoterIdMapFile != null)
        {
            var mapPath = Path.Combine(electionFolder, "voteridmap");
            Directory.CreateDirectory(mapPath);
            var filePath = Path.Combine(mapPath, VoterIdMapFile.FileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await VoterIdMapFile.CopyToAsync(stream);
            }

            var dbPath = filePath.Replace(_baseUploadPath + Path.DirectorySeparatorChar, string.Empty);
            var importError = ImportElectionFile(
                "Elections.ImportBallotStyles",
                "@BallotStyleFile",
                dbPath,
                effectiveElectionId);

            if (importError != null)
            {
                UploadMessage = $"Ballot Styles uploaded, but SQL import failed: {importError}";
                return Page();
            }
        }

        if (BallotStyleLinksFile != null)
        {
            var linksPath = Path.Combine(electionFolder, "ballotstylelinks");
            Directory.CreateDirectory(linksPath);
            var filePath = Path.Combine(linksPath, BallotStyleLinksFile.FileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await BallotStyleLinksFile.CopyToAsync(stream);
            }

            var dbPath = filePath.Replace(_baseUploadPath + Path.DirectorySeparatorChar, string.Empty);
            var importError = ImportElectionFile(
                "Elections.ImportBallotStyleLinks",
                "@BallotStyleLinksFile",
                dbPath,
                effectiveElectionId);

            if (importError != null)
            {
                UploadMessage = $"Ballot Style Links uploaded, but SQL import failed: {importError}";
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
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
        }

        var updatedRecord = selectedRecord ?? new ElectionRecord();
        updatedRecord.ElectionId = effectiveElectionId;
        updatedRecord.ElectionName = ElectionName.Trim();
        updatedRecord.ElectionNameSpanish = ElectionNameSpanish?.Trim();
        updatedRecord.IsActive = IsActive;
        updatedRecord.IsPrimary = IsPrimary;
        updatedRecord.Announcement = Announcement?.Trim();
        updatedRecord.AnnouncementSpanish = AnnouncementSpanish?.Trim();

        SaveElectionRecord(records, updatedRecord);

        UploadMessage = "Update successful!";
        SelectedElectionId = effectiveElectionId;
        LoadExistingElections();
        LoadElectionFiles(effectiveElectionId);
        return Page();
    }

    public IActionResult OnPostDeleteElection()
    {
        LoadExistingElections();

        var electionIdToDelete = SelectedElectionId ?? ElectionId;
        if (string.IsNullOrWhiteSpace(electionIdToDelete))
        {
            UploadMessage = "Invalid election deletion request.";
            return Page();
        }

        var path = Path.Combine(_baseUploadPath, electionIdToDelete);
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        var records = LoadElectionRecords();
        var recordToDelete = records.FirstOrDefault(record =>
            record.ElectionId.Equals(electionIdToDelete, StringComparison.OrdinalIgnoreCase));

        if (recordToDelete != null)
        {
            records.Remove(recordToDelete);
            SaveElectionRecords(records);
            UploadMessage = $"Election '{recordToDelete.ElectionName}' deleted.";
        }
        else
        {
            UploadMessage = $"Election '{electionIdToDelete}' deleted.";
        }

        ElectionId = null;
        ElectionName = null;
        ElectionNameSpanish = null;
        SelectedElectionId = null;
        Announcement = null;
        AnnouncementSpanish = null;
        IsActive = false;
        IsPrimary = false;
        VoterListFiles.Clear();
        VoterIdMapFiles.Clear();
        BallotStyleLinksFiles.Clear();
        SampleBallotFiles.Clear();
        LoadExistingElections();
        return Page();
    }

    [IgnoreAntiforgeryToken]
    public IActionResult OnPost()
    {
        var electionId = Request.Form["electionId"].ToString();
        var fileName = Request.Form["fileName"].ToString();

        if (string.IsNullOrWhiteSpace(electionId) || string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest("Missing data.");
        }

        var basePath = Path.Combine(_baseUploadPath, electionId);
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

    private string ResolveElectionId(ElectionRecord? selectedRecord)
    {
        if (selectedRecord != null)
        {
            return selectedRecord.ElectionId;
        }

        var candidateId = string.IsNullOrWhiteSpace(ElectionId) ? ElectionName : ElectionId;
        var normalizedId = NormalizeElectionId(candidateId);
        if (string.IsNullOrWhiteSpace(normalizedId))
        {
            throw new InvalidOperationException("Election ID could not be generated.");
        }

        return normalizedId;
    }

    private ElectionRecord? ResolveSelectedRecord(List<ElectionRecord> records)
    {
        if (string.IsNullOrWhiteSpace(SelectedElectionId))
        {
            return null;
        }

        return records.FirstOrDefault(record =>
            record.ElectionId.Equals(SelectedElectionId, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeElectionId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var previousWasHyphen = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasHyphen = false;
            }
            else if (!previousWasHyphen)
            {
                builder.Append('-');
                previousWasHyphen = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private string? ImportElectionFile(string procedureName, string fileParameterName, string filePath, string electionId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            command.Parameters.AddWithValue(fileParameterName, filePath);
            command.Parameters.AddWithValue("@ElectionId", electionId);
            command.ExecuteNonQuery();
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void ParseToggleValues()
    {
        if (Request.Form.TryGetValue("IsActive", out var isActiveVal))
        {
            IsActive = isActiveVal.Contains("true", StringComparer.OrdinalIgnoreCase);
        }

        if (Request.Form.TryGetValue("IsPrimary", out var isPrimaryVal))
        {
            IsPrimary = isPrimaryVal.Contains("true", StringComparer.OrdinalIgnoreCase);
        }
    }

    private List<ElectionRecord> LoadElectionRecords()
    {
        var records = new List<ElectionRecord>();

        if (System.IO.File.Exists(_statusFilePath))
        {
            var json = System.IO.File.ReadAllText(_statusFilePath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                records = DeserializeElectionRecords(json);
            }
        }

        var recordIds = new HashSet<string>(records.Select(record => record.ElectionId), StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(_baseUploadPath))
        {
            foreach (var folder in Directory.GetDirectories(_baseUploadPath).Select(Path.GetFileName).Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                if (folder == null || recordIds.Contains(folder))
                {
                    continue;
                }

                records.Add(new ElectionRecord
                {
                    ElectionId = folder,
                    ElectionName = folder
                });
            }
        }

        return records
            .OrderBy(record => record.ElectionName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.ElectionId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<ElectionRecord> DeserializeElectionRecords(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind switch
            {
                JsonValueKind.Array => JsonSerializer.Deserialize<List<ElectionRecord>>(json) ?? new List<ElectionRecord>(),
                JsonValueKind.Object => DeserializeLegacyElectionRecords(json),
                _ => new List<ElectionRecord>()
            };
        }
        catch (JsonException)
        {
            return new List<ElectionRecord>();
        }
    }

    private static List<ElectionRecord> DeserializeLegacyElectionRecords(string json)
    {
        var legacyRecords = JsonSerializer.Deserialize<Dictionary<string, LegacyElectionStatusEntry>>(json)
            ?? new Dictionary<string, LegacyElectionStatusEntry>(StringComparer.OrdinalIgnoreCase);

        return legacyRecords.Select(entry => new ElectionRecord
        {
            ElectionId = entry.Key,
            ElectionName = entry.Key,
            ElectionNameSpanish = entry.Value.ElectionNameSpanish,
            IsActive = entry.Value.IsActive,
            IsPrimary = entry.Value.IsPrimary,
            Announcement = entry.Value.Announcement,
            AnnouncementSpanish = entry.Value.AnnouncementSpanish
        }).ToList();
    }

    private void SaveElectionRecord(List<ElectionRecord> records, ElectionRecord updatedRecord)
    {
        var existingRecord = records.FirstOrDefault(record =>
            record.ElectionId.Equals(updatedRecord.ElectionId, StringComparison.OrdinalIgnoreCase));

        if (existingRecord == null)
        {
            records.Add(updatedRecord);
        }
        else
        {
            existingRecord.ElectionName = updatedRecord.ElectionName;
            existingRecord.ElectionNameSpanish = updatedRecord.ElectionNameSpanish;
            existingRecord.IsActive = updatedRecord.IsActive;
            existingRecord.IsPrimary = updatedRecord.IsPrimary;
            existingRecord.Announcement = updatedRecord.Announcement;
            existingRecord.AnnouncementSpanish = updatedRecord.AnnouncementSpanish;
        }

        SaveElectionRecords(records);
    }

    private void SaveElectionRecords(List<ElectionRecord> records)
    {
        var normalizedRecords = records
            .OrderBy(record => record.ElectionName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(record => record.ElectionId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var json = JsonSerializer.Serialize(normalizedRecords, _jsonOptions);
        System.IO.File.WriteAllText(_statusFilePath, json);
    }

    private void LoadExistingElections()
    {
        ExistingElections = LoadElectionRecords()
            .Select(record => new ElectionListItem
            {
                ElectionId = record.ElectionId,
                ElectionName = record.ElectionName
            })
            .ToList();
    }

    private void PopulateForm(ElectionRecord election)
    {
        SelectedElectionId = election.ElectionId;
        ElectionId = election.ElectionId;
        ElectionName = election.ElectionName;
        ElectionNameSpanish = election.ElectionNameSpanish;
        IsActive = election.IsActive;
        IsPrimary = election.IsPrimary;
        Announcement = election.Announcement;
        AnnouncementSpanish = election.AnnouncementSpanish;
    }

    private void LoadElectionFiles(string electionId)
    {
        var electionPath = Path.Combine(_baseUploadPath, electionId);
        VoterListFiles = ListFilesIn(Path.Combine(electionPath, "voterlist"));
        VoterIdMapFiles = ListFilesIn(Path.Combine(electionPath, "voteridmap"));
        BallotStyleLinksFiles = ListFilesIn(Path.Combine(electionPath, "ballotstylelinks"));
        SampleBallotFiles = ListFilesIn(Path.Combine(electionPath, "sampleballots"));
    }

    private static List<string> ListFilesIn(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return new List<string>();
        }

        return Directory.GetFiles(folder)
            .Select(Path.GetFileName)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Text.Json;


namespace Wilco.Elections.SampleBallotLookup.Pages;

public class SampleBallotModel : PageModel
{
    private readonly IWebHostEnvironment _env;
    private readonly string _jsonPath;
    public SampleBallotModel(IWebHostEnvironment env)
    {
        _env = env;
        _jsonPath = Path.Combine(_env.ContentRootPath, "wwwroot", "data", "sampleBallotSettings.json");
    }

    [BindProperty]
    public SampleBallotSettings Settings { get; set; } = new();

    [BindProperty]
    public bool ShowSampleBallots
    {
        get => Settings.ShowSampleBallot;
        set => Settings.ShowSampleBallot = value;
    }

    public void OnGet()
    {
        if (System.IO.File.Exists(_jsonPath))
        {
            var json = System.IO.File.ReadAllText(_jsonPath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                Settings = JsonSerializer.Deserialize<SampleBallotSettings>(json) ?? new();
            }
        }
    }

    public IActionResult OnPost()
    {
        var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_jsonPath, json);
        return RedirectToPage();
    }

    public class SampleBallotSettings
    {
        public string EnglishText { get; set; } = "";
        public string SpanishText { get; set; } = "";
        public bool ShowSampleBallot { get; set; }
    }
}   
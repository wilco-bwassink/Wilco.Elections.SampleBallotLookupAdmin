using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace DeleteFileRazorPageApp.Pages.Api.Admin
{
    [IgnoreAntiforgeryToken]
    public class DeleteFileModel : PageModel
    {
        private readonly string BaseUploadPath;

        public DeleteFileModel(IConfiguration config)
        {
            BaseUploadPath = config["ElectionUploadSettings:BasePath"]
                             ?? @"\\wilcosql2\imports\Elections\uploads";
        }

            public IActionResult OnGet()
                {
                    return Content("GET handler hit");
                }

        public IActionResult OnPost()
        {
            var electionName = Request.Form["electionName"];
            var fileName = Request.Form["fileName"];

            Console.WriteLine($"[DEBUG] POST: {electionName} / {fileName}");

            if (string.IsNullOrWhiteSpace(electionName) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Missing data.");

            var basePath = Path.Combine(BaseUploadPath, electionName);
            var subdirs = new[] { "voterlist", "voteridmap", "ballotstylelinks", "sampleballots" };

            foreach (var sub in subdirs)
            {
                var fullPath = Path.Combine(basePath, sub, fileName);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return new JsonResult(new { success = true });
                }
            }

            return NotFound();
        }
    }
}

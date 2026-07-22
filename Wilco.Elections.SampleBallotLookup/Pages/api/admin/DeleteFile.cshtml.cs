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
            var electionId = Request.Form["electionId"];
            var fileName = Request.Form["fileName"];

            Console.WriteLine($"[DEBUG] POST: {electionId} / {fileName}");

            if (string.IsNullOrWhiteSpace(electionId) || string.IsNullOrWhiteSpace(fileName))
                return BadRequest("Missing data.");

            var basePath = Path.Combine(BaseUploadPath, electionId);
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

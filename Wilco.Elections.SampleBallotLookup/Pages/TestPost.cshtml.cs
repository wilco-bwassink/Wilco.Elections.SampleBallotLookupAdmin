using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Wilco.Elections.SampleBallotLookup.Pages
{
    public class TestPostModel : PageModel
    {
        public IActionResult OnPost()
        {
            Console.WriteLine("✅ Reached OnPost");

            var form = Request.HasFormContentType ? Request.Form : null;
            var electionId = form?["electionId"];
            var fileName = form?["fileName"];

            if (string.IsNullOrWhiteSpace(electionId) || string.IsNullOrWhiteSpace(fileName))
            {
                Console.WriteLine("❌ Missing form data");
                return new JsonResult(new { success = false, error = "Missing form fields." });
            }

            Console.WriteLine($"📨 Data: {electionId}, {fileName}");

            return new JsonResult(new { success = true, received = new { electionId, fileName } });
        }
    }
}

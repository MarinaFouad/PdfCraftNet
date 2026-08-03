using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class MergeModel : PageModel
{
    [BindProperty]
    public List<IFormFile> Files { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Files == null || Files.Count < 2)
        {
            ErrorMessage = "Please select at least two PDF files to merge.";
            return Page();
        }

        try
        {
            var byteArrays = new List<byte[]>();
            foreach (var file in Files)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                byteArrays.Add(ms.ToArray());
            }

            var merged = PdfToolsService.Merge(byteArrays);
            return File(merged, "application/pdf", "merged.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not merge files: {ex.Message}";
            return Page();
        }
    }
}

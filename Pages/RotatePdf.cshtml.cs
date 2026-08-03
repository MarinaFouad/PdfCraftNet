using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class RotatePdfModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public int Degrees { get; set; } = 90;

    [BindProperty]
    public string? PageSpec { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null)
        {
            ErrorMessage = "Please select a PDF file.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var result = PdfToolsService.Rotate(ms.ToArray(), Degrees, PageSpec);
            return base.File(result, "application/pdf", "rotated.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not rotate PDF: {ex.Message}";
            return Page();
        }
    }
}

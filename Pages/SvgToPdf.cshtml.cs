using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class SvgToPdfModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null)
        {
            ErrorMessage = "Please select an SVG file.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var result = ImageConversionService.SvgToPdf(ms.ToArray());
            return base.File(result, "application/pdf", "converted.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not convert SVG: {ex.Message}";
            return Page();
        }
    }
}

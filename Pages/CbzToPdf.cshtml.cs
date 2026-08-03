using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class CbzToPdfModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null)
        {
            ErrorMessage = "Please select a .cbz file.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var result = ImageConversionService.CbzToPdf(ms.ToArray());
            return base.File(result, "application/pdf", "comic.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not convert CBZ: {ex.Message}";
            return Page();
        }
    }
}

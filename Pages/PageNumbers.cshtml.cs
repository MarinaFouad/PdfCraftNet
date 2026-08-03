using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class PageNumbersModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public string Format { get; set; } = "Page {n} of {total}";

    [BindProperty]
    public double FontSize { get; set; } = 10;

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

            var result = PdfToolsService.AddPageNumbers(ms.ToArray(), Format, FontSize);
            return base.File(result, "application/pdf", "numbered.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not add page numbers: {ex.Message}";
            return Page();
        }
    }
}

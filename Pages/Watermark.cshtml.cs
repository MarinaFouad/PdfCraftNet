using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class WatermarkModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public string Text { get; set; } = string.Empty;

    [BindProperty]
    public double FontSize { get; set; } = 48;

    [BindProperty]
    public double Opacity { get; set; } = 0.3;

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || string.IsNullOrWhiteSpace(Text))
        {
            ErrorMessage = "Please select a PDF and enter watermark text.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var result = PdfToolsService.AddWatermark(ms.ToArray(), Text, Opacity, FontSize);
            return base.File(result, "application/pdf", "watermarked.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not add watermark: {ex.Message}";
            return Page();
        }
    }
}

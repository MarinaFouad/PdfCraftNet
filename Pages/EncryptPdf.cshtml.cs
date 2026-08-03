using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class EncryptPdfModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public string UserPassword { get; set; } = string.Empty;

    [BindProperty]
    public string? OwnerPassword { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || string.IsNullOrWhiteSpace(UserPassword))
        {
            ErrorMessage = "Please select a PDF and enter a password.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var result = PdfToolsService.Encrypt(ms.ToArray(), UserPassword, OwnerPassword);
            return base.File(result, "application/pdf", "encrypted.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not encrypt PDF: {ex.Message}";
            return Page();
        }
    }
}

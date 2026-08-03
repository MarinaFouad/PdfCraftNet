using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class DeletePagesModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public string PageSpec { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || string.IsNullOrWhiteSpace(PageSpec))
        {
            ErrorMessage = "Please select a PDF and specify which pages to delete.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var result = PdfToolsService.DeletePages(ms.ToArray(), PageSpec);
            return base.File(result, "application/pdf", "pages_deleted.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not delete pages: {ex.Message}";
            return Page();
        }
    }
}

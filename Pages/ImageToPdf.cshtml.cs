using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class ImageToPdfModel : PageModel
{
    [BindProperty]
    public List<IFormFile> Files { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Files == null || Files.Count == 0)
        {
            ErrorMessage = "Please select at least one image file.";
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

            var result = ImageConversionService.ImagesToPdf(byteArrays);
            return File(result, "application/pdf", "images.pdf");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not convert images: {ex.Message}";
            return Page();
        }
    }
}

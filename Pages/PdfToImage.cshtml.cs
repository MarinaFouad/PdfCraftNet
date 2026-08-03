using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class PdfToImageModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public string Format { get; set; } = "png";

    [BindProperty]
    public int Dpi { get; set; } = 150;

    [BindProperty]
    public bool Greyscale { get; set; }

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

            var images = ImageConversionService.PdfToImages(ms.ToArray(), Format, Greyscale, Dpi);
            string ext = Format.ToLowerInvariant();

            if (images.Count == 1)
            {
                return base.File(images[0], $"image/{ext}", $"page1.{ext}");
            }

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                for (int i = 0; i < images.Count; i++)
                {
                    var entry = archive.CreateEntry($"page{i + 1}.{ext}");
                    using var entryStream = entry.Open();
                    entryStream.Write(images[i], 0, images[i].Length);
                }
            }

            return base.File(zipStream.ToArray(), "application/zip", "pdf_pages.zip");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not convert PDF to images: {ex.Message}";
            return Page();
        }
    }
}

using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PdfCraftNet.Services;

namespace PdfCraftNet.Pages;

public class SplitModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    [BindProperty]
    public int PagesPerChunk { get; set; } = 1;

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null)
        {
            ErrorMessage = "Please select a PDF file.";
            return Page();
        }

        if (PagesPerChunk < 1)
        {
            ErrorMessage = "Pages per file must be at least 1.";
            return Page();
        }

        try
        {
            using var ms = new MemoryStream();
            await File.CopyToAsync(ms);

            var chunks = PdfToolsService.Split(ms.ToArray(), PagesPerChunk);

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                for (int i = 0; i < chunks.Count; i++)
                {
                    var entry = archive.CreateEntry($"part_{i + 1}.pdf");
                    using var entryStream = entry.Open();
                    entryStream.Write(chunks[i], 0, chunks[i].Length);
                }
            }

            return base.File(zipStream.ToArray(), "application/zip", "split_output.zip");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not split file: {ex.Message}";
            return Page();
        }
    }
}

using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.Security;

namespace PdfCraftNet.Services;

/// <summary>
/// Static helpers implementing each PDF tool. Every method takes raw PDF byte[] input(s)
/// and returns raw PDF byte[] output, so the Razor Pages layer only has to deal with
/// file upload/download plumbing.
/// </summary>
public static class PdfToolsService
{
    // ---------- Merge ----------
    public static byte[] Merge(IEnumerable<byte[]> files)
    {
        using var outputDocument = new PdfDocument();

        foreach (var fileBytes in files)
        {
            using var ms = new MemoryStream(fileBytes);
            using var inputDocument = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
            for (int i = 0; i < inputDocument.PageCount; i++)
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
            }
        }

        return Save(outputDocument);
    }

    // ---------- Split (returns one PDF per page-range group; caller zips them) ----------
    public static List<byte[]> Split(byte[] file, int pagesPerChunk)
    {
        using var ms = new MemoryStream(file);
        using var inputDocument = PdfReader.Open(ms, PdfDocumentOpenMode.Import);

        var results = new List<byte[]>();
        int total = inputDocument.PageCount;

        for (int start = 0; start < total; start += pagesPerChunk)
        {
            using var chunkDoc = new PdfDocument();
            int end = Math.Min(start + pagesPerChunk, total);
            for (int i = start; i < end; i++)
            {
                chunkDoc.AddPage(inputDocument.Pages[i]);
            }
            results.Add(Save(chunkDoc));
        }

        return results;
    }

    // ---------- Extract specific pages (1-based page numbers, e.g. "1,3,5-7") ----------
    public static byte[] ExtractPages(byte[] file, string pageSpec)
    {
        var pageNumbers = ParsePageSpec(pageSpec);

        using var ms = new MemoryStream(file);
        using var inputDocument = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        using var outputDocument = new PdfDocument();

        foreach (var pageNum in pageNumbers)
        {
            if (pageNum < 1 || pageNum > inputDocument.PageCount)
                throw new ArgumentException($"Page {pageNum} is out of range (document has {inputDocument.PageCount} pages).");

            outputDocument.AddPage(inputDocument.Pages[pageNum - 1]);
        }

        return Save(outputDocument);
    }

    // ---------- Delete specific pages (1-based) ----------
    public static byte[] DeletePages(byte[] file, string pageSpec)
    {
        var toDelete = new HashSet<int>(ParsePageSpec(pageSpec));

        using var ms = new MemoryStream(file);
        using var inputDocument = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        using var outputDocument = new PdfDocument();

        for (int i = 0; i < inputDocument.PageCount; i++)
        {
            int pageNum = i + 1;
            if (!toDelete.Contains(pageNum))
            {
                outputDocument.AddPage(inputDocument.Pages[i]);
            }
        }

        if (outputDocument.PageCount == 0)
            throw new ArgumentException("That would delete every page — nothing left to save.");

        return Save(outputDocument);
    }

    // ---------- Rotate all pages (or a subset) by 90/180/270 ----------
    public static byte[] Rotate(byte[] file, int degrees, string? pageSpec)
    {
        using var ms = new MemoryStream(file);
        using var document = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);

        var targetPages = string.IsNullOrWhiteSpace(pageSpec)
            ? Enumerable.Range(1, document.PageCount).ToHashSet()
            : ParsePageSpec(pageSpec).ToHashSet();

        for (int i = 0; i < document.PageCount; i++)
        {
            if (targetPages.Contains(i + 1))
            {
                var page = document.Pages[i];
                page.Rotate = (page.Rotate + degrees) % 360;
            }
        }

        return Save(document);
    }

    // ---------- Add a diagonal text watermark to every page ----------
    public static byte[] AddWatermark(byte[] file, string text, double opacity, double fontSize)
    {
        using var ms = new MemoryStream(file);
        using var document = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);

        var font = new XFont("Arial", fontSize);
        var brush = new XSolidBrush(XColor.FromArgb((int)(opacity * 255), 128, 128, 128));

        foreach (var page in document.Pages)
        {
            using var gfx = XGraphics.FromPdfPage(page);
            gfx.TranslateTransform(page.Width / 2, page.Height / 2);
            gfx.RotateTransform(-45);

            var size = gfx.MeasureString(text, font);
            gfx.DrawString(text, font, brush,
                new XPoint(-size.Width / 2, size.Height / 2));
        }

        return Save(document);
    }

    // ---------- Add page numbers (bottom-center) ----------
    public static byte[] AddPageNumbers(byte[] file, string format, double fontSize)
    {
        // format supports {n} = page number, {total} = total pages
        using var ms = new MemoryStream(file);
        using var document = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);

        var font = new XFont("Arial", fontSize);
        var brush = XBrushes.Black;
        int total = document.PageCount;

        for (int i = 0; i < total; i++)
        {
            var page = document.Pages[i];
            using var gfx = XGraphics.FromPdfPage(page);

            string label = format
                .Replace("{n}", (i + 1).ToString())
                .Replace("{total}", total.ToString());

            var size = gfx.MeasureString(label, font);
            gfx.DrawString(label, font, brush,
                new XPoint((page.Width - size.Width) / 2, page.Height - 24));
        }

        return Save(document);
    }

    // ---------- Encrypt with a password ----------
    public static byte[] Encrypt(byte[] file, string userPassword, string? ownerPassword)
    {
        using var ms = new MemoryStream(file);
        using var document = PdfReader.Open(ms, PdfDocumentOpenMode.Modify);

        document.SecuritySettings.UserPassword = userPassword;
        document.SecuritySettings.OwnerPassword = string.IsNullOrWhiteSpace(ownerPassword)
            ? userPassword
            : ownerPassword;
        document.SecuritySettings.PermitPrinting = true;
        document.SecuritySettings.PermitFullQualityPrint = true;

        return Save(document);
    }

    // ---------- helpers ----------

    private static byte[] Save(PdfDocument document)
    {
        using var outStream = new MemoryStream();
        document.Save(outStream, false);
        return outStream.ToArray();
    }

    /// <summary>Parses strings like "1,3,5-7,10" into a sorted list of 1-based page numbers.</summary>
    private static List<int> ParsePageSpec(string spec)
    {
        var result = new List<int>();
        var parts = spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-', StringSplitOptions.TrimEntries);
                if (range.Length == 2 && int.TryParse(range[0], out int from) && int.TryParse(range[1], out int to))
                {
                    for (int i = Math.Min(from, to); i <= Math.Max(from, to); i++)
                        result.Add(i);
                }
                else
                {
                    throw new ArgumentException($"Invalid page range: '{part}'");
                }
            }
            else if (int.TryParse(part, out int single))
            {
                result.Add(single);
            }
            else
            {
                throw new ArgumentException($"Invalid page number: '{part}'");
            }
        }

        if (result.Count == 0)
            throw new ArgumentException("No pages specified.");

        return result;
    }
}

using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using SkiaSharp;
using Svg.Skia;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using System.IO.Compression;

namespace PdfCraftNet.Services;

/// <summary>
/// Handles Image -> PDF and PDF -> Image conversions.
/// Images are normalized through ImageSharp (decode) and re-encoded to PNG before being
/// embedded into a PDF page via PdfSharpCore, so the pipeline works the same regardless
/// of the original raster format (jpg/png/bmp/tiff/webp/gif).
/// </summary>
public static class ImageConversionService
{
    private const double PixelsToPoints = 72.0 / 96.0; // assume 96 DPI source images

    // ---------- Image(s) -> single PDF, one page per image ----------
    public static byte[] ImagesToPdf(IEnumerable<byte[]> imageFiles)
    {
        using var document = new PdfDocument();

        foreach (var imageBytes in imageFiles)
        {
            using var image = Image.Load(imageBytes);
            using var pngStream = new MemoryStream();
            image.Save(pngStream, new PngEncoder());
            pngStream.Position = 0;

            AddImagePage(document, pngStream, image.Width, image.Height);
        }

        return Save(document);
    }

    // ---------- SVG -> single-page PDF (rasterized) ----------
    public static byte[] SvgToPdf(byte[] svgBytes, int width = 1240, int height = 1754)
    {
        using var svg = new SKSvg();
        using var inputStream = new MemoryStream(svgBytes);
        var picture = svg.Load(inputStream);

        if (picture == null)
            throw new ArgumentException("Could not parse this SVG file.");

        // Use the SVG's own intrinsic size when available, otherwise fall back to A4-ish px.
        var bounds = picture.CullRect;
        int w = bounds.Width > 0 ? (int)Math.Ceiling(bounds.Width) : width;
        int h = bounds.Height > 0 ? (int)Math.Ceiling(bounds.Height) : height;

        using var pngStream = new MemoryStream();
        picture.ToImage(pngStream, SKColors.White, SKEncodedImageFormat.Png, 100,
            1f, 1f, SKColorType.Rgba8888, SKAlphaType.Premul, null);
        pngStream.Position = 0;

        using var document = new PdfDocument();
        AddImagePage(document, pngStream, w, h);
        return Save(document);
    }

    // ---------- CBZ (zip of images) -> PDF, pages in filename order ----------
    public static byte[] CbzToPdf(byte[] cbzBytes)
    {
        using var zipStream = new MemoryStream(cbzBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var imageEntries = archive.Entries
            .Where(e => IsImageFile(e.Name))
            .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (imageEntries.Count == 0)
            throw new ArgumentException("No image files found inside this archive.");

        var imageBytesList = new List<byte[]>();
        foreach (var entry in imageEntries)
        {
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            imageBytesList.Add(ms.ToArray());
        }

        return ImagesToPdf(imageBytesList);
    }

    // ---------- PDF -> images, one file per page (format: jpg/png/bmp/tiff/webp) ----------
    public static List<byte[]> PdfToImages(byte[] pdfBytes, string format, bool greyscale, int dpi = 150)
    {
        using var pdfStream = new MemoryStream(pdfBytes);

        var results = new List<byte[]>();
        var renderOptions = new PDFtoImage.RenderOptions(Dpi: dpi, Grayscale: greyscale);

        foreach (var bitmap in PDFtoImage.Conversion.ToImages(pdfStream, options: renderOptions))
        {
            using (bitmap)
            {
                using var skPngData = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                using var image = Image.Load(skPngData.AsStream());

                using var outStream = new MemoryStream();
                SaveInFormat(image, outStream, format);
                results.Add(outStream.ToArray());
            }
        }

        if (results.Count == 0)
            throw new ArgumentException("This PDF has no pages to render.");

        return results;
    }

    // ---------- helpers ----------

    private static void AddImagePage(PdfDocument document, Stream pngStream, int pixelWidth, int pixelHeight)
    {
        var page = document.AddPage();
        page.Width = XUnit.FromPoint(pixelWidth * PixelsToPoints);
        page.Height = XUnit.FromPoint(pixelHeight * PixelsToPoints);

        using var xImage = XImage.FromStream(() => pngStream);
        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);
    }

    private static void SaveInFormat(Image image, Stream outStream, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "jpg":
            case "jpeg":
                image.Save(outStream, new JpegEncoder { Quality = 90 });
                break;
            case "png":
                image.Save(outStream, new PngEncoder());
                break;
            case "bmp":
                image.Save(outStream, new BmpEncoder());
                break;
            case "tiff":
            case "tif":
                image.Save(outStream, new TiffEncoder());
                break;
            case "webp":
                image.Save(outStream, new WebpEncoder());
                break;
            default:
                throw new ArgumentException($"Unsupported output format: '{format}'");
        }
    }

    private static bool IsImageFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".tiff" or ".tif" or ".webp";
    }

    private static byte[] Save(PdfDocument document)
    {
        using var outStream = new MemoryStream();
        document.Save(outStream, false);
        return outStream.ToArray();
    }
}

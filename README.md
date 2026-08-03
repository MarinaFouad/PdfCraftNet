# PdfCraft.NET

A small ASP.NET Core (Razor Pages) port of a subset of PDFCraft's tools. Unlike the
original (which processes files client-side in the browser via WebAssembly), this
version processes PDFs **server-side** using [PdfSharpCore](https://github.com/ststeiger/PdfSharpCore)
(MIT-licensed), so files are uploaded to the server, processed, and streamed back.

## Included tools (12)


https://github.com/user-attachments/assets/1be9c065-fa5d-4ce7-97f0-a40fcfbf1c9d




| Tool | Route | Description |
|------|-------|-------------|
| Merge PDF | `/Merge` | Combine multiple PDFs into one |
| Split PDF | `/Split` | Split into N-page chunks, downloaded as a .zip |
| Extract Pages | `/ExtractPages` | Pull specific pages into a new PDF |
| Delete Pages | `/DeletePages` | Remove specific pages |
| Rotate PDF | `/RotatePdf` | Rotate all or specific pages by 90/180/270° |
| Add Watermark | `/Watermark` | Diagonal text watermark on every page |
| Page Numbers | `/PageNumbers` | Customizable page numbering |
| Encrypt PDF | `/EncryptPdf` | Password-protect a PDF |
| Image to PDF | `/ImageToPdf` | JPG/PNG/BMP/TIFF/WebP/GIF → one PDF page per image |
| SVG to PDF | `/SvgToPdf` | Rasterize a vector SVG to a single-page PDF |
| CBZ to PDF | `/CbzToPdf` | Comic book archive (zip of images) → PDF |
| PDF to Image | `/PdfToImage` | PDF pages → JPG/PNG/WebP/BMP/TIFF, greyscale optional, zipped if multi-page |

**Note on scope**: the original README lists a separate tool per format (JPG to PDF, PNG
to PDF, WebP to PDF, etc.). Here they're consolidated into one generic *Image to PDF* and
one generic *PDF to Image* tool with a format selector — same coverage, less duplicate code.

## Project layout

```
PdfCraftNet/
├── Program.cs                  # App startup, Razor Pages + upload size config
├── PdfCraftNet.csproj           # References PdfSharpCore
├── Services/
│   └── PdfToolsService.cs      # All PDF logic lives here, framework-agnostic
├── Pages/
│   ├── Index.cshtml            # Tool directory / home page
│   ├── Merge.cshtml(.cs)
│   ├── Split.cshtml(.cs)
│   ├── ExtractPages.cshtml(.cs)
│   ├── DeletePages.cshtml(.cs)
│   ├── RotatePdf.cshtml(.cs)
│   ├── Watermark.cshtml(.cs)
│   ├── PageNumbers.cshtml(.cs)
│   ├── EncryptPdf.cshtml(.cs)
│   └── Shared/_Layout.cshtml
└── wwwroot/css/site.css
```

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Run locally

```bash
cd PdfCraftNet
dotnet restore
dotnet run
```

Then open http://localhost:5080.

> **Note:** I wasn't able to run `dotnet restore`/`dotnet build` myself while generating
> this project (no NuGet access in this environment), so please run a build on your end
> to confirm it compiles cleanly. The code follows standard ASP.NET Core 8 / PdfSharpCore
> patterns, but flag anything odd and I'll fix it fast.

### Docker (optional)

A minimal Dockerfile you can add:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PdfCraftNet.dll"]
```

## New dependencies (image conversion)

| Package | Used for | License |
|---------|----------|---------|
| `SixLabors.ImageSharp` | Decoding/encoding jpg/png/bmp/tiff/webp/gif | Six Labors Split (free for most uses; check for commercial) |

> **Pinned to 2.1.9, not the latest 3.x**: PdfSharpCore's compiled DLL internally calls an
> `Image.Load(Stream, out IImageFormat)` overload that ImageSharp removed in its 3.x line.
> If you bump this package to 3.x you'll hit `MissingMethodException: Method not found`
> at runtime the moment an image gets loaded — even though the project builds fine, since
> NuGet's version resolution is looser than what actually works at runtime. Leave this
> pinned unless you've confirmed a newer PdfSharpCore release supports ImageSharp 3.x.
| `Svg.Skia` | Rasterizing SVG files | MIT |
| `PDFtoImage` | Rendering PDF pages to bitmaps (wraps native PDFium via SkiaSharp) | MIT |

> **ImageSharp licensing note**: recent ImageSharp versions use the "Six Labors Split
> License" — free for most open-source/non-commercial and small-business use, but check
> https://sixlabors.com/pricing/ if this is for a commercial product before shipping.

## Tools intentionally left out (see trade-offs discussion)

Office document conversion (Word/Excel/PowerPoint ↔ PDF), OCR, EPUB/MOBI/DjVu, table
extraction, and PDF/A conversion aren't included — they all need either a native external
engine (LibreOffice, Tesseract) or a paid commercial library to do well. Happy to build
any of these next once you've picked an approach.

## Design notes / how it differs from the original PDFCraft

- **Server-side, not client-side**: files are uploaded and processed on the server
  and streamed back as a download — there's no WebAssembly/browser-only processing.
  If "never leaves the device" privacy is important to you, this trade-off is worth
  knowing about; you'd want to self-host it on infrastructure you trust.
- **Scope**: this covers 8 of PDFCraft's 90+ tools — the most commonly used
  organize/edit operations. The `PdfToolsService` class is written so it's easy to
  bolt on more tools (e.g. compress, header/footer, metadata editing) using the same
  pattern: byte[] in, byte[] out.
- **Library**: uses PdfSharpCore instead of pdf-lib/PyMuPDF, since it's a mature,
  MIT-licensed .NET library well-suited to page-level manipulation (merge, split,
  rotate, watermark, encrypt). Some of the more advanced original tools (OCR,
  format conversion, table extraction) would need additional libraries.


To add a new tool:
1. Add a method to `Services/PdfToolsService.cs` (byte[] in → byte[] out).
2. Add a `Pages/YourTool.cshtml` + `.cshtml.cs` pair, following the pattern of
   any existing tool (e.g. `RotatePdf`).
3. Add a link to it in `Pages/Shared/_Layout.cshtml` and `Pages/Index.cshtml`.

watch the demo 
<h2>🎥 Demo</h2>

<video src="https://github.com/MarinaFouad/PdfCraftNet/blob/main/PDF.mp4" controls width="800"></video>

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using UglyToad.PdfPig;

class Program
{
    static void Main()
    {
        string pdfPath = Path.GetFullPath("1.pdf");   // Input PDF file
        string outputFolder = "ExtractedImages";      // Output folder for images

        // Ensure output folder exists
        Directory.CreateDirectory(outputFolder);

        ExtractImagesFromPdf(pdfPath, outputFolder);
    }

    static void ExtractImagesFromPdf(string pdfPath, string outputFolder)
    {
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"Error: File not found - {pdfPath}");
            return;
        }

        using (var document = PdfDocument.Open(pdfPath))
        {
            int imageCount = 0;

            foreach (var page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    if (image.RawBytes == null || image.RawBytes.Length == 0)
                    {
                        Console.WriteLine($"Skipping empty image on page {page.Number}");
                        continue;
                    }

                    imageCount++;
                    string imageFileName = Path.Combine(outputFolder, $"page_{page.Number}_image_{imageCount}.png");

                    try
                    {
                        byte[] imageBytes = image.RawBytes.ToArray();

                        // ✅ Check if it's JPEG 2000
                        if (IsJpeg2000(imageBytes))
                        {
                            Console.WriteLine($"Processing JPEG 2000 image on page {page.Number}...");

                            using (var imgStream = new MemoryStream(imageBytes))
                            using (var skBitmap = SKBitmap.Decode(imgStream))
                            {
                                if (skBitmap == null)
                                {
                                    Console.WriteLine($"Failed to decode JPEG 2000 image on page {page.Number}");
                                    continue;
                                }

                                using (var skImage = SKImage.FromBitmap(skBitmap))
                                using (var skData = skImage.Encode(SKEncodedImageFormat.Png, 100))
                                using (var outputFile = File.OpenWrite(imageFileName))
                                {
                                    skData.SaveTo(outputFile);
                                }

                                Console.WriteLine($"Saved JPEG 2000 image as PNG: {imageFileName}");
                            }
                        }
                        else
                        {
                            // ✅ Try to decode with ImageSharp first
                            try
                            {
                                using (var imgStream = new MemoryStream(imageBytes))
                                using (var img = SixLabors.ImageSharp.Image.Load<Rgba32>(imgStream))
                                {
                                    img.Mutate(x => x.AutoOrient()); // Fix rotation
                                    img.Save(imageFileName, new PngEncoder());
                                    Console.WriteLine($"Saved: {imageFileName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"ImageSharp failed to decode the image: {ex.Message}");
                                Console.WriteLine($"Attempting to decode with SkiaSharp...");

                                // ✅ Fallback to SkiaSharp
                                using (var imgStream = new MemoryStream(imageBytes))
                                using (var skBitmap = SKBitmap.Decode(imgStream))
                                {
                                    if (skBitmap == null)
                                    {
                                        Console.WriteLine($"Failed to decode image on page {page.Number}");
                                        continue;
                                    }

                                    using (var skImage = SKImage.FromBitmap(skBitmap))
                                    using (var skData = skImage.Encode(SKEncodedImageFormat.Png, 100))
                                    using (var outputFile = File.OpenWrite(imageFileName))
                                    {
                                        skData.SaveTo(outputFile);
                                    }

                                    Console.WriteLine($"Saved image as PNG using SkiaSharp: {imageFileName}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving image {imageFileName}: {ex.Message}");
                    }
                }
            }
        }
        Console.WriteLine("Image extraction completed!");
    }

    // ✅ Detect JPEG 2000 based on magic number
    static bool IsJpeg2000(byte[] imageBytes)
    {
        return imageBytes.Length > 4 &&
               imageBytes[0] == 0xFF &&
               imageBytes[1] == 0x4F &&
               imageBytes[2] == 0xFF &&
               imageBytes[3] == 0x51;
    }
}
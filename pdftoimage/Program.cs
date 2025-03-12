using PDFtoImage;

public class PdfToImageConverter
{
    public static void Main()
    {
        string pdfFilePath = "multiple.pdf";
        string outputDirectory = "ExtractedImages";

        ConvertPdfToJpeg(pdfFilePath, outputDirectory, dpi: 300);
    }

    public static void ConvertPdfToJpeg(string pdfFilePath, string outputDirectory, int dpi = 300)
    {
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        try
        {
            byte[] pdfContent = File.ReadAllBytes(pdfFilePath);

            int pageCount;
            using (var ms = new MemoryStream(pdfContent))
                pageCount = Conversion.GetPageCount(ms);

            for (int page = 0; page < pageCount; page++)
            {
                string outputPath = Path.Combine(outputDirectory, $"Page_{page + 1}.jpg");
                using (var imageStream = new MemoryStream())
                {
                    using (var pdfStream = new MemoryStream(pdfContent))
                    {
                        var renderOptions = new RenderOptions(Dpi: dpi);
                        Conversion.SaveJpeg(imageStream, pdfStream, page: page, options: renderOptions);
                    }
                    File.WriteAllBytes(outputPath, imageStream.ToArray());
                }
                Console.WriteLine($"Saved: {outputPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
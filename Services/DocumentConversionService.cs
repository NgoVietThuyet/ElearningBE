using Spire.Doc;
using Spire.Presentation;

namespace ElearningAPI.Services
{
    public class DocumentConversionService
    {
        public byte[]? ConvertToPdf(byte[] fileBytes, string fileName)
        {
            if (fileBytes == null || fileBytes.Length == 0) return null;

            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "EduSmartConversion");
                Directory.CreateDirectory(tempDir);
                var inputPath = Path.Combine(tempDir, fileName);
                var outputPath = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(fileName) + ".pdf");

                System.IO.File.WriteAllBytes(inputPath, fileBytes);

                switch (ext)
                {
                    case ".doc":
                    case ".docx":
                        using (var doc = new Document())
                        {
                            doc.LoadFromFile(inputPath);
                            doc.SaveToFile(outputPath, Spire.Doc.FileFormat.PDF);
                        }
                        break;
                    case ".ppt":
                    case ".pptx":
                        using (var ppt = new Presentation())
                        {
                            ppt.LoadFromFile(inputPath);
                            ppt.SaveToFile(outputPath, Spire.Presentation.FileFormat.PDF);
                        }
                        break;
                    default:
                        return null;
                }

                var result = System.IO.File.ReadAllBytes(outputPath);
                try { System.IO.File.Delete(inputPath); System.IO.File.Delete(outputPath); } catch { }
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}

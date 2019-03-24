using System;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.IO;
using System.Text;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers.DbFileWriters;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class PdfHandler : IFileHandler
    {
        public FileDetails FileData;

        public PdfHandler(FileInfo f)
        {
            FileData = new FileDetails(
                f.LastWriteTime,
                f.CreationTime,
                f.Extension,
                System.IO.File.GetAccessControl(f.FullName)
                    .GetOwner(typeof(System.Security.Principal.NTAccount))
                    .ToString(),
                f.Name,
                1,
                f.FullName,
                f.Name,
                f.Length / 1024);

            if (FileData.FileSize == 0)
            {
                FileData.FileSize = 1;
            }
        }

        public void ExtractContent()
        {
            if (String.Compare(FileData.Path, "") != 0)
            {
                PdfReader pdfReader = new PdfReader(FileData.Path);
                FileData.Pages = pdfReader.NumberOfPages;
                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

                    currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8,
                        Encoding.Default.GetBytes(currentText)));
                    FileData.Content.Add(currentText);
                }

                pdfReader.Close();
            }
        }

        public void WriteToDb(IDbFileWriter dbFileWriter)
        {
            dbFileWriter.WriteToDb(FileData);
        }

        public FileDetails GetFileDetails()
        {
            return FileData;
        }
    }
}
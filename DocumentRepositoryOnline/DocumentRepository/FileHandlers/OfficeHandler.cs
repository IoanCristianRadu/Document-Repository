using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;
using System.Data;
using DocumentFormat.OpenXml.Presentation;
using DocumentRepositoryOnline.DocumentRepository.FileHandlers.DbFileWriters;
using Excel;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class OfficeHandler : IFileHandler
    {
        public FileDetails FileData;
        public WordprocessingDocument WordDoc;

        public OfficeHandler(FileInfo f)
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
            switch (FileData.Extension)
            {
                case ".pptx":
                    ExtractPptxContent();
                    break;
                case ".xlsx":
                    ExtractXlsxContent();
                    break;
                case ".docx":
                    ExtractDocxContent();
                    break;
            }
        }

        public void ExtractPptxContent()
        {
            using (PresentationDocument presentationDocument = PresentationDocument.Open(FileData.Path, false))
            {
                int slideIndex = 0;
                if (presentationDocument == null)
                {
                    throw new ArgumentNullException($"presentationDocument");
                }

                PresentationPart presentationPart = presentationDocument.PresentationPart;
                if (presentationPart != null && presentationPart.Presentation != null)
                {
                    Presentation presentation = presentationPart.Presentation;
                    if (presentation.SlideIdList != null)
                    {
                        var slideIds = presentation.SlideIdList.ChildElements;
                        while (slideIndex < slideIds.Count)
                        {
                            string slidePartRelationshipId = (slideIds[slideIndex] as SlideId).RelationshipId;
                            SlidePart slidePart = (SlidePart) presentationPart.GetPartById(slidePartRelationshipId);
                            if (slidePart == null)
                            {
                                throw new ArgumentNullException("slidePart");
                            }

                            if (slidePart.Slide != null)
                            {
                                String paragraphText = "";
                                foreach (var paragraph in slidePart.Slide
                                    .Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                                {
                                    paragraphText = paragraphText + paragraph.InnerText + " | ";
                                }

                                if (paragraphText.Length > 0)
                                {
                                    FileData.Content.Add(paragraphText.ToString());
                                }
                            }

                            slideIndex++;
                        }
                    }
                }
            }
        }

        public void ExtractXlsxContent()
        {
            foreach (var worksheet in Workbook.Worksheets(FileData.Path))
            {
                String worksheetPage = "";
                foreach (var row in worksheet.Rows)
                {
                    String rowText = "";
                    foreach (var cell in row.Cells)
                    {
                        if (cell != null)
                        {
                            rowText = rowText + cell.Text + " | ";
                        }
                    }

                    worksheetPage = worksheetPage + rowText;
                }

                FileData.Content.Add(worksheetPage);
            }
        }

        public void ExtractDocxContent()
        {
            WordDoc = WordprocessingDocument.Open(FileData.Path, false);
            String s = WordDoc.MainDocumentPart.Document.InnerText;
            FileData.Content.Add(s);
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
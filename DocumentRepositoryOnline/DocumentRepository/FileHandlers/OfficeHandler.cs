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
            if (FileData.Extension == ".pptx")
            {
                using (PresentationDocument presentationDocument = PresentationDocument.Open(FileData.Path, false))
                {
                    int slideIndex = 0;

                    // Verify that the presentation document exists.
                    if (presentationDocument == null)
                    {
                        throw new ArgumentNullException($"presentationDocument");
                    }

                    // Get the presentation part of the presentation document.
                    PresentationPart presentationPart = presentationDocument.PresentationPart;

                    // Verify that the presentation part and presentation exist.
                    if (presentationPart != null && presentationPart.Presentation != null)
                    {
                        // Get the Presentation object from the presentation part.
                        Presentation presentation = presentationPart.Presentation;

                        // Verify that the slide ID list exists.
                        if (presentation.SlideIdList != null)
                        {
                            // Get the collection of slide IDs from the slide ID list.
                            var slideIds = presentation.SlideIdList.ChildElements;

                            // If the slide ID is in range...
                            while (slideIndex < slideIds.Count)
                            {
                                // Get the relationship ID of the slide.
                                string slidePartRelationshipId = (slideIds[slideIndex] as SlideId).RelationshipId;

                                // Get the specified slide part from the relationship ID.
                                SlidePart slidePart = (SlidePart) presentationPart.GetPartById(slidePartRelationshipId);
                                // Verify that the slide part exists.
                                if (slidePart == null)
                                {
                                    throw new ArgumentNullException("slidePart");
                                }

                                // If the slide exists...
                                if (slidePart.Slide != null)
                                {
                                    String paragraphText = "";
                                    // Iterate through all the paragraphs in the slide.
                                    foreach (var paragraph in slidePart.Slide
                                        .Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                                    {
                                        paragraphText = paragraphText + paragraph.InnerText + " | ";
                                    }

                                    if (paragraphText.Length > 0)
                                    {
                                        // Add each paragraph to the linked list.
                                        FileData.Content.Add(paragraphText.ToString());
                                    }
                                }

                                slideIndex++;
                            }
                        }
                    }
                }
            }
            else if (FileData.Extension == ".xlsx")
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
            else if (FileData.Extension == ".docx")
            {
                WordDoc = WordprocessingDocument.Open(FileData.Path, false);
                String s = WordDoc.MainDocumentPart.Document.InnerText;
                FileData.Content.Add(s);
            }
        }

        public void WriteToDb(IDbFileWriter dbFileWriter)
        {
            dbFileWriter.WriteToDb(FileData);
        }

        public FileDetails getFileDetails()
        {
            return FileData;
        }
    }
}
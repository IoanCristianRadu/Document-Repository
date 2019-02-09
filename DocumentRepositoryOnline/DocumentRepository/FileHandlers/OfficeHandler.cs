using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
//using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;
using System.Data;
using DocumentFormat.OpenXml.Presentation;
using Excel;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{

    public class OfficeHandler : TextHandler
    {
        public WordprocessingDocument wordDoc;

        public OfficeHandler(FileInfo inputFile)
            : base(inputFile)
        {

        }

        public override void extractContent()
        {
            if (this.extension == ".pptx")
            {
                using (PresentationDocument presentationDocument = PresentationDocument.Open(path, false))
                {
                    int slideIndex = 0;

                    // Verify that the presentation document exists.
                    if (presentationDocument == null)
                    {
                        throw new ArgumentNullException("presentationDocument");
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
                                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slidePartRelationshipId);
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
                                    foreach (var paragraph in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                                    {
                                        paragraphText = paragraphText + paragraph.InnerText + " | ";
                                    }
                                    if (paragraphText.Length > 0)
                                    {
                                        // Add each paragraph to the linked list.
                                        content.Add(paragraphText.ToString());
                                    }
                                }
                                slideIndex++;
                            }
                        }
                    }
                }
            }
            else if (this.extension == ".xlsx")
            {
                foreach (var worksheet in Workbook.Worksheets(path))
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
                    content.Add(worksheetPage);
                }



                /*
                //2. Reading from a OpenXml Excel file (2007 format; *.xlsx)
                FileStream fs = new FileStream(path, FileMode.Open);
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(fs);

                DataSet ds = excelReader.AsDataSet();
                //ds.Tables

                //5. Data Reader methods
                while (excelReader.Read())
                {
                    excelReader.GetInt32(0);
                }

                //6. Free resources (IExcelDataReader is IDisposable)
                excelReader.Close();*/
            }
            else if (this.extension == ".docx")
            {
                wordDoc = WordprocessingDocument.Open(path, false);
                String s = wordDoc.MainDocumentPart.Document.InnerText;
                content.Add(s);
                /*
                    using (WordprocessingDocument doc =
                        WordprocessingDocument.Open(memoryStream, false))
                    {
                        RevisionAccepter.AcceptRevisions(doc);
                        XElement root = doc.MainDocumentPart.GetXDocument().Root;
                        XElement body = root.LogicalChildrenContent().First();
                        foreach (XElement blockLevelContentElement in body.LogicalChildrenContent())
                        {
                            if (blockLevelContentElement.Name == W.p)
                            {
                                var text = blockLevelContentElement
                                    .LogicalChildrenContent()
                                    .Where(e => e.Name == W.r)
                                    .LogicalChildrenContent()
                                    .Where(e => e.Name == W.t)
                                    .Select(t => (string)t)
                                    .StringConcatenate();
                                Console.WriteLine("Paragraph text >{0}<", text);
                                continue;
                            }
                            // If element is not a paragraph, it must be a table.
                            Console.WriteLine("Table");
                        }
                    }
                }*/

                //String o = GetSubstringByString("<w:t>", "</w:t>", s);


                /*
                foreach (OpenXmlElement oneElem in wordDoc.MainDocumentPart.Document.Body.ChildElements)
                {
                    oneElem.in

                }
                Body body = wordDoc.MainDocumentPart.Document.Body.ChildElements[0];

                using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
                {
                    Xml
                        docText = sr.ReadToEnd();

                }
                
                while (body.HasChildren)
                {
                    Paragraph para = body.ChildElements.
                }
                */



            }
        }

    }


}

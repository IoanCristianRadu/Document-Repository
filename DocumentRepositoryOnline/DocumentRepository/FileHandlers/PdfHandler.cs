using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class PdfHandler : TextHandler
    {
        public PdfHandler(FileInfo f) : base(f)
        {
        }

        public override void ExtractContent()
        {
            if (String.Compare(this.Path, "") != 0)
            {
                PdfReader pdfReader = new PdfReader(this.Path);
                this.Pages = pdfReader.NumberOfPages;
                for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(pdfReader, page, strategy);

                    currentText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8,
                        Encoding.Default.GetBytes(currentText)));
                    Content.Add(currentText);
                }

                pdfReader.Close();
            }
        }
    }
}
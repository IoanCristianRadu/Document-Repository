using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentRepositoryOnline.DocumentRepository
{
    public class Record
    {
        public bool IsCorrectPath;

        public string Content { get; set; }

        public int PartNumber { get; set; }

        public string LastModified { get; set; }

        public string DateCreated { get; set; }

        public int FileSize { get; set; }

        public string Extension { get; set; }

        public string Author { get; set; }

        public string Title { get; set; }

        public int Pages { get; set; }

        public string LocalPath { get; set; }

        public int MostRelevantPage { get; set; }

        public string RelevantContent { get; set; }

        public Record(string content, int partNumber, string lastModified, string dateCreated, int fileSize,
            string extension, string author, string title, int pages, string localPath)
        {
            this.Content = content;
            this.PartNumber = partNumber;
            this.LastModified = lastModified;
            this.DateCreated = dateCreated;
            this.FileSize = fileSize;
            this.Extension = extension;
            this.Author = author;
            this.Title = title;
            this.Pages = pages;
            this.LocalPath = localPath;
            this.IsCorrectPath = true;
        }

        public Record(string lastModified, string dateCreated, int fileSize, string extension, string author,
            string title, int pages, string localPath)
        {
            this.LastModified = lastModified;
            this.DateCreated = dateCreated;
            this.FileSize = fileSize;
            this.Extension = extension;
            this.Author = author;
            this.Title = title;
            this.Pages = pages;
            this.LocalPath = localPath;
            this.IsCorrectPath = true;
        }

        public Record(string lastModified, string dateCreated, int fileSize, string extension, string author,
            string title, int pages)
        {
            this.LastModified = lastModified;
            this.DateCreated = dateCreated;
            this.FileSize = fileSize;
            this.Extension = extension;
            this.Author = author;
            this.Title = title;
            this.Pages = pages;
            this.IsCorrectPath = true;
        }
    }
}
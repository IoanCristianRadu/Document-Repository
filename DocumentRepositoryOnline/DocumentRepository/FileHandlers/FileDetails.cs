using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.DocumentRepository.FileHandlers
{
    public class FileDetails
    {
        public List<string> Content = new List<string>();
        public DateTime LastModified;
        public DateTime DateCreated;
        public long FileSize = 0;
        public string Extension;
        public string Author;
        public string Title;
        public int Pages;
        public string Path;
        public string Name;

        public FileDetails(DateTime lastModified, DateTime dateCreated, string extension, string author, string title,
            int pages, string path, string name, long fileSize)
        {
            LastModified = lastModified;
            DateCreated = dateCreated;
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Pages = pages;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FileSize = fileSize;
        }
    }
}
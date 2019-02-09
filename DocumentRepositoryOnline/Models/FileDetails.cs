using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class FileDetails : IComparable
    {
        public int ID { get; set; }
        public String LastModified { get; set; }
        public String DateCreated { get; set; }
        public int FileSize { get; set; }
        public String Extension { get; set; }
        public String Author { get; set; }
        public String Title { get; set; }
        public int Pages { get; set; }
        public int FolderId { get; set; }
        public String AccountEmail { get; set; }
        public String Path { get; set; }

        public int CompareTo(object obj)
        {
            return this.Title.CompareTo(((FileDetails)obj).Title);
        }
    }
}
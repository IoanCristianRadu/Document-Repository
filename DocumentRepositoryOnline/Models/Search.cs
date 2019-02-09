using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class Search
    {
        //Search options
        [DisplayName("Content")]
        public String content { get; set; }

        [DisplayName("Minimum ammount of pages")]
        public int pagesMin { get; set; }

        [DisplayName("Maximum ammount of pages")]
        public int pagesMax { get; set; }

        [DisplayName("Minimum date for last modified")]
        public String lastModifiedMin { get; set; }

        [DisplayName("Maximum date for last modified")]
        public String lastModifiedMax { get; set; }

        [DisplayName("Minimum date of creation")]
        public String creationDateMin { get; set; }

        [DisplayName("Maximum date of creation")]
        public String creationDateMax { get; set; }

        [DisplayName("Minimum file size")]
        public int fileSizeMin { get; set; }

        [DisplayName("Maximum file size")]
        public int fileSizeMax { get; set; }

        [DisplayName("File extension")]
        public String extension { get; set; }

        [DisplayName("File name")]
        public String title { get; set; }

        public List<DocumentRepository.Record> recordList = new List<DocumentRepository.Record>();
    }
}
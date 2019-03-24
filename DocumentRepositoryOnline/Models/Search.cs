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
        [DisplayName("Content")] public String Content { get; set; }

        [DisplayName("Minimum ammount of pages")]
        public int PagesMin { get; set; }

        [DisplayName("Maximum ammount of pages")]
        public int PagesMax { get; set; }

        [DisplayName("Minimum date for last modified")]
        public String LastModifiedMin { get; set; }

        [DisplayName("Maximum date for last modified")]
        public String LastModifiedMax { get; set; }

        [DisplayName("Minimum date of creation")]
        public String CreationDateMin { get; set; }

        [DisplayName("Maximum date of creation")]
        public String CreationDateMax { get; set; }

        [DisplayName("Minimum file size")]
        public int FileSizeMin { get; set; }

        [DisplayName("Maximum file size")]
        public int FileSizeMax { get; set; }

        [DisplayName("File extension")]
        public String Extension { get; set; }

        [DisplayName("File name")]
        public String Title { get; set; }

        public List<DocumentRepository.Record> RecordList = new List<DocumentRepository.Record>();
    }
}
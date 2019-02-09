using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class FileDetailsVector
    {
        List<FileDetails> fileDetailsList = new List<FileDetails>();

        public List<FileDetails> FileDetailsList
        {
            get { return fileDetailsList; }
            set { fileDetailsList = value; }
        }


    }
}
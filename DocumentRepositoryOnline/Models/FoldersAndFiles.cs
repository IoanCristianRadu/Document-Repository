using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class FoldersAndFiles
    {
        public Folders FolderList { get; set; }
        public FileDetailsVector FileList { get; set; }
        public String CreateFolderPath { get; set; }
        public String AddEmail { get; set; }
    }
}
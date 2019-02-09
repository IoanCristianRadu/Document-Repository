using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class FoldersAndFiles
    {
        public Folders folderList { get; set; }
        public FileDetailsVector fileList { get; set; }
        public String createFolderPath { get; set; }
        public String addEmail { get; set; }
    }
}
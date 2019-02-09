using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class Folder : IComparable
    {
        public int ID { get; set; }

        public String LocalPath { get; set; }
        public int ParentId { get; set; }
        public int Fullscan { get; set; }

        public int CompareTo(object obj)
        {
            return this.LocalPath.CompareTo(((Folder)obj).LocalPath);
        }
    }
}
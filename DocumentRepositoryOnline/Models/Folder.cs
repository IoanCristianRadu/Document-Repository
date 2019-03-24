using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class Folder : IComparable
    {
        public int Id { get; set; }
        public String LocalPath { get; set; }
        public int ParentId { get; set; }
        public int FullScan { get; set; }

        public int CompareTo(object obj)
        {
            return String.Compare(this.LocalPath, ((Folder) obj).LocalPath, StringComparison.Ordinal);
        }
    }
}
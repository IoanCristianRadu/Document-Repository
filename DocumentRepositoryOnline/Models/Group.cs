using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class Group
    {
        public int GroupId { get; set; }

        [DisplayName("Group name")]
        public String GroupName { get; set; }
        public String DateCreated { get; set; }
        public String MemberType { get; set; }
        public String AccountEmail { get; set; }
    }
}
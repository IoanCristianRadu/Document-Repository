using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class GroupMember
    {
        public int groupId { get; set; }
        public String accountEmail { get; set; }
        public String memberType { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DocumentRepositoryOnline.Models
{
    public class Account
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [DisplayName("Retype password")]
        public string RetypePassword { get; set; }

        [DisplayName("Date Created")]
        public String DateCreated { get; set; }

        public String Type { get; set; }

        [DisplayName("First Name")]
        public String FirstName { get; set; }

        [DisplayName("Last Name")]
        public String LastName { get; set; }
        public String Location { get; set; }
    }
}
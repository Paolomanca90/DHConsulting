using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DHConsulting.Models
{
    public class GoogleProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Given_Name { get; set; }
        public string Family_Name { get; set; }
        public string Email { get; set; }
        public string MobilePhone { get; set; }
    }
}
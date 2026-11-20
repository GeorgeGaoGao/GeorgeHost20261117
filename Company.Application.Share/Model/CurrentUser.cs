using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Company.Applications.Share.Model
{
    public class CurrentUser
    {
        public string? Name { get;  set; }
        public string? Password { get; set; }

        public int Age { get; set; }
        public DateTime? LoginTime { get; set; }

    }
}

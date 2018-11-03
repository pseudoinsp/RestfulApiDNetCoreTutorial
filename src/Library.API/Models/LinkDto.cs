using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Models
{
    public class LinkDto
    {
        public LinkDto(string href, string rel, string method)
        {
            Href = href;
            Rel = rel;
            Method = method;
        }

        public string Href { get; set; }

        public string Rel { get; set; }

        public string Method { get; set; }
    }
}

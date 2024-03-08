using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GEDCOM
{
    public class LinkPerson
    {
        public string id { get; set; }
        public INDI person { get; set; }

        public LinkPerson(string newID)
        {
            id = newID;
            person = null;
        }

        public override string ToString()
        {
            return (person != null) ? person.Name : id;
        }
    }
}

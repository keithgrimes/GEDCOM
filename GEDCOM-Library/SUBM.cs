using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEDCOM
{
    public class SUBM : EntryList
    {
        public string id { get; }
        public SUBM(string line) : base(line)
        { }
    }
}

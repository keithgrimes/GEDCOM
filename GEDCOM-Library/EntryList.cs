using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GEDCOM
{
    public class EntryList
    {
        public List<BaseEntry> lines = new List<BaseEntry>();

        public EntryList(string L0Line)
        {
            // Create the Base Entry Line for the Level 0 Records
            lines.Add(new BaseEntry(L0Line));
        }
    }
}

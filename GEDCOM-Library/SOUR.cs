using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class SOUR : EntryList
    {
        public string id { get; }

        public SOUR(string line) : base(line)
        {
            // ID is going to be on the first record
            id = this.lines[0].Details;
        }

        private string DebuggerDisplay
        {
            get
            {
                return id;
            }
        }
    }
}

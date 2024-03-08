using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BaseEntry
    {
        public int Level { get; }
        public string Type { get; }
        public string Details { get; set; }
        public string Line { get; }

        public BaseEntry(string LineEntry)
        {
            string[] detail = LineEntry.Split(' ');
            Line = LineEntry;
            Details = "";
            //Parse each of the entries.
            if ((Regex.IsMatch(detail[0], @"^\d+$")))
            {
                Level = Int32.Parse(detail[0]);
                if (Level == 0)
                {
                    if (detail.GetUpperBound(0) > 1)
                    {
                        // This is a base level entry so it looks different
                        Type = detail[2];
                        Details = detail[1];
                    }
                    else
                    {
                        Type = detail[1];
                    }
                }
                else
                {
                    Type = detail[1].ToUpper();
                    if (LineEntry.IndexOf(' ', LineEntry.IndexOf(' ', 0) + 2) >= 0)
                    {
                        Details = LineEntry.Substring(LineEntry.IndexOf(' ', LineEntry.IndexOf(' ', 0) + 2)).Trim();
                    }
                }
            }
        }

        public void appendDetails(string detail)
        {
            Details += detail;
        }

        private string DebuggerDisplay
        {
            get
            {
                return Line;
            }
        }

    }

}

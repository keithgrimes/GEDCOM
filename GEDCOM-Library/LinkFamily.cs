using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class LinkFamily
    {
        public string id { get; set; }
        public FAM family { get; set; }

        public LinkFamily(string newID)
        {
            id = newID;
            family = null;
        }

        public override string ToString()
        {
            return (family != null) ? family.ToString() : id;
        }

        public bool Match(LinkFamily potentialFamily, StringBuilder report, LogLevel loggingLevel)
        {
            bool returnValue = false;
            if (loggingLevel == LogLevel.Trace)
            {
                String currentHusband = "None";
                String currentWife = "None";
                String potentialHusband = "None";
                String potentialWife = "None";
                if (this.family.Husband != null) currentHusband = this.family.Husband.person.Name;
                if (this.family.Wife != null) currentWife = this.family.Wife.person.Name;
                if (potentialFamily.family.Husband != null) potentialHusband = potentialFamily.family.Husband.person.Name;
                if (potentialFamily.family.Wife != null) potentialWife = potentialFamily.family.Wife.person.Name;
                report.AppendFormat("Matching Families (Husb/Wife) Current [{0}/{1}] potential [{2}/{3}]{4}", currentHusband, currentWife, potentialHusband,potentialWife, Environment.NewLine);
            }
            if (this.family != null && potentialFamily.family != null)
            {
                if (this.family.Husband != null
                    && potentialFamily.family.Husband != null
                    && this.family.Wife != null
                    && potentialFamily.family.Wife != null)
                {
                    // There is a husband and wife for both.
                    if (this.family.Husband.person.Match(potentialFamily.family.Husband.person, report)
                        &&
                        this.family.Wife.person.Match(potentialFamily.family.Wife.person, report))
                    {

                        returnValue = true;
                    }
                    else if (this.family.Husband.person.Match(potentialFamily.family.Wife.person, report)
                        &&
                        this.family.Wife.person.Match(potentialFamily.family.Husband.person, report))
                    {
                        report.AppendFormat("WARNING: Matching Families (Husb/Wife) Current [{0}/{1}] potential [{2}/{3}] - Partners are oppositely aligned ie. Husband == Wife or Wife == Husband{4}", this.family.Husband.person.Name, this.family.Wife.person.Name, potentialFamily.family.Husband.person.Name,potentialFamily.family.Wife.person.Name, Environment.NewLine);
                        returnValue = true;
                    }
                }
                else
                {
                    // There is only one member of the family.
                    // FMP Bug. When the sole parent is female they are recorded as Husband within the GEDCOM.
                    // As such match both partners. 
                    if (this.family.Husband != null)
                    {
                        // There is only a husband to match
                        returnValue = (potentialFamily.family.Husband != null) ? 
                            this.family.Husband.person.Match(potentialFamily.family.Husband.person, report) :
                            this.family.Husband.person.Match(potentialFamily.family.Wife.person, report); 
                    }
                    else if (this.family.Wife != null)
                    {
                        // The spouse needs matching
                        returnValue = (potentialFamily.family.Wife != null) ?
                            this.family.Wife.person.Match(potentialFamily.family.Wife.person, report) :
                            this.family.Wife.person.Match(potentialFamily.family.Husband.person, report);
                    }
                }
            }
            return returnValue;
        }

        private string DebuggerDisplay
        {
            get
            {
                return String.Format("{0}", this.ToString());
            }
        }

    }
}

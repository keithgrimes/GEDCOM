using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.Design;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class INDI : EntryList
    {

        public string id { get; }
        public string Name { get; set; }
        public string DOB { get; set; }
        public string originalDOB { get; set; }
        public string DOD { get; set; }
        public string originalDOD { get; set; }
        public LinkFamily FAMC { get; set; }  // Family Group as Child
        public List<LinkFamily> FAMS { get; set; }  // Family Group as Spouse
        public INDI personMatch { get; set; } // the Matched Person
        public bool reportIncluded = false;

        public INDI(string line) : base(line)
        {
            // ID is going to be on the first record
            id = this.lines[0].Details;
            Name = "";
            DOB = "";
            personMatch = null;
            FAMS = new List<LinkFamily>();
        }

        public Boolean Match(INDI potentialPerson, StringBuilder report)
        {
            // TODO: Match to include both Name and dates of birth and death.
            // Neither have already been matched. 
            var cultureInfo = new CultureInfo("en-GB");
            var thisDOB = "";
            var potentialDOB = "" ; 

            try {
                thisDOB = DateTime.Parse(this.DOB).ToString(cultureInfo);
                potentialDOB = DateTime.Parse(potentialPerson.DOB).ToString(cultureInfo);
            } catch (Exception)
            {
                // Failed to conver to date time so use the strings.
                thisDOB = this.DOB;
                potentialDOB = potentialPerson.DOB;
            }
            if (this.Name.Trim().ToUpper() == potentialPerson.Name.Trim().ToUpper() && thisDOB.Trim().ToUpper() == potentialDOB.Trim().ToUpper())
            {
                // Name and Date of Birth Match - this is a match
                // Provide a two way match
                report.AppendFormat("Comparing ({0} - {1}) with ({2} - {3}) -- Matched{4}", this.Name, this.DOB, potentialPerson.Name, potentialPerson.DOB, Environment.NewLine);
                return true;
            }
            else
            {
                report.AppendFormat("Comparing ({0} - {1}) with ({2} - {3}) -- NOT Matched{4}", this.Name, this.DOB, potentialPerson.Name, potentialPerson.DOB, Environment.NewLine);
                return false;
            }
        }

        public void MatchIterative(INDI potentialPerson, Boolean MatchParents, Boolean MatchSpouse, Boolean MatchChildren, StringBuilder report, LogLevel loggingLevel)
        {
            // Only try to match if we have not done already and the person we are matching to is not already matched
            if (personMatch == null && potentialPerson.personMatch == null)
            {
                if (this.Name.ToUpper().Contains("not set")) Debugger.Break();
                if (loggingLevel == LogLevel.Trace) report.AppendFormat("Commencing Match Iterative for {0}({1}) with {2}({3}){4}", this.Name, this.DOB, potentialPerson.Name, potentialPerson.DOB, Environment.NewLine);
                //Not Matched already - Great. Check to see if this record is a match
                if (this.Match(potentialPerson, report)) {
                    if (this.Name.ToUpper() == "not set") Debugger.Break();
                    // This person is a match, to provide the two way link. 
                    this.personMatch = potentialPerson;
                    potentialPerson.personMatch = this;

                    if (MatchParents)
                    {
                        // Now we need to try and match the parents.
                        if (this.FAMC != null && potentialPerson.FAMC != null)  // the link to a family as a child.
                        {
                            // First see if the Father is registered
                            if (this.FAMC.family.Husband != null && potentialPerson.FAMC.family.Husband != null)
                            {
                                if (loggingLevel == LogLevel.Trace) report.AppendFormat("-- Matching Father {0} of {1}{2}", this.FAMC.family.Husband.person.Name, this.Name, Environment.NewLine);

                                // Father exists on both sides, so see if they match
                                this.FAMC.family.Husband.person.MatchIterative(potentialPerson.FAMC.family.Husband.person, true, true, true, report, loggingLevel);
                            }
                            // Now see if the mother is registered
                            if (this.FAMC.family.Wife != null && potentialPerson.FAMC.family.Wife != null)
                            {
                                if (loggingLevel == LogLevel.Trace) report.AppendFormat("-- Matching Mother {0} of {1}{2}", this.FAMC.family.Wife.person.Name, this.Name, Environment.NewLine);
                                // Father exists on both sides, so see if they match
                                this.FAMC.family.Wife.person.MatchIterative(potentialPerson.FAMC.family.Wife.person, true, true, true, report, loggingLevel);
                            }
                        }
                    }

                    if (MatchSpouse || MatchChildren)
                    {
                        // Check first there is a family.
                        if (this.FAMS != null && potentialPerson.FAMS != null)
                        {
                            //if (FAMS.Count > 1) Debugger.Break();
                            foreach (var currentFAMS in FAMS)
                            {
                                // First need to find the matching relationshiop in the potential person.
                                foreach (var potentialFAMS in potentialPerson.FAMS)
                                {
                                    // Now see if the is the correct match of family.
                                    if (currentFAMS.Match(potentialFAMS, report, loggingLevel))
                                    {
                                        // There is a family, so first check the spouse if there is one defined (remember there could be multiple)
                                        if (MatchSpouse)
                                        {
                                            if (currentFAMS.family.Wife != null && potentialFAMS.family.Wife != null)
                                            {
                                                // There is a wife, and it does not match
                                                if (currentFAMS.family.Wife.person != this)
                                                {
                                                    // Compare the Wife
                                                    if (loggingLevel == LogLevel.Trace) report.AppendFormat("{0} - Matching Spouse (Wife - {1}){2}", this.Name, potentialFAMS.family.Wife.person.Name, Environment.NewLine);
                                                    currentFAMS.family.Wife.person.MatchIterative(potentialFAMS.family.Wife.person, true, true, true, report, loggingLevel);
                                                }

                                            }
                                            if (currentFAMS.family.Husband != null && potentialFAMS.family.Husband != null)
                                            {
                                                // There is a husband, and it does not match
                                                if (currentFAMS.family.Husband.person != this)
                                                {
                                                    // Compare the Husband
                                                    if (loggingLevel == LogLevel.Trace) report.AppendFormat("{0} - Matching Spouse (Husband - {1}){2}", this.Name, potentialFAMS.family.Husband.person.Name, Environment.NewLine);
                                                    currentFAMS.family.Husband.person.MatchIterative(potentialFAMS.family.Husband.person, true, true, true, report, loggingLevel);
                                                }
                                            }
                                        }
                                        // Now we have done the parents, We need to match any children. 
                                        if (MatchChildren)
                                        {
                                            // We need to iterate the children, We cannot assume that they are listed in the same order

                                            List<LinkPerson> masterChildren = currentFAMS.family.Children;
                                            List<LinkPerson> compareChildren = potentialFAMS.family.Children;

                                            foreach (var masterChild in masterChildren)
                                            {
                                                foreach (var compareChild in compareChildren)
                                                {
                                                    if (loggingLevel == LogLevel.Trace) report.AppendFormat("-- Matching Child {0} ({1}) of [{2}]{3}", masterChild.person.Name, masterChild.person.id, this.Name, Environment.NewLine);

                                                    // Match the Children but don't need to match parents, as this is where we are coming from.
                                                    masterChild.person.MatchIterative(compareChild.person, true, true, true, report, loggingLevel);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
       
        public void ReportDifferences(bool verbose, ref int MissingCount, StringBuilder personReport)
        {
            // Have we reported on this person.
            if (!reportIncluded)
            {
                // NO So we will now. 
                reportIncluded = true;
                // First see if there is a matched person, If not increment the missing count.
                // if there is a match, then just move on
                if (personMatch == null)
                {
                    if (this.Name.ToUpper() == "NOT SET") Debugger.Break();
                    //personReport.AppendFormat("No Match for : ({2})(DOB: {3}, DOD: {4}) {0}{1}", this.Name, Environment.NewLine, this.id, this.DOB, this.DOD);
                    MissingCount++;
                    if (MissingCount == 1)
                    {
                        //this is the start of someone who does not have a match.
                        reportFamilyDifferences(verbose, ref MissingCount, personReport);
                        personReport.AppendFormat("Missing Person {0} ({3}) - Ancestor/Decendents {1}{2}{2}", this.Name, MissingCount, Environment.NewLine, this.DOB);

                        // Reset as we have now reported for this person.
                        MissingCount = 0;
                    }
                    else //No Match, but we are counting the number
                    {
                        reportFamilyDifferences(verbose, ref MissingCount, personReport);
                    }

                }
                else // There is a match, so we dont need to count this one.
                {
                    // As there was a match, keep going. 
                    reportFamilyDifferences(verbose, ref MissingCount, personReport);
                }
            }
        }

        public void reportFamilyDifferences(bool verbose, ref int MissingCount, StringBuilder familyReport)
        {
            INDI partner = null;

            if (FAMC != null)
            {
                // First Report Differences of the Parents
                if (FAMC.family.Husband != null) FAMC.family.Husband.person.ReportDifferences(verbose, ref MissingCount, familyReport);
                if (FAMC.family.Wife != null) FAMC.family.Wife.person.ReportDifferences(verbose, ref MissingCount, familyReport);
            }
            foreach (var currentFAMS in FAMS)
            {
                // First Validate the spouse

                if (currentFAMS.family.Husband != null && currentFAMS.family.Wife != null)
                {
                    partner = (currentFAMS.family.Husband.person == this) ? currentFAMS.family.Wife.person : currentFAMS.family.Husband.person;
                }
                if (partner != null) partner.ReportDifferences(verbose, ref MissingCount, familyReport);

                // This person is a head of a family, count the children
                foreach (var child in currentFAMS.family.Children)
                {
                    // Report any Children
                    child.person.ReportDifferences(verbose, ref MissingCount,familyReport);
                }
            }
        }

        private int ParseName(int idxLine)
        {
            // Store the current level
            int level = 0;
            String GIVN = "";
            String SURN = "";
            String SECG = "";
            String TYPE = "";
            String FullName = "";
            Boolean bPreferred = false;
            BaseEntry line = null;

            // Get the current line and level
            line = base.lines[idxLine];
            level = line.Level;  // Store the level to maintain
            FullName = line.Details;
            idxLine++;
            line = base.lines[idxLine];
            while (line != null && line.Level > level)
            {
                if (line.Level == level + 1)
                {
                    switch (line.Type) {
                        case "GIVN":
                            GIVN = line.Details ;
                            break;
                        case "SECG":
                            SECG = line.Details.ToUpper().Trim();
                            break;
                        case "SURN":
                            SURN = line.Details;
                            break;
                        case "TYPE": 
                            TYPE = line.Details.ToUpper();
                            break;
                        case "_PRIM":
                            bPreferred = true;
                            break;
                        default: 
                            break;
                    }
                }   
                // Move to the next line
                if (idxLine < base.lines.Count) {
                    idxLine ++;
                    line = base.lines[idxLine];
                }
                else
                {
                    // terminate the loop.
                    line = null;
                }
            }
            // Finished reading the information
            // Store the values found            
            if ((Name == "" || bPreferred) && (TYPE == "" || TYPE == "MAIDEN"))
            {
                // Update if there is no value or if the preferred value has been found
                if (SECG == "")
                {
                    Name = FullName.Replace("/","");
                    Name = Regex.Replace(Name, @"\s+", " "); // Remove any additional whitespace
                }
                else
                {
                    // Second Given Name has been provided, so build the name up
                    Name = String.Concat(GIVN, " ", SECG, " ", SURN.Trim());
                    Name = Name.Replace("/","");
                    Name = Regex.Replace(Name, @"\s+", " "); // Remove any additional whitespace
                }
            }
            return idxLine-1;
        }
        private int ParseBirth(int idxLine)
        {
            // Store the current level
            int level = 0;
            String sDATE = "";
            String PLAC = "";
            Boolean bPreferred = false;
            BaseEntry line = null;

            // Get the current line and level
            line = base.lines[idxLine];
            level = line.Level;
            idxLine++;
            line = base.lines[idxLine];
            while (line != null && line.Level > level)
            {
                if (line.Level == level + 1)
                {
                    switch (line.Type.ToUpper()) {
                        case "DATE":
                            sDATE = line.Details ;
                            break;
                        case "PLAC":
                            PLAC = line.Details;
                            break;
                        case "_PRIM":
                            bPreferred = true;
                            break;
                        default: 
                            break;
                    }
                }
                // Move to the next line
                if (idxLine < base.lines.Count-1) {
                    idxLine ++;
                    line = base.lines[idxLine];
                }
                else
                {
                    // terminate the loop.
                    line = null;
                }
            }
            // Finished reading the information
            // Store the values found            
            if (DOB == "" || bPreferred)
            {
                // Update if there is no value or if the preferred value has been found
                DOB = stdDate(sDATE) ;
                originalDOB = sDATE;
            }

            return idxLine-1;
        }
        private int ParseDeath(int idxLine)
        {
            // Store the current level
            int level = 0;
            String sDATE = "";
            String PLAC = "";
            Boolean bPreferred = false;
            BaseEntry line = null;

            // Get the current line and level
            line = base.lines[idxLine];
            level = line.Level;
            idxLine++;
            line = base.lines[idxLine];
            while (line != null && line.Level > level)
            {
                if (line.Level == level + 1)
                {
                    switch (line.Type.ToUpper()) {
                        case "DATE":
                            sDATE = line.Details ;
                            break;
                        case "PLAC":
                            PLAC = line.Details;
                            break;
                        case "_PRIM":
                            bPreferred = true;
                            break;
                        default: 
                            break;
                    }
                }
                // Move to the next line
                if (idxLine < base.lines.Count-1) {
                    idxLine ++;
                    line = base.lines[idxLine];
                }
                else
                {
                    // terminate the loop.
                    line = null;
                }
            }
            // Finished reading the information
            // Store the values found            
            if (DOD == "" || bPreferred)
            {
                // Update if there is no value or if the preferred value has been found
                DOD = stdDate(sDATE) ;
                originalDOD = sDATE;
            }

            return idxLine-1;
        }
        public String stdMonth(String sMonth)
        {
            switch (sMonth.ToUpper())
            {
                case "JAN":
                case "JANUARY" : 
                    sMonth = "January"; break;
                case "FEB":
                case "FEBRUARY" : 
                    sMonth = "February"; break;
                case "MAR":
                case "MARCH" : 
                    sMonth = "March"; break;
                case "APR":
                case "APRIL" : 
                    sMonth = "April"; break;
                case "MAY":
                    sMonth = "May"; break;
                case "JUN":
                case "JUNE":
                    sMonth = "June"; break;
                case "JUL":
                case "JULY" : 
                    sMonth = "July"; break;
                case "AUG":
                case "AUGUST" : 
                    sMonth = "August"; break;
                case "SEP":
                case "SEPTEMBER" : 
                    sMonth = "September"; break;
                case "OCT":
                case "OCTOBER" : 
                    sMonth = "October"; break;
                case "NOV":
                case "NOVEMBER" : 
                    sMonth = "November"; break;
                case "DEC":
                case "DECEMBER" : 
                    sMonth = "December"; break;
                default: break;
            }

            return sMonth;
        }
        public String stdDate(String dt)
        {
            string[] parts = dt.Split(" ") ;
            string output = "";
            if (parts.Length == 3) // This is a full date
            {
                String sday = parts[0];
                String sMonth = parts[1];
                String sYear = parts[2];

                // Standardise the Month
                sMonth = stdMonth(sMonth);

                if (sday.ToUpper() == "ABT" || sday.ToUpper() == "ABOUT")
                {
                    sday = "";
                } else
                {
                    int day ; 
                    try {
                        day = int.Parse(sday);
                        sday = day.ToString();
                    }
                    catch (Exception)
                    {
                        // Do nothing;
                    }
                }
                output = sday + " " + sMonth + " " + sYear;
                output = output.Trim();

            } else if (parts.Length == 2)
            {
                // Assumed to be Month & Year
                String sMonth = parts[0];
                String sYear = parts[1];
                // Standardise the Month

                sMonth = stdMonth(sMonth);

                if (sMonth.ToUpper() == "ABT" || sMonth.ToUpper() == "ABOUT")
                {
                    sMonth = "";
                }
                output = sMonth + " " + sYear;
                output = output.Trim();
            } else 
            {
                // Only one part so use it
                output = parts[0];
            }
            output = Regex.Replace(output, @"\s+", " "); // Remove any additional whitespace

            return output;
        }
        public void Parse()
        {
            BaseEntry line = null;
            int idxLine;

            // Iterate each line
            // foreach (var line in base.lines)
            for (idxLine = 0; idxLine < base.lines.Count; idxLine++)
            {
                line = base.lines[idxLine];
                switch (line.Type)
                {
                    case "NAME":
                        idxLine = this.ParseName(idxLine); 
                        break;
                    case "BIRT":
                        idxLine = this.ParseBirth(idxLine);
                        break;
                    case "DEAT":
                        idxLine = this.ParseDeath(idxLine);
                        break;
                    case "FAMS":
                        FAMS.Add(new LinkFamily(line.Details));
                        break;
                    case "FAMC":
                        FAMC = new LinkFamily(line.Details);
                        break;
                    default:
                        break;
                }
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return String.Format("{0} {1} ({2})", id,Name, DOB);
            }
        }
    }
}

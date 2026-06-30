using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
        public List<String> Flags { get; set; }
        public INDI personMatch { get; set; } // the Matched Person
        public bool reportIncluded = false;
        public bool isIncludedInTree = false ;
        public bool isIgnoredDecendent = false;
        public bool isBloodLine = false ;
        private string isBloodLineS = null ;

        public INDI(string line) : base(line)
        {
            // ID is going to be on the first record
            id = this.lines[0].Details;
            Name = "";
            DOB = "";
            personMatch = null;
            FAMS = new List<LinkFamily>();
            Flags = new List<String>();
        }

        static String ToStandardDate(String srcDate)
        {
            var cultureInfo = new CultureInfo("en-GB");
            string stdDate;

            stdDate = srcDate;
            // Only validate the date if there is one
            if (srcDate != null)
            {
                if (DateTime.TryParse(srcDate, out DateTime newDate))
                {
                    stdDate = newDate.ToString(cultureInfo);
                }
                else
                {
                    stdDate = srcDate;
                }
            }
            return stdDate;
        }

        public void SetInTree()
        {
            // Only do this if the person is currently not defines as in the tree.
            if (!this.isIncludedInTree)
            {
                //First set the current person as in the tree
                this.isIncludedInTree = true; 
                //Next set the Children & Partner
                foreach(LinkFamily relationship in FAMS)
                {
                    // Children First
                    foreach(LinkPerson child in relationship.family?.Children)
                    {
                        child.person?.SetInTree();
                    }
                    // Now set the Partner for this relationship
                    relationship.family?.Wife?.person.SetInTree();
                    relationship.family?.Husband?.person.SetInTree();
                }
                //Finally the Parents    
                FAMC?.family?.Husband?.person.SetInTree();
                FAMC?.family?.Wife?.person.SetInTree();
            }
        }
        public void SetBloodLine(bool includeParents)
        {
            // Only do this if the person is currently not defines as in the tree.
            if (this.isBloodLineS == null)
            {
                //First set the current person as in the tree
                this.isBloodLineS = "Y";
                this.isBloodLine = true; 
                //Next set the Children & Partner
                foreach(LinkFamily relationship in FAMS)
                {
                    // Children First
                    foreach(LinkPerson child in relationship.family?.Children)
                    {
                        child.person?.SetBloodLine(false);
                    }
                    // Do not match the partner as they are not blood line (and any associated partner of them)
                }
                //Finally the Parents    
                if (includeParents)
                {
                    FAMC?.family?.Husband?.person.SetBloodLine(true);
                    FAMC?.family?.Wife?.person.SetBloodLine(true);                    
                }
            }
        }

        public Boolean Match(INDI potentialPerson, StringBuilder report)
        {
            // TODO: Match to include both Name and dates of birth and death.
            // Neither have already been matched. 
            var cultureInfo = new CultureInfo("en-GB");
            var thisDOB = "";
            var potentialDOB = "";
            
            // Now lets see if they have a match
            try
            {
                /* NEED TO STANDARDIZE THE DATE FORMAT FOR CONVERSION TO REMOVE THE EXCEPTION HANDLING */
                thisDOB = INDI.ToStandardDate(this.DOB);
                potentialDOB = INDI.ToStandardDate(potentialPerson.DOB);
            }
            catch (Exception)
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

        public void MatchIterative(INDI potentialPerson, StringBuilder report, CONFIG appConfig)
        {
            // Only try to match if we have not done already and the person we are matching to is not already matched
            if (personMatch == null && potentialPerson.personMatch == null)
            {
                if (appConfig.loggingLevel == LogLevel.Trace) report.AppendFormat("Commencing Match Iterative for {0}({1}) with {2}({3}){4}", this.Name, this.DOB, potentialPerson.Name, potentialPerson.DOB, Environment.NewLine);

                //Not Matched already - Great. Check to see if this record is a match
                if (this.Match(potentialPerson, report))
                {
                    // This person is a match, to provide the two way link. 
                    this.personMatch = potentialPerson;
                    potentialPerson.personMatch = this;

                    if (appConfig.MatchParents)
                    {
                        // Now we need to try and match the parents.
                        if (this.FAMC != null && potentialPerson.FAMC != null)  // the link to a family as a child.
                        {
                            // First see if the Father is registered
                            if (this.FAMC.family.Husband != null && potentialPerson.FAMC.family.Husband != null)
                            {
                                if (appConfig.loggingLevel == LogLevel.Trace) report.AppendFormat("-- Matching Father {0} of {1}{2}", this.FAMC.family.Husband.person.Name, this.Name, Environment.NewLine);

                                // Father exists on both sides, so see if they match
                                this.FAMC.family.Husband.person.MatchIterative(potentialPerson.FAMC.family.Husband.person, report, appConfig);
                            }
                            // Now see if the mother is registered
                            if (this.FAMC.family.Wife != null && potentialPerson.FAMC.family.Wife != null)
                            {
                                if (appConfig.loggingLevel == LogLevel.Trace) report.AppendFormat("-- Matching Mother {0} of {1}{2}", this.FAMC.family.Wife.person.Name, this.Name, Environment.NewLine);
                                // Father exists on both sides, so see if they match
                                this.FAMC.family.Wife.person.MatchIterative(potentialPerson.FAMC.family.Wife.person, report, appConfig);
                            }
                        }
                    }
                    if (appConfig.MatchSpouse || appConfig.MatchChildren)
                    {
                        // Check first there is a family.
                        if (this.FAMS != null && potentialPerson.FAMS != null) // There are families
                        {
                            foreach (var currentFAMS in FAMS)
                            {
                                // First need to find the matching relationshiop in the potential person.
                                foreach (var potentialFAMS in potentialPerson.FAMS)
                                {
                                    // Now see if the is the correct match of family.
                                    if (currentFAMS.Match(potentialFAMS, report, appConfig.loggingLevel))
                                    {
                                        // There is a family, so first check the spouse if there is one defined (remember there could be multiple)
                                        if (appConfig.MatchSpouse)
                                        {
                                            if (currentFAMS.family.Wife != null && potentialFAMS.family.Wife != null)
                                            {
                                                // There is a wife, and it does not match
                                                if (currentFAMS.family.Wife.person != this)
                                                {
                                                    // Compare the Wife
                                                    if (appConfig.loggingLevel == LogLevel.Trace) report.AppendFormat("{0} - Matching Spouse (Wife - {1}){2}", this.Name, potentialFAMS.family.Wife.person.Name, Environment.NewLine);
                                                    currentFAMS.family.Wife.person.MatchIterative(potentialFAMS.family.Wife.person, report, appConfig);
                                                }

                                            }
                                            if (currentFAMS.family.Husband != null && potentialFAMS.family.Husband != null)
                                            {
                                                // There is a husband, and it does not match
                                                if (currentFAMS.family.Husband.person != this)
                                                {
                                                    // Compare the Husband
                                                    if (appConfig.loggingLevel == LogLevel.Trace) report.AppendFormat("{0} - Matching Spouse (Husband - {1}){2}", this.Name, potentialFAMS.family.Husband.person.Name, Environment.NewLine);
                                                    currentFAMS.family.Husband.person.MatchIterative(potentialFAMS.family.Husband.person, report, appConfig);
                                                }
                                            }
                                        }

                                        // Now we have done the parents, We need to match any children (unless this is not blood line or you have chosen not to)
                                        if (appConfig.MatchChildren)
                                        {
                                            // We need to iterate the children, We cannot assume that they are listed in the same order

                                            List<LinkPerson> masterChildren = currentFAMS.family.Children;
                                            List<LinkPerson> compareChildren = potentialFAMS.family.Children;

                                            foreach (var masterChild in masterChildren)
                                            {
                                                // You are trying to match, even though there may not be one.
                                                foreach (var compareChild in compareChildren)
                                                {
                                                    if (appConfig.loggingLevel == LogLevel.Trace) report.AppendFormat("-- Matching Child {0} ({1}) of [{2}]{3}", masterChild.person.Name, masterChild.person.id, this.Name, Environment.NewLine);

                                                    // Match the Children but don't need to match parents, as this is where we are coming from.
                                                    masterChild.person.MatchIterative(compareChild.person, report, appConfig);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // This person did not match. 
            }
        }
       
        public LinkFamily findFlagRecursive(string flag)
        {
            // returns the LinkFamily where the isIgnored Flag has been set
            // If not exists, then null
            LinkFamily match = null;
            if (this.FAMC != null) // Check there are some parents (could be top of the tree)
            {
                if (this.FAMC.family != null)
                {
                    if (this.FAMC.family.FlagExists(flag)) match = this.FAMC;
                    if (match == null)
                    {
                        // Still no match so need to look further up the tree.
                        // Check if the father was ignored (assuming there was a father)
                        if (this.FAMC.family.Husband != null) match = this.FAMC.family.Husband.person.findFlagRecursive(flag);
                        // If the husband was not ignored, try the mothers line (if one exists)
                        if (match == null & this.FAMC.family.Wife != null) match = this.FAMC.family.Wife.person.findFlagRecursive(flag);
                    }                                    
                }
            }
            return match;
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
                    bool canParse = int.TryParse(sday, out day);
                    if (canParse)
                    {
                        sday = day.ToString();
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
                    case "LABL":
                        // This is a family label so record it.
                        this.Flags.Add(line.Details);
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

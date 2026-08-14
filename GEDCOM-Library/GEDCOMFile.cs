using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GEDCOM
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical,
        None
    }

    public class GEDCOMFile
    {
        public List<BaseEntry> Records = new List<BaseEntry>();
        public List<INDI> people = new List<INDI>();
        public List<SOUR> sources = new List<SOUR>();
        public List<FAM> families = new List<FAM>();
        public List<LABL> labels = new List<LABL>();
        public string path = "";

        public GEDCOMFile(string filename)
        {
            path = filename;
            // First Read the file
            ReadFile(filename);

            // Now you have read the file, parse the records to get the data
            ParseINDI();
            ParseFAM();
            ParseLABL();
        }

        private void ParseINDI()
        {
            foreach (var person in people)
            {
                person.Parse();
                // You can only be a child in one family, so find it (if it exists)
                if (person.FAMC != null) person.FAMC.family = FindFamily(person.FAMC.id, families);
                // if (person.FAMS.Count > 1) Debugger.Break();
         
                // You can be a spouse in more than one family, so find them all. 
                foreach (var currentFAMS in person.FAMS)
                {
                    currentFAMS.family = FindFamily(currentFAMS.id, families);
                }

                // if (person.FAMS.Count > 1) Debugger.Break();
            }
        }

        private FAM FindFamily(string id, List<FAM> families)
        {
            FAM returnFamily = null;
            foreach (var family in families)
            {
                if (family.id == id) {
                    returnFamily = family;
                    break; }
            }
            return returnFamily;
        }

        private void ParseFAM()
        {
            foreach (FAM family in families)
            {
                family.Parse();
                if (family.Husband != null)
                {
                    family.Husband.person = FindPerson(family.Husband.id, people);
                }

                if (family.Wife != null)
                {
                    family.Wife.person = FindPerson(family.Wife.id, people);
                }

                foreach (var person in family.Children)
                {
                    // Find the person for each child
                    person.person = FindPerson(person.id, people);
                }
            }
        }
        private void ParseLABL()
        {
            foreach (var label in labels)
            {
                label.Parse();
            }
        }

        public void SetIgnoredDescendents(string flagTitle)
        {
            string flagId = null;
            List<FAM> ignoreFamilyDescendents = null;
            // Get the code for the flag title
            flagId = labels.Find(x => x.Description.Equals(flagTitle, StringComparison.CurrentCultureIgnoreCase)).Id;

            // Get a list of families which contain the @L1 flag
            ignoreFamilyDescendents = families.FindAll(
            delegate(FAM family)
            {
                return (family.Flags.Contains(flagId)); // not matched, not excluded and not bloodline
            }
            );

            // Iterate the families who we need to ignore
            foreach (FAM family in ignoreFamilyDescendents)
            {
                family.ignoreDescendents = true;
                // Set the flag for all children of this family
                foreach (var child in family.Children)
                {
                    child.person.isIgnoredDecendent = true;
                    // But now you need to iterate their spouses and children to set the flag for them as well. This is a recursive function.
                    SetIgnoredDescendentsAndPartner(child.person);

                }
            }
        }
        private void SetIgnoredDescendentsAndPartner(INDI person)
        {
            // Set the flag for all children of this family
            foreach (var currentFAMS in person.FAMS)
            {
                // Ignore both Husband and Wife, appreciate one will have already been done. But we don't know which one was the original person, so set both.
                currentFAMS.family.Wife?.person.isIgnoredDecendent = true;
                currentFAMS.family.Husband?.person.isIgnoredDecendent = true;
                // Now do all the children of this family
                foreach (var child in currentFAMS.family.Children)
                {
                    child.person.isIgnoredDecendent = true;
                    // But now you need to iterate their spouses and children to set the flag for them as well. This is a recursive function.
                    SetIgnoredDescendentsAndPartner(child.person);
                }
            }
        }

        public void CopyIgnoredFamiliesToLinkedFamilies()
        {
            foreach (var family in families)
            {
                // Copy the flag to the linked family
                family.familyMatch?.Flags = [.. family.Flags];
            }
        }

        public void MatchFamilies(StringBuilder report)
        {
            foreach (var family in families)
            {
                if (family.familyMatch == null)
                {
                    // First see if there are parents/spouses which match. There needs to be a Husband and Wife for this to work
                    if (family.Husband != null && family.Wife != null && family.Husband.person?.personMatch != null && family.Wife.person?.personMatch != null)
                    {
                        foreach (var potentialFamily in family.Husband.person?.personMatch.FAMS)
                        {
                            if (potentialFamily.family.Wife.person.personMatch != null)
                            {
                                if (potentialFamily.family.Wife.person.personMatch.Equals(family.Wife?.person))
                                {
                                    family.familyMatch = potentialFamily.family;
                                    potentialFamily.family.familyMatch = family;
                                    break;
                                }
                            }
                        }
                    }
                }

                // First see if there are children in the family. If there are, this is easier as
                // children only have a single set of parents, where parents can belong to multiple families.
                if (family.familyMatch == null && family.Children.Count > 0)
                {
                    // There are children, so use one of them to find the correct matching. Ensuring they have a person match
                    INDI child = family.Children.Find(x=>x.person.personMatch != null)?.person;
                    if (child != null){
                        family.familyMatch = child.personMatch.FAMC?.family;
                        child.personMatch.FAMC?.family.familyMatch = family;
                    }
                }

                // Still not matched, so either there were no children, or those that were did not have matches
                if (family.familyMatch == null)
                {
                    INDI husband = family.Husband?.person;
                    INDI wife = family.Wife?.person;
                    if (husband != null && wife != null &&husband.personMatch != null && wife.personMatch != null)
                    {
                        foreach (var potentialFamily in husband.personMatch.FAMS)
                        {
                            INDI potentialWife = potentialFamily.family.Wife?.person;
                            if (potentialWife != null && potentialWife.personMatch != null)
                            {
                                if(potentialWife.personMatch.Equals(wife))
                                {
                                    family.familyMatch = potentialFamily.family;
                                    potentialFamily.family.familyMatch = family;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void MatchDOD(StringBuilder report)
        {
            String DOD = "";
            String mDOD = "";
            foreach (var person in people)
            {
                if (person.personMatch != null)
                {
                    DOD = (person.DOD == null || person.DOD.Trim() == "") ? "" : person.DOD;
                    mDOD = (person.personMatch?.DOD == null || person.personMatch?.DOD.Trim() == "") ? "" : person.personMatch?.DOD;
                    if (DOD == mDOD)
                    {
                        person.DODMatch = true;
                        person.personMatch.DODMatch = true;
                    }
                    else if(INDI.stdDate(person.personMatch?.DOD) == INDI.stdDate(person.DOD))
                    {
                        // Ensure both Dates are flagged as matched
                        person.DODMatch = true;
                        person.personMatch.DODMatch = true;
                    }
                }
            }
        }
        public void MatchDOM(StringBuilder report)
        {
            String DOM = "";
            String mDOM = "";
            foreach (var family in families)
            {
                if (family.familyMatch != null)
                {
                    DOM = (family.DOMarriage == null || family.DOMarriage.Trim() == "") ? "" : family.DOMarriage;
                    mDOM = (family.familyMatch.DOMarriage == null || family.familyMatch.DOMarriage.Trim() == "") ? "" : family.familyMatch.DOMarriage;

                    if (DOM == mDOM)
                    {
                        family.DOMarriageMatch = true;
                        family.familyMatch.DOMarriageMatch = true;
                    }
                    else if(INDI.stdDate(family.familyMatch?.DOMarriage) == INDI.stdDate(family.DOMarriage))
                    {
                        // Ensure both Dates are flagged as matched
                        family.DOMarriageMatch = true;
                        family.familyMatch.DOMarriageMatch = true;
                    }
                }
            }
        }
        public INDI FindPerson(string Name)
        {
            INDI returnPerson = null;
            foreach (var person in people)
            {
                if (person.Name == Name) { returnPerson = person; break; }
            }
            if (returnPerson != null)
            {
                returnPerson.SetInTree();
                returnPerson.SetBloodLine(true);
            }
            return returnPerson;
        }
        private INDI FindPerson(string id, List<INDI> people)
        {
            INDI returnPerson = null;
            foreach (var person in people)
            {
                if (person.id == id) { returnPerson = person; break; }
            }
            return returnPerson;
        }
            
        private void ReadFile(string filename)
        {
            // Validate and load the file into the class
            EntryList currentRecord = null;
            int counter = 0;
            INDI person;
            SOUR source;
            FAM family;
            LABL label;
            BaseEntry newRecord = null;
            BaseEntry lastRecord = null;

            // Read the file and display it line by line.
            if (System.IO.File.Exists(filename))
            {
                foreach (string line in System.IO.File.ReadLines(filename))
                {
                    newRecord = new BaseEntry(line);
                    if (newRecord.Level == 0)
                    {
                        // This is a new base entry, so create the appropriate record.
                        switch (newRecord.Type)
                        {
                            case "HEAD":
                                currentRecord = null;
                                break;
                            case "SUBM":
                                currentRecord = null;
                                break;
                            case "INDI":
                                person = new INDI(line);
                                people.Add(person);
                                currentRecord = person;
                                break;
                            case "SOUR":
                                source = new SOUR(line);
                                sources.Add(source);
                                currentRecord = source;
                                break;
                            case "FAM":
                                family = new FAM(line);
                                families.Add(family);
                                currentRecord = family;
                                break;
                            case "LABL":
                                label = new LABL(line);
                                labels.Add(label);
                                currentRecord = label;
                                break;
                            default:
                                currentRecord = null;
                                break;
                        }
                        // Add this record in
                        Records.Add(newRecord);
                    }
                    else
                    {
                        if (newRecord.Type == "CONC")
                        {
                            // This is a continuation of the last record
                            lastRecord.appendDetails(newRecord.Details);
                            // Don't include this as a separate record
                        }
                        else
                        {
                            // This record is not a continuation
                            // If we are populating a record do so.
                            if (currentRecord != null)
                            {
                                currentRecord.lines.Add(newRecord);
                            }
                            // Ensure this is added to the overall list of records
                            Records.Add(newRecord);
                        }
                    }
                    // Ensure we have a link to the last record for CONC records
                    lastRecord = newRecord;

                    counter++;
                }
            }
            else
            {
                Debug.WriteLine(String.Format("File not found: {0}", filename));
            }
        }
    }
}

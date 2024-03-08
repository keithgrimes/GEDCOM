using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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

        public GEDCOMFile(string filename)
        {
            // First Read the file
            ReadFile(filename);

            // Now you have read the file, parse the records to get the data
            ParseINDI();
            ParseFAM();

            // Now link the records
        }

        private void ParseINDI()
        {
            foreach (var person in people)
            {
                person.Parse();
                if (person.FAMC != null) person.FAMC.family = FindFamily(person.FAMC.id, families);
                // if (person.FAMS.Count > 1) Debugger.Break();
         
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
            foreach (var family in families)
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

        public void ListNotUsed(StringBuilder report)
        {
            foreach (var person in people)
            {
                if ( !person.reportIncluded)
                {
                    // This person is not included in the report
                    report.AppendFormat("{2} - {0}({1}) Not included within Tree Structure {3}", person.Name, person.DOB, person.id, Environment.NewLine);
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

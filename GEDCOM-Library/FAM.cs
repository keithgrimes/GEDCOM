using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading.Tasks.Dataflow;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class FAM : EntryList
    {
        public string id { get; }
        public LinkPerson Husband { get; set; }
        public LinkPerson Wife { get; set; }
        public List<LinkPerson> Children { get; set; }
        public List<String> Flags { get; set; }
        public FAM familyMatch { get; set; }
        public bool ignoreDescendents {get; set;}
        public bool matchAttempted {get; set;}
        public String DOMarriage { get; set; }
        public bool DOMarriageMatch { get; set; }

        public FAM(string line) : base(line)
        {
            // ID is going to be on the first record
            id = this.lines[0].Details;
            // Initialise the Children List
            Children = new List<LinkPerson>();
            Flags = new List<String>();
            ignoreDescendents= false;
            matchAttempted = false;
        }
        public bool ChildCountMatch()
        {
            if (this.familyMatch != null)
            {
                if (this.Children.Count == this.familyMatch.Children.Count)
                {
                    return true;
                }
            }
            return false;
        }
        public bool FlagExists(string flg)
        {
            foreach (string f in Flags)
            {
                if (f.ToUpper().Trim() == flg.ToUpper().Trim()) return true;
            }
            return false;
        }

        public void Parse()
        {
            String[] path = {"FAM"};
            foreach (var line in base.lines)
            {
                //create the path;
                if (line.Level > 0)
                {
                    if (path.Length == line.Level + 1) path[line.Level] = line.Type;
                    else if (path.Length < line.Level + 1)
                    {
                        Array.Resize(ref path, line.Level + 1);
                        path[line.Level] = line.Type;
                    }
                    else if (path.Length > line.Level + 1)
                    {
                        Array.Resize(ref path, line.Level + 1);
                        path[line.Level] = line.Type;
                    }
                }

                switch (line.Type)
                {
                    case "HUSB":
                        Husband = new LinkPerson(line.Details);
                        break;
                    case "WIFE":
                        Wife = new LinkPerson(line.Details);
                        break;
                    case "CHIL":
                        Children.Add(new LinkPerson(line.Details));
                        break;
                    case "LABL":
                        // This is a family label so record it.
                        this.Flags.Add(line.Details);
                        break;
                    case "DATE":
                        if (String.Join('/', path) == "FAM/MARR/DATE") // Marriage Date is at level 2  
                        {
                            DOMarriage = INDI.stdDate(line.Details);
                        }
                        break;
                }

            }
        }

        public override int GetHashCode()
        {
            // Create hash based on content, not reference
            var hashCode = new HashCode();
            hashCode.Add(Husband?.person?.Name);
            hashCode.Add(Wife?.person?.Name);
            return hashCode.ToHashCode();
        }

        public override bool Equals(object obj)
        {
            bool matched = false;
            bool husbandMatched = false;
            bool wifeMatched = false;
            if (obj is not FAM fam) return false;

            matchAttempted = true;
            fam.matchAttempted = true;

            // There is a Husband and Wife in this family so use this to validate
            if (this.Husband.person?.personMatch != null && this.Wife.person?.personMatch != null)
            {
                // The husband has a person match so use that to compare instead of the person object.
                matched = ( fam.Husband.person?.personMatch == this.Husband.person)
                                &&
                            (fam.Wife.person.personMatch == this.Wife.person) ? true : false;

                return matched;
            }

            // So now at least match the mother or father (depending upon which one is there)
            if (this.Husband.person?.personMatch != null) husbandMatched = fam.Husband.person?.personMatch == this.Husband.person;
            if (this.Wife.person?.personMatch != null) wifeMatched = fam.Wife.person?.personMatch == this.Wife.person;
            
            // At this point there is only 1 parent in the family. Try the children to see if their parents match
            if ((husbandMatched || wifeMatched) && this.Children.Count > 0)
            {
                // Take the first child of the family and see if they have a person match.
                INDI child = this.Children[0].person;
                if (child.personMatch != null) // Check they had a match
                {
                    // Now start matching this person's parents across trees.
                    if (husbandMatched) matched = (child.personMatch.FAMC.family.Husband.person.personMatch == this.Husband.person) ? true : false;
                    if (!matched && wifeMatched) matched = (child.personMatch.FAMC.family.Wife.person.personMatch == this.Wife.person) ? true : false;
                    return matched;
                }
            }
 
            return (fam.Husband.person.Equals(this.Husband.person) && fam.Wife.person.Equals(this.Wife.person));
        }

        public override string ToString()
        {
            string strHusband = (Husband != null) ? Husband.ToString() : "*** Not Set ***";
            string strWife = (Wife != null) ? Wife.ToString() : "*** Not Set ***";
            return string.Format("({0}) {1} - {2}", this.id, strHusband, strWife);
        }

        public bool isBloodline()
        {
            if (this.Husband?.person?.isBloodLine == true || this.Wife?.person?.isBloodLine == true) return true;
            return false;
        }

        private string DebuggerDisplay
        {
            get
            {
                return this.ToString();
            }
        }
    }
}

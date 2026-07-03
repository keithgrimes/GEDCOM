using System.Diagnostics;
namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class LABL : EntryList
    {
        public string Id { get; }
        public string Description { get; set;}

        public LABL(string line) : base(line)
        {
            // ID is going to be on the first record
            Id = this.lines[0].Details;
            Description = "";
        }

        private string DebuggerDisplay
        {
            get
            {
                return Id + " - " + Description;
            }
        }

        public void Parse()
        {
            foreach (var line in base.lines)
            {
                switch (line.Type)
                {
                    case "TITL":
                        //Set the title of the label.
                        Description = line.Details;
                        break;
                }
            }
        }
    }
}

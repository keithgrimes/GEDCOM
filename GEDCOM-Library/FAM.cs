using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class FAM : EntryList
    {
        public string id { get; }
        public LinkPerson Husband { get; set; }
        public LinkPerson Wife { get; set; }
        public List<LinkPerson> Children { get; set; }

        public FAM(string line) : base(line)
        {
            // ID is going to be on the first record
            id = this.lines[0].Details;
            // Initialise the Children List
            Children = new List<LinkPerson>();
        }

        public void Parse()
        {
            if (this.id == "@300@") Debugger.Break();
            foreach (var line in base.lines)
            {
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
                }

            }
        }

        public override string ToString()
        {
            string strHusband = (Husband != null) ? Husband.ToString() : "*** Not Set ***";
            string strWife = (Wife != null) ? Wife.ToString() : "*** Not Set ***";
            return string.Format("{0} - {1}", strHusband, strWife);
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

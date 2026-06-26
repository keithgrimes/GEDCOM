using System;
using GEDCOM;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CONFIG
    {
        public string masterFileName { get; set;}
        public string comparisonFileName { get; set;}
        public string masterPersonName { get; set;}
        public string reportFileName { get; set;}
        public string matchDOB { get; set;}            
        public string matchDOD { get; set;}
        public GEDCOM.LogLevel loggingLevel { get; set;}
        public string flgNotBloodLine { get; set;}
        public string flgIgnoreDescendents { get; set;}
        public bool MatchChildren { get; set;}
        public bool MatchSpouse { get; set;}
        public bool MatchParents { get; set;}
        public string BreakPersonId { get; set; }

        public CONFIG()
        {
            // ID is going to be on the first record
            masterFileName = "";
            comparisonFileName = "";
            matchDOB = "";
            matchDOD = "";
            loggingLevel = GEDCOM.LogLevel.Information;
            reportFileName = "";
            masterPersonName = "";
            flgIgnoreDescendents = "";
            flgNotBloodLine = "";
            MatchChildren = true;
            MatchSpouse = true;
            MatchParents = true;
            BreakPersonId = "";
        }
        
        private string DebuggerDisplay
        {
            get
            {
                return masterFileName;
            }
        }
    }
}

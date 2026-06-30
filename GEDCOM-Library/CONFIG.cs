using System.Diagnostics;
using Microsoft.Extensions.Configuration;


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
        }
        public CONFIG(string configFile)
        {
            this.LoadConfiguration(configFile);
        }       

        private string DebuggerDisplay
        {
            get
            {
                return masterFileName;
            }
        }
        static bool StrToBool(string str)
        {
            return !str.ToUpper().Trim().Equals("FALSE");
        }
        public void LoadConfiguration(string configFile)
        {
            var Builder = new ConfigurationBuilder().AddJsonFile(configFile, false, true);
            var config = Builder.Build();
            
            this.masterFileName = config["masterFile:fileName"];
            this.comparisonFileName = config["comparisonFile:fileName"];
            this.masterPersonName = config["masterFile:person"];
            this.reportFileName = config["ReportFile"];
            this.matchDOB = config["Matching:matchDOB"];
            this.matchDOD = config["Matching:matchDOD"];

            this.flgNotBloodLine = config["Labels:NotBloodLine"];
            this.flgIgnoreDescendents = config["Labels:IgnoreDescendents"];

            this.MatchChildren = StrToBool(config["masterFile:MatchChildren"]);
            this.MatchParents = StrToBool(config["masterFile:MatchParents"]);
            this.MatchSpouse = StrToBool(config["masterFile:MatchSpouse"]);

            switch (config["Logging:LogLevel:Default"].ToUpper())
            {
                case "TRACE":
                    this.loggingLevel = LogLevel.Trace;
                    break;
                default:
                    // Trace, Debug, Information, Warning, Error, Critical, None
                    this.loggingLevel = LogLevel.Information;
                    break;
            }
            return;
        }
    }
}

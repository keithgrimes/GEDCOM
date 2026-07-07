using System.Diagnostics;
using Microsoft.Extensions.Configuration;
namespace GEDCOM
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CONFIGReporting
    {
        public bool reportIncludedinTreeNotMatched { get; set;}
        public bool reportNotIncludedinTree { get; set;}
        public bool reportNotBloodLine { get; set;}
        public bool reportExcluded { get; set;}
        public bool reportExcludedAndNotBloodLine { get; set;}
        public bool reportUnmatchedFamilies { get; set; }

        public CONFIGReporting()
        {
            // ID is going to be on the first record
            reportIncludedinTreeNotMatched = false;
            reportNotIncludedinTree = false;
            reportNotBloodLine = false;
            reportExcluded = false;
            reportExcludedAndNotBloodLine = false;
            reportUnmatchedFamilies = false;
        }
        private string DebuggerDisplay
        {
            get
            {
                return "Reporting Configuration";
            }
        }
    }
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
        public bool IncludeIgnoredDescendents { get; set;}
        public bool MatchChildren { get; set;}
        public bool MatchSpouse { get; set;}
        public bool MatchParents { get; set;}

        public CONFIGReporting MasterFileReporting = new ();
        public CONFIGReporting ComparisonFileReporting = new ();

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
            MatchChildren = true;
            MatchSpouse = true;
            MatchParents = true;
            IncludeIgnoredDescendents = false;
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

            this.flgIgnoreDescendents = config["Labels:IgnoreDescendents"];

            this.MatchChildren = StrToBool(config["masterFile:MatchChildren"]);
            this.MatchParents = StrToBool(config["masterFile:MatchParents"]);
            this.MatchSpouse = StrToBool(config["masterFile:MatchSpouse"]);
            this.IncludeIgnoredDescendents = StrToBool(config["masterFile:IncludeIgnoredDescendents"]);

            // Load the reporting configuration for the master file
            this.MasterFileReporting.reportIncludedinTreeNotMatched = StrToBool(config["masterFile:Reporting:reportIncludedinTreeNotMatched"]);
            this.MasterFileReporting.reportNotIncludedinTree = StrToBool(config["masterFile:Reporting:reportNotIncludedinTree"]);
            this.MasterFileReporting.reportNotBloodLine = StrToBool(config["masterFile:Reporting:reportNotBloodLine"]);
            this.MasterFileReporting.reportExcluded = StrToBool(config["masterFile:Reporting:reportExcluded"]); 
            this.MasterFileReporting.reportExcludedAndNotBloodLine = StrToBool(config["masterFile:Reporting:reportExcludedAndNotBloodLine"]);
            this.MasterFileReporting.reportUnmatchedFamilies = StrToBool(config["masterFile:Reporting:reportUnmatchedFamilies"]);

            // Load the reporting configuration for the comparison file
            this.ComparisonFileReporting.reportIncludedinTreeNotMatched = StrToBool(config["comparisonFile:Reporting:reportIncludedinTreeNotMatched"]);
            this.ComparisonFileReporting.reportNotIncludedinTree = StrToBool(config["comparisonFile:Reporting:reportNotIncludedinTree"]);
            this.ComparisonFileReporting.reportNotBloodLine = StrToBool(config["comparisonFile:Reporting:reportNotBloodLine"]);
            this.ComparisonFileReporting.reportExcluded = StrToBool(config["comparisonFile:Reporting:reportExcluded"]); 
            this.ComparisonFileReporting.reportExcludedAndNotBloodLine = StrToBool(config["comparisonFile:Reporting:reportExcludedAndNotBloodLine"]);
            this.ComparisonFileReporting.reportUnmatchedFamilies = StrToBool(config["comparisonFile:Reporting:reportUnmatchedFamilies"]);
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

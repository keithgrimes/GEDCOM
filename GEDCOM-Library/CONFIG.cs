using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
namespace GEDCOM
{
    public class CONFIGReporting
    {
        public bool IsMasterFile {get; set;}
        public string filename {get; set;}
        public string csvPath {get; set;}

        public CONFIGReporting()
        {
            IsMasterFile = false;
            filename = "";
            csvPath = "";

            
        }
        private string DebuggerDisplay
        {
            get
            {
                return "Reporting Configuration";
            }
        }
    }
    public class CONFIGMasterFile
    {
        public string filePath {get; set;}
        public string fileName {get; set;}
        public CONFIGReporting Reporting {get; set;}

        public CONFIGMasterFile()
        {
            filePath = "";
            fileName = "";
        }
        public CONFIGMasterFile(string name, string path)
        {
            filePath = path;
            fileName = name;
        }
    }
    public class CONFIGComparisonFile
    {
        public string filePath {get; set;}
        public string fileName {get; set;}
        public bool Include {get; set;}
        public CONFIGReporting Reporting {get; set;}

        public CONFIGComparisonFile()
        {
            filePath = "";
            fileName = "";
            Include = false;
        }
        public CONFIGComparisonFile(string name, string path)
        {
            filePath = path;
            fileName = name;
            Include = false;
        }
    }
    public class BaseConfiguration
    {
        public string MasterPerson {get; set;}
        public bool IncludeIgnoredDescendents { get; set;}
        public bool MatchChildren { get; set;}
        public bool MatchSpouse { get; set;}
        public bool MatchParents { get; set;}
        public bool MatchDOB {get; set;}
        public bool MatchDOD {get; set;}
        public string IgnoreDescendents {get; set;}
        public string FullReport {get; set;}
        public GEDCOM.LogLevel LogLevel {get; set;}
    }
    
    public class CONFIG
    {
        public BaseConfiguration baseConfiguration ;
        public CONFIGMasterFile masterConfiguration;
        public List<CONFIGComparisonFile> comparisonConfiguration;

        public CONFIG(string configFile)
        {
            var Builder = new ConfigurationBuilder().AddJsonFile(configFile, false, true);
            var config = Builder.Build();

            // Get the master and comparison file configuration    
            baseConfiguration = config.GetSection("Configuration").Get<BaseConfiguration>();   
            masterConfiguration = config.GetSection("masterFile").Get<CONFIGMasterFile>();   
            masterConfiguration.Reporting.IsMasterFile = true;  
            comparisonConfiguration = config.GetSection("comparisonFiles").Get<List<CONFIGComparisonFile>>();
            foreach(CONFIGComparisonFile x in comparisonConfiguration) x.Reporting.IsMasterFile = false;

            // Remove any comparisons that have been excluded
            List<CONFIGComparisonFile> exclude = comparisonConfiguration.FindAll(x=>x.Include == false);
            foreach(CONFIGComparisonFile file in exclude)
            {
                comparisonConfiguration.Remove(file);
            }
            return;
        }       
    }
}

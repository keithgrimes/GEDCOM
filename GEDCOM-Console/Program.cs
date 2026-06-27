using System;
using GEDCOM;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace GEDCOM_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            CONFIG appConfig = new CONFIG();
            StringBuilder personReport = new StringBuilder();
            StringBuilder verboseReport = new StringBuilder();
            var Builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", false, true);
            var config = Builder.Build();
            var masterFileName = config["masterFile:fileName"];
            var comparisonFileName = config["comparisonFile:fileName"];
            var masterPersonName = config["masterFile:person"];
            var reportFileName = config["ReportFile"];
            var matchDOB = config["Matching:matchDOB"];
            var matchDOD = config["Matching:matchDOD"];
            var loggingLevel = LogLevel.Information;
            

            appConfig.masterFileName = masterFileName;
            appConfig.comparisonFileName = comparisonFileName;
            appConfig.masterPersonName = masterPersonName;
            appConfig.reportFileName = reportFileName;
            appConfig.matchDOB = matchDOB;
            appConfig.matchDOD = matchDOD;
            //appConfig.BreakPersonId = "@11808996@";

            appConfig.flgNotBloodLine = config["Labels:NotBloodLine"];
            appConfig.flgIgnoreDescendents = config["Labels:IgnoreDescendents"];

            appConfig.MatchChildren = StrToBool(config["masterFile:MatchChildren"]);
            appConfig.MatchParents = StrToBool(config["masterFile:MatchParents"]);
            appConfig.MatchSpouse = StrToBool(config["masterFile:MatchSpouse"]);


            // Now hold the flag information


            switch (config["Logging:LogLevel:Default"].ToUpper())
            {
                case "TRACE":
                    loggingLevel = LogLevel.Trace;
                    break;
                default:
                    // Trace, Debug, Information, Warning, Error, Critical, None
                    loggingLevel = LogLevel.Information;
                    break;
            }
            appConfig.loggingLevel = loggingLevel;

            // Load the master File first
            GEDCOMFile masterFile = new GEDCOMFile(masterFileName);
            // Find the record for the selected person
            INDI masterPerson = masterFile.FindPerson(masterPersonName);

            // Now load the comparison File
            GEDCOMFile comparisonFile = new GEDCOMFile(comparisonFileName);
            // Now find the comparison person record
            INDI comparisonPerson = comparisonFile.FindPerson(masterPersonName);

            personReport.AppendFormat("MasterFile Statistics: {0}{1}", masterFileName, Environment.NewLine);
            personReport.AppendFormat("People Count: {0}{1}", masterFile.people.Count.ToString(), Environment.NewLine);
            personReport.AppendFormat("Family Count: {0}{1}{1}", masterFile.families.Count, Environment.NewLine);
            personReport.AppendFormat("ComparisonFile Statistics: {0}{1}", comparisonFileName.ToString(), Environment.NewLine);
            personReport.AppendFormat("People Count: {0}{1}", comparisonFile.people.Count, Environment.NewLine);
            personReport.AppendFormat("Family Count: {0}{1}{1}{1}", comparisonFile.families.Count, Environment.NewLine);
            personReport.AppendFormat("***************  Generating Report *****************{0}", Environment.NewLine);

            // We have now loaded the files and got the people to start comparing
            masterPerson.MatchIterative(comparisonPerson, verboseReport, appConfig);

            /*
            ** At this point all the data has been loaded. The trees can be queried to see how they have
            ** been configured. 
            ** 
            ** To find a person (Example). Execute in the debug console
            **      - comparisonFile.people.Find(x=>x.Name.Contains("Thomas Brumwell"))
            **      - comparisonFile.people.Find(x=>x.id.Contains("@I1224@))
            **
            ** These enable you to find specific people within tree structure.
            */

            personReport.AppendFormat("***************  Generating Report *****************{0}", Environment.NewLine);

            int MissingCount = 0;
            masterPerson.ReportDifferences(true, ref MissingCount, personReport, appConfig);
            personReport.AppendFormat("{0}{0} ******************** Non Linked People **************{0}", Environment.NewLine);
            masterFile.ListNotUsed(personReport, appConfig);

            personReport.AppendFormat("Processing Complete{0}", Environment.NewLine);

            if (loggingLevel == LogLevel.Trace)
            {
                personReport.AppendFormat("{0}{0}****************** Verbose Reporting ******************{0}", Environment.NewLine);
                personReport.Append(verboseReport);
                personReport.AppendFormat("{0}{0}****************** End of Verbose Reporting ******************{0}", Environment.NewLine);
            }


            // Only write the file if the report directory exists
            if (Directory.Exists(Path.GetDirectoryName(reportFileName)))
            {
                // We have now done the comparison
                personReport.AppendFormat("Report File Path has been updated ({0}){1}", reportFileName, Environment.NewLine);
                // Now we need to write the report out. First check the file does not exist
                File.WriteAllText(reportFileName, personReport.ToString());
            }
            else
            {
                // We have now done the comparison
                personReport.AppendFormat("Report File Path was not found ({0}){1}", reportFileName, Environment.NewLine);
            }

            // We have now done the comparison
            Console.WriteLine(personReport.ToString());
        }

        static bool StrToBool(string str)
        {
            if (str.ToUpper().Trim().Equals("FALSE"))
            {
                return false;
            }
            return true;
        }
    }
}

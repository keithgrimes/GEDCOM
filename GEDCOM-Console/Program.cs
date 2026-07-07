using System;
using GEDCOM;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace GEDCOM_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            CONFIG appConfig = new($"appsettings.json");
            StringBuilder personReport = new ();
            StringBuilder verboseReport = new ();

            // Load the master File first
            GEDCOMFile masterFile = new(appConfig.masterFileName);

            // Find the record for the selected person
            INDI masterPerson = masterFile.FindPerson(appConfig.masterPersonName);
        
            // Now load the comparison File
            GEDCOMFile comparisonFile = new (appConfig.comparisonFileName);
            // Now find the comparison person record
            INDI comparisonPerson = comparisonFile.FindPerson(appConfig.masterPersonName);

            // We have now loaded the files and got the people to start comparing and linking.
            masterPerson.MatchIterative(comparisonPerson, verboseReport, appConfig);
            // Once we have Matched the people we can match the Families
            masterFile.MatchFamilies(verboseReport);
            
            if (!appConfig.IncludeIgnoredDescendents){
                masterFile.SetIgnoredDescendents(appConfig.flgIgnoreDescendents);
                // Copy Flags for Families
                masterFile.CopyIgnoredFamiliesToLinkedFamilies();
                // Set the ignored descendents in the comparison file.
                comparisonFile.labels = new List<LABL>(masterFile.labels);
                comparisonFile.SetIgnoredDescendents(appConfig.flgIgnoreDescendents);
            }
            /*
            ** At this point all the data has been loaded. The trees can be queried to see how they have
            ** been configured. 
            ** Matching poeple should have been found so this can be validated through the Linked Person Field
            ** People in the tree will have the flag set, those not won't.
            ** 
            ** To find a person (Example). Execute in the debug console
            **      - comparisonFile.people.Find(x=>x.Name.Contains("Thomas Brumwell"))
            **      - comparisonFile.people.Find(x=>x.id.Contains("@I1224@))
            **
            ** These enable you to find specific people within tree structure.
            */

            // Now report the appropriate configuration
            ReportConfiguration("Master File", masterFile, appConfig.MasterFileReporting, personReport);
            ReportConfiguration("Comparison File", comparisonFile, appConfig.ComparisonFileReporting, personReport);

            personReport.AppendFormat("{0}{0}Processing Complete{0}", Environment.NewLine);

            if (appConfig.loggingLevel == LogLevel.Trace)
            {
                personReport.AppendFormat("{0}{0}****************** Trace Reporting ******************{0}", Environment.NewLine);
                personReport.Append(verboseReport);
                personReport.AppendFormat("{0}{0}****************** End of Trace Reporting ******************{0}", Environment.NewLine);
            }


            // Only write the file if the report directory exists
            if (Directory.Exists(Path.GetDirectoryName(appConfig.reportFileName)))
            {
                // We have now done the comparison
                personReport.AppendFormat("Report File Path has been updated ({0}){1}", appConfig.reportFileName, Environment.NewLine);
                // Now we need to write the report out. First check the file does not exist
                File.WriteAllText(appConfig.reportFileName, personReport.ToString());
            }
            else
            {
                // We have now done the comparison, But the file path was not found. Log to the screen/console
                personReport.AppendFormat("Report File Path was not found ({0}){1}", appConfig.reportFileName, Environment.NewLine);
            }

            // We have now done the comparison
            Console.WriteLine(personReport.ToString());
        }
        static void ReportConfiguration(String name, GEDCOMFile file, CONFIGReporting cfg, StringBuilder report)
        {
            /*
            ** Summary stats
            ** 
            ** Total number of people in the master file = Total number included in tree + Total number not included in tree
            ** Total number included in tree = Total number in tree matched + Total number in tree not matched
            ** Total number to check = Total number in tree - total number ignored - total number not bloodline
            ** Total number to check (matched) = Total number in tree matched - total number ignored - total number not bloodline where matched
            ** Total number being checked (not matched)) = Total number in tree matched - total number ignored - total number not bloodline not matched
            */
            report.AppendFormat("***************  Reporting Totals for {1}  *****************{0}", Environment.NewLine, name);
            report.AppendFormat("{2} Statistics: {0}{1}", file.path, Environment.NewLine, name);
            report.AppendFormat("People Count: {0}{1}", file.people.Count, Environment.NewLine);
            report.AppendFormat("Family Count: {0}{1}{1}", file.families.Count, Environment.NewLine);

            List<FAM> famMatched = null;
            List<FAM> famNotMatched = null;
            famMatched = file.families.FindAll(x=>x.familyMatch != null); 
            famNotMatched = file.families.FindAll(x=>x.familyMatch == null); // Total People not included in the tree
            report.AppendFormat("Number of families in the file (Matched : {0}, Not Matched : {1}){2}", famMatched.Count, famNotMatched.Count, Environment.NewLine);


            List<INDI> inTree = null;
            List<INDI> notInTree = null;
            inTree = file.people.FindAll(x=>x.isIncludedInTree); // Total People included in the tree
            notInTree = file.people.FindAll(x=>!x.isIncludedInTree); // Total People not included in the tree
            report.AppendFormat("Number of people in the file (InTree : {0}, NotInTree : {1}){2}", inTree.Count, notInTree.Count, Environment.NewLine);

            List<INDI> InTreeIncluded = null;
            List<INDI> InTreeExcluded = null;
            InTreeIncluded = inTree.FindAll(x=>x.isBloodLine && !x.isIgnoredDecendent ); // Total People in the tree and included            
            InTreeExcluded = inTree.FindAll(x=>!x.isBloodLine || x.isIgnoredDecendent ); // Total People in the tree and excluded
            report.AppendFormat("Number of people in the tree (Included {0}, Excluded {1}){2}", InTreeIncluded.Count, InTreeExcluded.Count, Environment.NewLine);

            List<INDI> InTreeIncludedMatched = null;
            List<INDI> InTreeIncludedNotMatched = null;
            InTreeIncludedMatched = InTreeIncluded.FindAll(x=>x.personMatch != null ); // Total People not included in the tree
            InTreeIncludedNotMatched = InTreeIncluded.FindAll(x=>x.personMatch == null ); // Total People not included in the tree
            report.AppendFormat("Number of people in the tree, included (matched : {0}, unmatched {1}){2}", InTreeIncludedMatched.Count, InTreeIncludedNotMatched.Count, Environment.NewLine);

            List<INDI> InTreeExcludedMatched = null;
            List<INDI> InTreeExcludedNotMatched = null;
            InTreeExcludedMatched = InTreeExcluded.FindAll(x=>x.personMatch != null ); // Total People in tree, excluded and not matched
            InTreeExcludedNotMatched = InTreeExcluded.FindAll(x=>x.personMatch == null ); // Total People in tree, excluded and not matched
            report.AppendFormat("Number of people in the tree, excluded (matched : {0}, ummatched {1}){2}", InTreeExcludedMatched.Count, InTreeExcludedNotMatched.Count, Environment.NewLine);

            List<INDI> InTreeExcludedNotBloodLine = null;
            InTreeExcludedNotBloodLine = InTreeExcluded.FindAll(x=>!x.isBloodLine); // Total People not included in the tree
            report.AppendFormat("Number of people in the tree, excluded due to not Blood Line {0}{1}", InTreeExcludedNotBloodLine.Count, Environment.NewLine);

            List<INDI> InTreeExcludedIgnored = null;
            InTreeExcludedIgnored = InTreeExcluded.FindAll(x=>x.isIgnoredDecendent); // Total People not included in the tree
            report.AppendFormat("Number of people in the tree, excluded due to being ignored (flag set) {0}{1}", InTreeExcludedIgnored.Count, Environment.NewLine);

            List<INDI> InTreeExcludedBloodLineAndIgnored = null;
            InTreeExcludedBloodLineAndIgnored = InTreeExcluded.FindAll(x=>!x.isBloodLine && x.isIgnoredDecendent); // Total People not included in the tree
            report.AppendFormat("Number of people in the tree, excluded due to not Blood Line and being ignored {0}{1}", InTreeExcludedBloodLineAndIgnored.Count, Environment.NewLine);


            // Now list the details, but first list some counts

            report.AppendFormat("{0}{0}***************  Generating Reports for {1}  *****************{0}", Environment.NewLine, name);

            if (cfg.reportUnmatchedFamilies)
            {
                report.AppendFormat("***************  Families who are in tree and not matched ({1}) *****************{0}{0}", Environment.NewLine, famNotMatched.Count);
                foreach(FAM family in famNotMatched)report.AppendFormat("{1} - ({0}) Not matched{2}", family.ToString(), family.id, Environment.NewLine);
            }

            if (cfg.reportIncludedinTreeNotMatched)
            {
                report.AppendFormat("***************  People who are in tree, included and not matched ({1})*****************{0}{0}", Environment.NewLine, InTreeIncludedNotMatched.Count);
                foreach(INDI ancestor in InTreeIncludedNotMatched)report.AppendFormat("{2} - {0} ({1}) Not matched{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }

            if (cfg.reportExcludedAndNotBloodLine)
            {
                report.AppendFormat("***************  People who are in tree, Excluded due to Ignored and not Blood Line ({1})*****************{0}{0}", Environment.NewLine, InTreeExcludedBloodLineAndIgnored.Count);
                foreach(INDI ancestor in InTreeExcludedBloodLineAndIgnored)report.AppendFormat("{2} - {0} ({1}) Ignored & Not Blood Line {3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);                
            }

            if (cfg.reportExcluded)
            {
                report.AppendFormat("***************  People who are in tree, Excluded due to Selection ({1})*****************{0}{0}", Environment.NewLine, InTreeExcludedIgnored.Count);
                foreach(INDI ancestor in InTreeExcludedIgnored)report.AppendFormat("{2} - {0} ({1}) Ignored Only due to Selection (flagged) {3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);                
            }

            if (cfg.reportNotBloodLine)
            {
                report.AppendFormat("***************  People who are in tree, Excluded due not being of Blood Line ({1})*****************{0}{0}", Environment.NewLine, InTreeExcludedNotBloodLine.Count);
                foreach(INDI ancestor in InTreeExcludedNotBloodLine)report.AppendFormat("{2} - {0} ({1}) Ignored Only due to Selection (flagged) {3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);                
            }
            if (cfg.reportNotIncludedinTree)
            {
                report.AppendFormat("***************  People who are NOT in tree ({1})*****************{0}{0}", Environment.NewLine, notInTree.Count);
                foreach(INDI ancestor in notInTree)report.AppendFormat("{2} - {0} ({1}) Not in Tree{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }
            report.AppendFormat("{0}***************  Reporting Completed for {1}  *****************{0}{0}", Environment.NewLine, name);
        }
    }
}

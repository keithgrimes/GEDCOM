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
            masterFile.SetIgnoredDescendents("Ignore Descendents");
            // Find the record for the selected person
            INDI masterPerson = masterFile.FindPerson(appConfig.masterPersonName);
            

            // Now load the comparison File
            GEDCOMFile comparisonFile = new (appConfig.comparisonFileName);
            // Now find the comparison person record
            INDI comparisonPerson = comparisonFile.FindPerson(appConfig.masterPersonName);

            personReport.AppendFormat("MasterFile Statistics: {0}{1}", appConfig.masterFileName, Environment.NewLine);
            personReport.AppendFormat("People Count: {0}{1}", masterFile.people.Count, Environment.NewLine);
            personReport.AppendFormat("Family Count: {0}{1}{1}", masterFile.families.Count, Environment.NewLine);
            personReport.AppendFormat("ComparisonFile Statistics: {0}{1}", appConfig.comparisonFileName.ToString(), Environment.NewLine);
            personReport.AppendFormat("People Count: {0}{1}", comparisonFile.people.Count, Environment.NewLine);
            personReport.AppendFormat("Family Count: {0}{1}{1}{1}", comparisonFile.families.Count, Environment.NewLine);

            // We have now loaded the files and got the people to start comparing and linking.
            masterPerson.MatchIterative(comparisonPerson, verboseReport, appConfig);

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


            /*
            ** Summary stats
            ** 
            ** Total number of people in the master file = Total number included in tree + Total number not included in tree
            ** Total number included in tree = Total number in tree matched + Total number in tree not matched
            ** Total number to check = Total number in tree - total number ignored - total number not bloodline
            ** Total number to check (matched) = Total number in tree matched - total number ignored - total number not bloodline where matched
            ** Total number being checked (not matched)) = Total number in tree matched - total number ignored - total number not bloodline not matched
            */
            // First Report people who exist in the tree but don't have a match and were not excluded(ignoreDecendents or notBloodLine)

            // People who are in the tree 
            List<INDI> inTree = null;
            inTree = masterFile.people.FindAll(x=>x.isIncludedInTree); // Total People included in the tree
            personReport.AppendFormat("Number of people in the tree {0}{1}", inTree.Count, Environment.NewLine);

            //People who are not in the tree
            List<INDI> notInTree = null;
            notInTree = masterFile.people.FindAll(x=>!x.isIncludedInTree); // Total People not included in the tree
            personReport.AppendFormat("Number of people not in the tree {0}{1}", notInTree.Count, Environment.NewLine);

            List<INDI> InTreeIncluded = null;
            InTreeIncluded = inTree.FindAll(x=>x.isBloodLine && !x.isIgnoredDecendent ); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree AND included {0}{1}", InTreeIncluded.Count, Environment.NewLine);
            
            List<INDI> InTreeExcluded = null;
            InTreeExcluded = inTree.FindAll(x=>!x.isBloodLine || x.isIgnoredDecendent ); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree AND excluded {0}{1}", InTreeExcluded.Count, Environment.NewLine);

            List<INDI> InTreeIncludedMatched = null;
            InTreeIncludedMatched = InTreeIncluded.FindAll(x=>x.personMatch != null ); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree, included and matched {0}{1}", InTreeIncludedMatched.Count, Environment.NewLine);

            List<INDI> InTreeIncludedNotMatched = null;
            InTreeIncludedNotMatched = InTreeIncluded.FindAll(x=>x.personMatch == null ); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree, included and not matched {0}{1}", InTreeIncludedNotMatched.Count, Environment.NewLine);

            List<INDI> InTreeExcludedNotBloodLine = null;
            InTreeExcludedNotBloodLine = InTreeExcluded.FindAll(x=>!x.isBloodLine); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree, excluded due to not Blood Line {0}{1}", InTreeExcludedNotBloodLine.Count, Environment.NewLine);

            List<INDI> InTreeExcludedIgnored = null;
            InTreeExcludedIgnored = InTreeExcluded.FindAll(x=>x.isIgnoredDecendent); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree, excluded due to being ignored {0}{1}", InTreeExcludedIgnored.Count, Environment.NewLine);

            List<INDI> InTreeExcludedBloodLineAndIgnored = null;
            InTreeExcludedBloodLineAndIgnored = InTreeExcluded.FindAll(x=>!x.isBloodLine && x.isIgnoredDecendent); // Total People not included in the tree
            personReport.AppendFormat("Number of people in the tree, excluded due to not Blood Line and being ignored {0}{1}", InTreeExcludedBloodLineAndIgnored.Count, Environment.NewLine);


            // Now list the details, but first list some counts

            personReport.AppendFormat("***************  Generating Report *****************{0}", Environment.NewLine);

            personReport.AppendFormat("{0}***************  People who are in tree, included and not matched ({1})*****************{0}{0}", Environment.NewLine, InTreeIncludedNotMatched.Count);
            foreach(INDI ancestor in InTreeIncludedNotMatched)
            {
                personReport.AppendFormat("{2} - {0} ({1}) Not matched{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }

            personReport.AppendFormat("{0}***************  People who are in tree, Excluded due to Ignored and not Blood Line ({1})*****************{0}{0}", Environment.NewLine, InTreeExcludedBloodLineAndIgnored.Count);
            
            foreach(INDI ancestor in InTreeExcludedBloodLineAndIgnored)
            {
                personReport.AppendFormat("{2} - {0} ({1}) Ignored & Not Blood Line {3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }


            //int MissingCount = 0;
            //masterPerson.ReportDifferences(true, ref MissingCount, personReport, appConfig);
            //personReport.AppendFormat("{0}{0} ******************** Non Linked People **************{0}", Environment.NewLine);
            //masterFile.ListNotUsed(personReport, appConfig);

            personReport.AppendFormat("Processing Complete{0}", Environment.NewLine);

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
    }
}

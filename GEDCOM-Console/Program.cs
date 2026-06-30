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

            // First Report people who exist in the tree but don't have a match and were not excluded(ignoreDecendents or notBloodLine)
            List<INDI> notMatched = null;
            notMatched = masterFile.people.FindAll(
            delegate(INDI person)
            {
                bool matched = person.personMatch != null? true : false;
                return (person.isIncludedInTree && !matched && !person.isIgnoredDecendent && person.isBloodLine ); // not matched, not excluded and are bloodline
            }
            );

            // Second Report people who exist in the tree but were excluded by ignore Descendents (Trace Only)
            List<INDI> peopleIgnored = null;
            peopleIgnored = masterFile.people.FindAll(
            delegate(INDI person)
            {
                bool matched = person.personMatch != null? true : false;
                return (person.isIncludedInTree && person.isIgnoredDecendent); // not matched, excluded and bloodline
            }
            );

            // Third Report people who exist in the tree but excluded as they were not of Blood line (Trace Only)
            List<INDI> notBloodline = null;
            notBloodline = masterFile.people.FindAll(
            delegate(INDI person)
            {
                return (person.isIncludedInTree && !person.isBloodLine); // not matched, not excluded and not bloodline
            }
            );

            // Fourth Report people who are not linked to the tree, 
            List<INDI> notInTree = null;
            notInTree = masterFile.people.FindAll(
            delegate(INDI person)
            {
                bool matched = person.personMatch != null? true : false;
                return (!person.isIncludedInTree && (person.isBloodLine || !person.isIgnoredDecendent)); // not included in Tree
            }
            );

            List<INDI> peopleMatched = null;
            peopleMatched = masterFile.people.FindAll(
            delegate(INDI person)
            {
                bool matched = person.personMatch != null? true : false;
                return (person.isIncludedInTree && matched); // not included in Tree
            }
            );

            // Now list the details, but first list some counts

            personReport.AppendFormat("Number of people matched in your tree {0}{1}", peopleMatched.Count, Environment.NewLine);
            personReport.AppendFormat("Number of people not matched (excluding ignored/not blood line) was {0}{1}", notMatched.Count, Environment.NewLine);
            personReport.AppendFormat("Number of people excluded was {0}{1}", peopleIgnored.Count, Environment.NewLine);
            personReport.AppendFormat("Number of people not in your blood line was {0}{1}", notBloodline.Count, Environment.NewLine);
            personReport.AppendFormat("Number of people not in the tree {0}{1}", notInTree.Count, Environment.NewLine);
            personReport.AppendFormat("Total Count of Above {0}{1}", peopleMatched.Count + notMatched.Count + peopleIgnored.Count + notBloodline.Count + notInTree.Count, Environment.NewLine);


            personReport.AppendFormat("***************  Generating Report *****************{0}", Environment.NewLine);

            personReport.AppendFormat("{0}***************  People who are not matched or excluded/not Bloodline *****************{0}{0}", Environment.NewLine);
            foreach(INDI ancestor in notMatched)
            {
                personReport.AppendFormat("{2} - {0} ({1}) Not matched (or excluded by ignoreDescendent or notBloodline){3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }
            personReport.AppendFormat("{0}***************  People who are excluded due to Ancestor/descendent Exclusion (ignoreDecendents) *****************{0}{0}", Environment.NewLine);
            foreach(INDI ancestor in peopleIgnored)
            {
                personReport.AppendFormat("{2} - {0} ({1}) Ignored by Ancestor Exclusion{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }
            personReport.AppendFormat("{0}***************  People who are not in your bloodline *****************{0}{0}", Environment.NewLine);
            foreach(INDI ancestor in notBloodline)
            {
                personReport.AppendFormat("{2} - {0} ({1}) Due to not being bloodline{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
            }
            personReport.AppendFormat("{0}***************  People who are not included within your tree *****************{0}{0}", Environment.NewLine);
            foreach(INDI ancestor in notInTree)
            {
                personReport.AppendFormat("{2} - {0} ({1}) Not within the master persons tree.{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
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

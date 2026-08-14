using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GEDCOM;

public class Comparison
{
    GEDCOMFile masterFile;
    GEDCOMFile comparisonFile;
    CONFIGMasterFile masterFileConfig ; 
    CONFIGComparisonFile comparisonFileConfig;
    BaseConfiguration baseConfiguration;
 
    string FullReportPath = "";
   public Comparison(CONFIGMasterFile masterfileConfig, 
                        CONFIGComparisonFile comparisonfileConfig,
                        BaseConfiguration baseConfig)
    {
        // Load the files.
        masterFile = new GEDCOMFile(masterfileConfig.filePath);
        comparisonFile = new GEDCOMFile(comparisonfileConfig.filePath);   

        // Store the configuration for later use. 
        this.masterFileConfig = masterfileConfig;
        this.comparisonFileConfig = comparisonfileConfig;    
        this.baseConfiguration = baseConfig ;

        // Calculate the Full Report Path
        string filename = String.Concat(this.masterFileConfig.fileName, "-" , this.comparisonFileConfig.fileName, ".txt");
        if (File.Exists(this.baseConfiguration.FullReport))
        {
            // This is a file, so get the parent directory
            FullReportPath = Path.Combine(Directory.GetParent(this.baseConfiguration.FullReport).ToString(), filename);

        }
        else
        {
            FullReportPath = Path.Combine(this.baseConfiguration.FullReport, filename);
        }

    }
   public void Compare(StringBuilder report)
    {
        // Find the records for the starting people
       INDI masterPerson = masterFile.FindPerson(this.baseConfiguration.MasterPerson);
       INDI comparisonPerson = comparisonFile.FindPerson(this.baseConfiguration.MasterPerson);

        // We have now loaded the files and got the people to start comparing and linking.
        masterPerson.MatchIterative(comparisonPerson, report, this.baseConfiguration);
        // Once we have Matched the people we can match the Families
        masterFile.MatchFamilies(report);
        masterFile.MatchDOD(report);
        masterFile.MatchDOM(report);
        
        if (!this.baseConfiguration.IncludeIgnoredDescendents){
            masterFile.SetIgnoredDescendents(this.baseConfiguration.IgnoreDescendents);
            // Copy Flags for Families
            masterFile.CopyIgnoredFamiliesToLinkedFamilies();
            // Set the ignored descendents in the comparison file.
            comparisonFile.labels = new List<LABL>(masterFile.labels);
            comparisonFile.SetIgnoredDescendents(this.baseConfiguration.IgnoreDescendents);
        }
    }
   public void Report(StringBuilder report)
    {
                    // Now report the appropriate configuration
            ReportConfiguration(this.masterFileConfig.fileName, this.masterFile, this.masterFileConfig.Reporting, report);
            ReportConfiguration(this.comparisonFileConfig.fileName, this.comparisonFile, this.comparisonFileConfig.Reporting, report);

            report.AppendFormat("{0}{0}Processing Complete{0}", Environment.NewLine);

            if (this.baseConfiguration.LogLevel == LogLevel.Trace)
            {
                report.AppendFormat("{0}{0}**** Trace Reporting ****{0}", Environment.NewLine);
                report.Append(report);
                report.AppendFormat("{0}{0}**** End of Trace Reporting ****{0}", Environment.NewLine);
            }

                // Now create the csv files for manipulation
            if (Directory.Exists(Path.GetDirectoryName(this.comparisonFileConfig.Reporting.csvPath)))
            {
                StringBuilder csvPeopleReport = new ();
                StringBuilder csvFamilyReport = new ();
                csvPeopleReport.AppendFormat("Source,Type,Id,GivenNames,Surname,DOB,DOD,MatchId,InTree,Attempted,DODMatch,BloodLine,Changed{0}", Environment.NewLine);
                foreach (INDI person in masterFile.people)
                {
                    csvPeopleReport.AppendFormat("{0},INDI,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},False{11}",
                        this.masterFileConfig.fileName,
                        person.id, person.GivenNames,
                        person.Surname,
                        INDI.stdDate(person.DOB),
                        INDI.stdDate(person.DOD),
                        person.personMatch?.id,
                        person.isIncludedInTree,
                        person.matchAttempted,
                        person.DODMatch,
                        person.isBloodLine,
                        Environment.NewLine);
                }
                foreach (INDI person in comparisonFile.people)
                {
                    csvPeopleReport.AppendFormat("{0},INDI,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},False{11}",
                        this.comparisonFileConfig.fileName,
                        person.id,
                        person.GivenNames,
                        person.Surname,
                        INDI.stdDate(person.DOB),
                        INDI.stdDate(person.DOD),
                        person.personMatch?.id,
                        person.isIncludedInTree,
                        person.matchAttempted,
                        person.DODMatch,
                        person.isBloodLine,
                        Environment.NewLine);
                }
                csvFamilyReport.AppendFormat("Source,Type,Id,H GivenNames,H Surname,H DOB,W GivenNames,W Surname,W DOB,ChildCount,MatchId,DOM,Attempted,DOMMatch,BloodLine,ChildMatch,Changed{0}", Environment.NewLine);
                foreach (FAM family in masterFile.families)
                {
                    csvFamilyReport.AppendFormat("{0},FAM,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},False{15}",
                        this.masterFileConfig.fileName,
                        family.id,
                        family.Husband?.person?.GivenNames ?? "",
                        family.Husband?.person?.Surname ?? "",
                        INDI.stdDate(family.Husband?.person?.DOB ?? ""),
                        family.Wife?.person?.GivenNames ?? "",
                        family.Wife?.person?.Surname ?? "",
                        INDI.stdDate(family.Wife?.person?.DOB ?? ""),
                        family.Children.Count,
                        family.familyMatch?.id,
                        family.DOMarriage,
                        family.matchAttempted,
                        family.DOMarriageMatch,
                        family.isBloodline().ToString(),
                        family.ChildCountMatch().ToString(),
                        Environment.NewLine);
                }
                foreach (FAM family in comparisonFile.families)
                {
                    csvFamilyReport.AppendFormat("{0},FAM,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},False{15}",
                        this.comparisonFileConfig.fileName,
                        family.id,
                        family.Husband?.person?.GivenNames ?? "",
                        family.Husband?.person?.Surname ?? "",
                        INDI.stdDate(family.Husband?.person?.DOB ?? ""),
                        family.Wife?.person?.GivenNames ?? "",
                        family.Wife?.person?.Surname ?? "",
                        INDI.stdDate(family.Wife?.person?.DOB ?? ""),
                        family.Children.Count,
                        family.familyMatch?.id,
                        family.DOMarriage,
                        family.matchAttempted,
                        family.DOMarriageMatch,
                        family.isBloodline().ToString(),
                        family.ChildCountMatch().ToString(),
                        Environment.NewLine);
                }
                // We have now done the comparison
                File.WriteAllText(this.comparisonFileConfig.Reporting.csvPath + "/People.csv", csvPeopleReport.ToString());
                File.WriteAllText(this.comparisonFileConfig.Reporting.csvPath + "/Families.csv", csvFamilyReport.ToString());

            }

            if (Directory.Exists(Path.GetDirectoryName(FullReportPath)))
            {
                // Now we need to write the report out. First check the file does not exist
                report.AppendFormat("Report File Path has been updated ({0}){1}", this.baseConfiguration.FullReport, Environment.NewLine);
                File.WriteAllText(FullReportPath, report.ToString());
            }
            else
            {
                // We have now done the comparison, But the file path was not found. Log to the screen/console
                report.AppendFormat("Report File Path was not found ({0}){1}", FullReportPath, Environment.NewLine);
            }
            Console.WriteLine(report.ToString());
    }
    static void ReportConfiguration(String name, GEDCOMFile file, CONFIGReporting cfg, StringBuilder fullreport)
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
        StringBuilder report = new ();
        report.AppendFormat("***  Reporting Totals for {1}  ***{0}", Environment.NewLine, name);
        report.AppendFormat("{2} Statistics: {0}{1}", file.path, Environment.NewLine, name);
        report.AppendFormat("People Count: {0}{1}", file.people.Count, Environment.NewLine);
        report.AppendFormat("Family Count: {0}{1}{1}", file.families.Count, Environment.NewLine);

        List<FAM> famMatched = file.families.FindAll(x=>x.familyMatch != null); // Number of Families which matched
        List<FAM> famNotMatched = file.families.FindAll(x=>x.familyMatch == null); // Number of Families which did not match
        report.AppendFormat("Families (Matched : {0}, Not Matched : {1}){2}", famMatched.Count, famNotMatched.Count, Environment.NewLine);

        List<FAM> famNotMatchedAttempted = famNotMatched.FindAll(x=>x.matchAttempted); // Number of Families which did not match but we attempted to match
        List<FAM> famNotMatchedNotAttempted = famNotMatched.FindAll(x=>!x.matchAttempted); // Number of Families which did not match and we did not attempt to match
        report.AppendFormat("Families Not Matched (Attempted : {0}, Not Attempted : {1}){2}", famNotMatchedAttempted.Count, famNotMatchedNotAttempted.Count, Environment.NewLine);

        if (cfg.IsMasterFile)
        {
            List<FAM> famNotMatchedAttemptedIncluded = famNotMatchedAttempted.FindAll(x=>!x.ignoreDescendents); // Number of Families which did not match but we attempted to match and are included in the tree
            List<FAM> famNotMatchedAttemptedNotIncluded = famNotMatchedAttempted.FindAll(x=>x.ignoreDescendents); // Number of Families which did not match but we attempted to match and are not included in the tree
            List<FAM> famNotMatchedNotAttemptedIncluded = famNotMatchedNotAttempted.FindAll(x=>!x.ignoreDescendents); // Number of Families which did not match but we attempted to match and are included in the tree
            List<FAM> famNotMatchedNotAttemptedNotIncluded = famNotMatchedNotAttempted.FindAll(x=>x.ignoreDescendents); // Number of Families which did not match but we attempted to match and are not included in the tree
            report.AppendFormat("(MasterFile Only) Families Not Matched, Attempted (Included : {0}, Not Included : {1}){2}", famNotMatchedAttemptedIncluded.Count, famNotMatchedAttemptedNotIncluded.Count, Environment.NewLine);
            report.AppendFormat("(MasterFile Only)Families Not Matched, Not Attempted (Included : {0}, Not Included : {1}){2}", famNotMatchedNotAttemptedIncluded.Count, famNotMatchedNotAttemptedNotIncluded.Count, Environment.NewLine);
        }

        List<FAM> famIncludedMatchedChildrenCountMismatch = famMatched.FindAll(x=>x.Children.Count != x.familyMatch.Children.Count); 
        List<FAM> famIncludedMatchedChildrenCountMatched = famMatched.FindAll(x=>x.Children.Count == x.familyMatch.Children.Count); 
        report.AppendFormat("Families Included, Matched (Child Match : {0}, Child Mismatch : {1}) (reportFamiliesMatchedChildrenCountMismatch) {2}", famIncludedMatchedChildrenCountMatched.Count, famIncludedMatchedChildrenCountMismatch.Count, Environment.NewLine);

        List<INDI> inTree = null;
        List<INDI> notInTree = null;
        inTree = file.people.FindAll(x=>x.isIncludedInTree); // Total People included in the tree
        notInTree = file.people.FindAll(x=>!x.isIncludedInTree); // Total People not included in the tree
        report.AppendFormat("People (InTree : {0}, NotInTree : {1}) (reportPeopleInTree) {2}", inTree.Count, notInTree.Count, Environment.NewLine);

        List<INDI> inTreeMatched = inTree.FindAll(x=>x.personMatch != null); // Total People in the tree and matched
        List<INDI> inTreeNotMatched = inTree.FindAll(x=>x.personMatch == null); // Total People in the tree and not matched
        report.AppendFormat("People inTree (Matched : {0}, Not Matched : {1}){2}", inTreeMatched.Count, inTreeNotMatched.Count, Environment.NewLine);

        List<INDI> inTreeNotMatchedAttempted = inTreeNotMatched.FindAll(x=>x.matchAttempted); // Total People in the tree and matched and we attempted to match
        List<INDI> inTreeNotMatchedNotAttempted = inTreeNotMatched.FindAll(x=>!x.matchAttempted); // Total People in the tree and matched and we did not attempt to match
        report.AppendFormat("People inTree, Not Matched (Attempted : {0}, Not Attempted : {1}){2}", inTreeNotMatchedAttempted.Count, inTreeNotMatchedNotAttempted.Count, Environment.NewLine);

        List<INDI> inTreeNotMatchedNotAttemptedIgnored = inTreeNotMatchedNotAttempted.FindAll(x=>x.isIgnoredDecendent); // Total People in the tree and not matched and we attempted to match
        List<INDI> inTreeNotMatchedNotAttemptedNotIgnored = inTreeNotMatchedNotAttempted.FindAll(x=>!x.isIgnoredDecendent); // Total People in the tree and not matched and we did not attempt to match
        report.AppendFormat("People inTree, Not Matched, Not Attempted (Ignored : {0}, Not Ignored : {1}){2}", inTreeNotMatchedNotAttemptedIgnored.Count, inTreeNotMatchedNotAttemptedNotIgnored.Count, Environment.NewLine);

        List<INDI> inTreeNotMatchedNotAttemptedBloodline = inTreeNotMatchedNotAttempted.FindAll(x=>x.isBloodLine); // Total People in the tree and not matched and we attempted to match
        List<INDI> inTreeNotMatchedNotAttemptedNotBloodline = inTreeNotMatchedNotAttempted.FindAll(x=>!x.isBloodLine); // Total People in the tree and not matched and we did not attempt to match
        report.AppendFormat("People inTree, Not Matched, Not Attempted (BloodLine : {0}, Not BloodLine : {1}){2}", inTreeNotMatchedNotAttemptedBloodline.Count, inTreeNotMatchedNotAttemptedNotBloodline.Count, Environment.NewLine);

        List<INDI> inTreeMatchedDODMatch = inTreeMatched.FindAll(x=>x.DODMatch); // Total People in the tree and not matched and we attempted to match
        List<INDI> inTreeMatchedDODMismatch = inTreeNotMatchedNotAttempted.FindAll(x=>!x.DODMatch); // Total People in the tree and not matched and we did not attempt to match
        report.AppendFormat("People inTree, Matched (DOD Match : {0}, DOD Mismatch : {1}){2}", inTreeMatchedDODMatch.Count, inTreeMatchedDODMismatch.Count, Environment.NewLine);
        // Now list the details, but first list some counts

        report.AppendFormat("{0}{0}***  Generating Reports for {1}  ***{0}", Environment.NewLine, name);

        report.AppendFormat("{0}*** Reporting Completed for {1} ***{0}{0}", Environment.NewLine, name);

        // Write the file out now.
        if (cfg.filename != "" && Directory.Exists(Path.GetDirectoryName(cfg.filename)))
        {
            // We have now done the comparison
            report.AppendFormat("Report File Path has been updated ({0}){1}", cfg.filename, Environment.NewLine);
            // Now we need to write the report out. First check the file does not exist
            File.WriteAllText(cfg.filename, report.ToString());
            // Append this to the full report
            fullreport.Append(report);
        }
        else
        {
            // Append this to the full report
            fullreport.Append(report);
            // We have now done the comparison, But the file path was not found. Log to the screen/console
            if (cfg.filename != "") report.AppendFormat("Report File Path was not found ({0}){1}", cfg.filename, Environment.NewLine);
        }
    }
}
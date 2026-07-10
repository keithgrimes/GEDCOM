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
                report.AppendFormat("{0}{0}****************** Trace Reporting ******************{0}", Environment.NewLine);
                report.Append(report);
                report.AppendFormat("{0}{0}****************** End of Trace Reporting ******************{0}", Environment.NewLine);
            }

            if (Directory.Exists(Path.GetDirectoryName(FullReportPath)))
            {
                // We have now done the comparison
                report.AppendFormat("Report File Path has been updated ({0}){1}", this.baseConfiguration.FullReport, Environment.NewLine);
                // Now we need to write the report out. First check the file does not exist
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

        List<INDI> failedMatches = InTreeIncluded.FindAll(x=>x.matchAttempted && x.personMatch == null);
        List<INDI> noMatchAttempted = InTreeIncluded.FindAll(x=>!x.matchAttempted);
        report.AppendFormat("Number of people in the tree, included, Unmatched (Match Attempted : {0}, Match Not Attempted : {1}){2}", failedMatches.Count, noMatchAttempted.Count, Environment.NewLine);

        List<INDI> DateOfDeathMismatch = InTreeIncludedMatched.FindAll(x=>INDI.stdDate(x.DOD) != INDI.stdDate(x.personMatch.DOD));
        report.AppendFormat("Number of people in the tree, included, Matched but Date of Death does not match ({0}){1}", DateOfDeathMismatch.Count, Environment.NewLine);


        List<INDI> InTreeExcludedMatched = null;
        List<INDI> InTreeExcludedNotMatched = null;
        InTreeExcludedMatched = InTreeExcluded.FindAll(x=>x.personMatch != null ); // Total People in tree, excluded and not matched
        InTreeExcludedNotMatched = InTreeExcluded.FindAll(x=>x.personMatch == null ); // Total People in tree, excluded and not matched
        report.AppendFormat("Number of people in the tree, excluded (matched : {0}, unmatched {1}){2}", InTreeExcludedMatched.Count, InTreeExcludedNotMatched.Count, Environment.NewLine);

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

        if (cfg.reportFailedAttemptedMatches)
        {
            report.AppendFormat("***************  People who are in tree, are included but not matched and we failed an attempted match ({1})*****************{0}{0}", Environment.NewLine, failedMatches.Count);
            foreach(INDI ancestor in failedMatches)report.AppendFormat("{2} - {0} ({1}) Match Attempted but Failed{3}", ancestor.Name, ancestor.DOB, ancestor.id, Environment.NewLine);
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
        if (cfg.reportDateOfDeathMismatch)
        {
            report.AppendFormat("***************  People who are InTree, Matched but Date of Death is not the same ({1})*****************{0}{0}", Environment.NewLine, DateOfDeathMismatch.Count);
            foreach(INDI ancestor in DateOfDeathMismatch)report.AppendFormat("{0} - {1} ({2} vs {3}) Date of Death does not match{4}",ancestor.id, ancestor.Name, INDI.stdDate(ancestor.DOD) ?? "null", INDI.stdDate(ancestor.personMatch.DOD) ?? "null", Environment.NewLine);            
        }
        report.AppendFormat("{0}***************  Reporting Completed for {1}  *****************{0}{0}", Environment.NewLine, name);

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
using System.Text;
using GEDCOM;

namespace GEDCOMTests;

[TestClass]
public sealed class LoadingTests
{
    static string masterFilePath = "/Users/keith/Documents/GEDCOM/Test Family Tree/Test Family Tree.ged";
    //static string comparisonFilePath = "/Users/keith/Documents/GEDCOM/MacFamilyTree/Grimes Family Tree.ged";
    static string comparisonPersonName = "Keith Grimes";

    [TestMethod]
    public void FileStructure()
    {
        // Load the configuration file
        CONFIG appSettings = new();
        appSettings.masterFileName = "/Users/keith/Documents/GEDCOM/Test Family Tree/Test Family Tree.ged";
        appSettings.masterPersonName = "Keith Grimes";

        GEDCOMFile masterFile = new(appSettings.masterFileName);
        // Find the record for the selected person
        INDI masterPerson = masterFile.FindPerson(appSettings.masterPersonName);

        // Define who is actually part of the tree
        masterPerson.SetInTree();

        // Check it has found the master person and that the file counts all match.
        Assert.IsNotNull(masterPerson, "Master Person was not found in the tree"); // Can we find the root person
        Assert.HasCount(467872, masterFile.Records, "Count of Records did not match 467872"); // Have we loaded the correct number of people
        Assert.HasCount(2759, masterFile.people, "Count of People did not match 2759"); // Have we loaded the correct number of people
        Assert.HasCount(854, masterFile.families, "Count of Families did not match 854"); // Have we loaded the correct number of people
        Assert.HasCount(1701, masterFile.sources, "Count of Sources did not match 1701"); // Have we loaded the correct number of people
    }

    [TestMethod]
    public void ConfigurationFile()
    {
        // Load the configuration file
        CONFIG appSettings = new($"appsettings.json");
    }
}

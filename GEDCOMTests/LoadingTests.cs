using System.Text;
using GEDCOM;

namespace GEDCOMTests;

[TestClass]
public sealed class LoadingTests
{
    [TestMethod]
    public void FileStructure()
    {
        // Load the configuration file
        CONFIG appSettings = new($"appsettings.json");

        GEDCOMFile masterFile = new(appSettings.masterConfiguration.filePath);
        // Find the record for the selected person
        INDI masterPerson = masterFile.FindPerson(appSettings.baseConfiguration.MasterPerson);

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

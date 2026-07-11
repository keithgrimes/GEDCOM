using System.Text;
using GEDCOM;

namespace GEDCOMTests;

[TestClass]
public sealed class StandardDateTests
{
    [TestMethod]
    public void DateFormats()
    {
        string tst = INDI.stdDate("abt 1739");

        // Years
        Assert.AreEqual("1739", INDI.stdDate("1739"));
        Assert.AreEqual("1739", INDI.stdDate("abt 1739"));
        Assert.AreEqual("1739", INDI.stdDate("abt. 1739"));
        Assert.AreEqual("1739", INDI.stdDate("circa 1739"));
        Assert.AreEqual("1739", INDI.stdDate("about 1739"));

        // Months
        Assert.AreEqual("March 1739", INDI.stdDate("abt March 1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("abt Mar 1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("mar 1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("03/1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("  03  /  1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("abt. 03/1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("circa 03/1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("about 03/1739"));
        Assert.AreEqual("March 1739", INDI.stdDate("abt 03/1739"));


        // Dates
        Assert.AreEqual("3 March 1739", INDI.stdDate("3 Mar 1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("3 March 1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("abt 3 Mar 1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("abt 3 March 1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("abt 3/3/1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("3/3/1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("03/03/1739"));
        Assert.AreEqual("3 March 1739", INDI.stdDate("3/03/1739"));

    }

}

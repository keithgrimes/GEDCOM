namespace GEDCOM
{
    public class SUBM : EntryList
    {
        public string id { get; }
        public SUBM(string line) : base(line)
        { }
    }
}

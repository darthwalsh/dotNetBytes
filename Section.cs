using System.Linq;
using System.Collections.Generic;

//TODO move?
public class SectionConversion
{
    int relativeAddress;
    int rawaddress;
    int length;

    static List<SectionConversion> sections = new List<SectionConversion>();

    public static void Add(int relativeAddress, int length, int rawaddress)
    {
        sections.Add(new SectionConversion
        {
            relativeAddress = relativeAddress,
            length = length,
            rawaddress = rawaddress,
        });
    }

    public static int GetRawAddress(int rel)
    {
        var section = sections.Where(s => s.relativeAddress <= rel && rel <= s.relativeAddress + s.length).Single();
        return section.rawaddress + rel - section.rawaddress;
    }
}

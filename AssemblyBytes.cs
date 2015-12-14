using System.IO;
using System.Web.Script.Serialization;

class AssemblyBytes
{
    FileFormat FileFormat;

    CodeNode node;

    public AssemblyBytes(string path)
    {
        Stream s = File.OpenRead(path);

        int start = (int)s.Position;

        FileFormat = s.ReadClass<FileFormat>();

        node = new CodeNode
        {
            Name = "Root",
            Description = "",
            Value = "",

            Start = start,
            End = (int)s.Position,
        };

        FileFormat.VisitFields(start, node);

        System.Console.Error.WriteLine(node.ToString());
    }

    public string AsJson
    {
        get
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Serialize(node);
        }
    }
}

using System.IO;
using System.Web.Script.Serialization;

class AssemblyBytes
{
    FileFormat FileFormat;

    CodeNode node;

    public AssemblyBytes(string path)
    {
        Stream s = File.OpenRead(path);

        node = s.ReadClass(ref FileFormat);

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

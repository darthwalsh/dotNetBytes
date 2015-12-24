using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web.Script.Serialization;

class AssemblyBytes
{
    FileFormat FileFormat;

    CodeNode node;

    //TODO test with a forward-only stream
    public AssemblyBytes(string path)
    {
        Stream s = File.OpenRead(path);

        node = s.ReadClass(ref FileFormat);

        node.Widen();

        System.Console.Error.WriteLine(node.ToString());
    }

    public string AsJson
    {
        get
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new[] { new CodeNodeConverter() });
            return serializer.Serialize(node);
        }
    }

    class CodeNodeConverter : JavaScriptConverter
    {
        public override IEnumerable<Type> SupportedTypes { get { return new[] { typeof(CodeNode) }; } }

        public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            return typeof(CodeNode).GetFields().ToDictionary(field => field.Name, field => field.GetValue(obj));
        }
    }
}

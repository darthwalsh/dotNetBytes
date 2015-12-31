using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web.Script.Serialization;

class CodeNodeConverter : JavaScriptConverter
{
    public static string ToJson(CodeNode node)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.RegisterConverters(new[] { new CodeNodeConverter() });
        return serializer.Serialize(node);
    }

    CodeNodeConverter() { }

    public override IEnumerable<Type> SupportedTypes => new[] { typeof(CodeNode) };

    public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
    {
        return typeof(CodeNode).GetFields().ToDictionary(field => field.Name, field => field.GetValue(obj));
    }
}


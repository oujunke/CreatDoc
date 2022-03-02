// See https://aka.ms/new-console-template for more information
using System.Reflection;
using System.Xml;

var dic = @"D:\code\dev\QuoteCore\Boying.TaiPingCrawl\Boying.TaiPingCrawl.InstallDirectory\";
var name = "Boying.InsuranceCrawl.IService";
var assembly = Assembly.LoadFile($"{dic}{name}.dll");
var type = assembly.GetType("Boying.InsuranceCrawl.IService.PolicyHistory.IPolicyDetailsService");
XmlDocument doc = new XmlDocument();
doc.Load($"{dic}{name}.xml");
var node = doc.ChildNodes[1].ChildNodes[1];
Dictionary<string, string> data = new Dictionary<string, string>();
foreach (XmlNode item in node.ChildNodes)
{
    if (item.Attributes == null || item.Attributes.Count != 1)
    {
        continue;
    }
    var name2 = item.Attributes[0].Value;
    var value = item.InnerText.Trim('\r', '\n').Trim();
    if (data.ContainsKey(name2))
    {
        data[name2] = value;
    }
    else
    {
        data.Add(name2, value);
    }
}
List<DocMethod> docMethods = new List<DocMethod>();
List<Type> exceptType = new List<Type> { typeof(string) };
Dictionary<string, string> typeData = new Dictionary<string, string>();
foreach (var methodInfo in type.GetMethods())
{
    var docMethod = new DocMethod();
    docMethods.Add(docMethod);
    docMethod.Name = methodInfo.Name;
    foreach (var parameterInfo in methodInfo.GetParameters())
    {
        DocType par = new DocType();
        par.Name = parameterInfo.Name;
        par.Type = GetName(parameterInfo.ParameterType);
        docMethod.ParameterList.Add(par);
        foreach (var property in parameterInfo.ParameterType.GetProperties())
        {
            AddProperty(par, property);
            Console.WriteLine($"{property.PropertyType.Name}\t\t\t{property.Name}");
        }
    }
    docMethod.ReturnParameter = new DocType
    {
        Name = methodInfo.ReturnParameter.Name,
        Type = GetName(methodInfo.ReturnParameter.ParameterType),
    };
    foreach (var property in methodInfo.ReturnParameter.ParameterType.GetProperties())
    {
        AddProperty(docMethod.ReturnParameter, property);
        Console.WriteLine($"{property.PropertyType.Name}\t\t\t{property.Name}");
    }
    System.Text.StringBuilder builder = new System.Text.StringBuilder();
    foreach (var method in docMethods)
    {
        builder.AppendLine($"接口:{method.Name}");
        builder.AppendLine($"请求参数:");
        foreach (var par in method.ParameterList)
        {
            builder.AppendLine($"{par.Type}\t{par.Name}\t{par.Dec}");
        }
        builder.AppendLine($"返回参数:");
        builder.AppendLine($"{method.ReturnParameter.Type}\t{method.ReturnParameter.Name}\t{method.ReturnParameter.Dec}");
        foreach (var par in method.ParameterList)
        {
            AddType(par, builder);
        }
        AddType(method.ReturnParameter, builder);
    }

    var dataString = builder.ToString();
}
void AddType(DocType type, System.Text.StringBuilder builder)
{
    if (type.DocTypes.Count <= 0||typeData.ContainsKey(type.Type))
    {
        return;
    }
    typeData.Add(type.Type,"1");
    builder.AppendLine($"类型:{type.Type}");
    builder.AppendLine("类型\t名称\t注释");
    foreach (var par in type.DocTypes)
    {
        builder.AppendLine($"{par.Type}\t{par.Name}\t{par.Dec}");
    }
    foreach (var par in type.DocTypes)
    {
        AddType(par, builder);
    }
}
string GetName(Type type)
{
    var name = type.Name;
    if (type.GenericTypeArguments.Length > 0)
    {
        if (name == "Nullable`1")
        {
            name = type.GenericTypeArguments[0].Name + "?";
        }
        else if (name == "List`1")
        {
            name = $"List<{type.GenericTypeArguments[0].Name}>";
        }
    }
    return name;
}

void AddProperty(DocType par, PropertyInfo property)
{
    DocType par2 = new DocType();
    par.DocTypes.Add(par2);
    par2.Name = property.Name;
    par2.Type = GetName(property.PropertyType);
    var key = $"P:{property.DeclaringType.FullName}.{property.Name}";
    if (data.ContainsKey(key))
    {
        par2.Dec = data[key].Replace("\r\n", ";").Replace(" ","");
    }
    if (property.PropertyType.BaseType == typeof(object) && !exceptType.Contains(property.PropertyType))
    {
        if (property.PropertyType.GenericTypeArguments.Length > 0)
        {
            foreach (var type1 in property.PropertyType.GenericTypeArguments)
            {
                foreach (var item in type1.GetProperties())
                {
                    AddProperty(par2, item);
                }
            }
        }
        else
        {
            foreach (var item in property.PropertyType.GetProperties())
            {
                AddProperty(par2, item);
            }
        }
    }
    else if (property.PropertyType.BaseType == typeof(Enum))
    {
        var fs = property.PropertyType.GetFields();
        for (int i = 1; i < fs.Length; i++)
        {
            var item = fs[i];
            var key2 = $"F:{item.DeclaringType.FullName}.{item.Name}";
            if (data.ContainsKey(key))
            {
                par2.Dec = data[key].Replace("\r\n",";").Replace(" ", "");
            }
            par2.DocTypes.Add(new DocType
            {
                Name = item.Name,
                Type = ((int)item.GetValue(null)).ToString(),
            });
        }
    }
}

class DocType
{
    public string Name;
    public string Type;
    public string Dec;
    public List<DocType> DocTypes = new List<DocType>();
    public override string ToString()
    {
        return $"{Type}\t{Name}\t{Dec}";
    }
}
class DocMethod
{
    public string Name;
    public List<DocType> ParameterList = new List<DocType>();
    public DocType ReturnParameter;
}

using System.Reflection;
using System.IO;
using DLH.Controls.Wpf;

namespace DLH.Controls.Wpf.AutomatedTests;

[TestClass]
[TestCategory("API")]
public sealed class PublicApiApprovalTests
{
    [TestMethod]
    public void PublicApiMatchesApprovedContract()
    {
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "PublicApi", "DLH.Controls.Wpf.txt");
        var actual = DescribeAssembly(typeof(DataGridView).Assembly);
        if (Environment.GetEnvironmentVariable("UPDATE_PUBLIC_API") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.WriteAllText(expectedPath, actual);
            return;
        }
        Assert.IsTrue(File.Exists(expectedPath), "Contrato da API pública ausente. Execute com UPDATE_PUBLIC_API=1 para criá-lo.");
        Assert.AreEqual(File.ReadAllText(expectedPath).ReplaceLineEndings("\n"), actual.ReplaceLineEndings("\n"),
            "A API pública mudou. Revise a compatibilidade e atualize o contrato somente quando a mudança for intencional.");
    }

    private static string DescribeAssembly(Assembly assembly)
    {
        var lines = new List<string>();
        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add($"type {TypeName(type)}");
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                         .Where(member => member is not MethodInfo { IsSpecialName: true })
                         .Select(DescribeMember).Where(text => text is not null).OrderBy(text => text, StringComparer.Ordinal))
                lines.Add("  " + member);
        }
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string? DescribeMember(MemberInfo member) => member switch
    {
        ConstructorInfo constructor => $"ctor({Parameters(constructor.GetParameters())})",
        MethodInfo method => $"method {TypeName(method.ReturnType)} {method.Name}({Parameters(method.GetParameters())})",
        PropertyInfo property => $"property {TypeName(property.PropertyType)} {property.Name} {{ {(property.CanRead ? "get; " : "")}{(property.CanWrite ? "set; " : "")} }}",
        EventInfo @event => $"event {TypeName(@event.EventHandlerType!)} {@event.Name}",
        FieldInfo field => $"field {TypeName(field.FieldType)} {field.Name}",
        _ => null
    };

    private static string Parameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(parameter => TypeName(parameter.ParameterType)));

    private static string TypeName(Type type)
    {
        if (type.IsByRef) return TypeName(type.GetElementType()!) + "&";
        if (type.IsArray) return TypeName(type.GetElementType()!) + "[]";
        if (!type.IsGenericType) return type.FullName ?? type.Name;
        var name = (type.GetGenericTypeDefinition().FullName ?? type.Name).Split('`')[0];
        return $"{name}<{string.Join(",", type.GetGenericArguments().Select(TypeName))}>";
    }
}

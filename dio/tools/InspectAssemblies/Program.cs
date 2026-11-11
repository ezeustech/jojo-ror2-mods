using Mono.Cecil;
using var assembly = AssemblyDefinition.ReadAssembly(args[0]);
Console.WriteLine(assembly.FullName);
Console.WriteLine("Runtime: " + assembly.MainModule.RuntimeVersion);
foreach (var reference in assembly.MainModule.AssemblyReferences) Console.WriteLine("REF " + reference);
var filters = args.Skip(1).ToArray();
IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types) => types.SelectMany(t => new[] { t }.Concat(AllTypes(t.NestedTypes)));
foreach (var type in AllTypes(assembly.MainModule.Types).Where(t => filters.Any(f => t.FullName.Contains(f))))
{
    Console.WriteLine("TYPE " + type.FullName + " : " + type.BaseType);
    foreach (var field in type.Fields) Console.WriteLine("  FIELD " + field.Attributes + " " + field.FieldType + " " + field.Name);
    foreach (var method in type.Methods) {
        Console.WriteLine("  METHOD " + method.Attributes + " " + method.FullName);
        if (args.Contains("--il") && method.HasBody)
            foreach (var instruction in method.Body.Instructions) Console.WriteLine("    " + instruction);
    }
}

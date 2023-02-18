// Note: These tools are copied from my personal code reuse libraries - Ross Wright

using System.Reflection;

namespace RossWright;

public static class AssemblyFinderExtensions
{
    /// <summary>
    /// Finds local assemlbies with the specified prefixes in the base directory of the app domain
    /// This method IS NOT suitable for Blazor WASM projects
    /// </summary>
    /// <param name="prefixes">list of prefixes for assembly matching, only assemblies with a filename starting with a provided prefix will be included in the results</param>
    /// <param name="verbose">When true outputs the found assemblies to the console</param>
    public static Assembly[] LoadLocalAssemblies(this AppDomain appDomain, string[] prefixes, bool verbose = true)
    {
        var asmFiles = prefixes.SelectMany(prefix =>
            Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, $"{prefix}.*.dll"));
        Console.WriteLine($"Found {asmFiles.Count()} assemblies:");
        List<Assembly> assemblies = new List<Assembly>();
        foreach (var asmFile in asmFiles.OrderBy(_ => _))
        {
            Console.Write($"\t{asmFile}... ");
            try
            {
                assemblies.Add(Assembly.Load(AssemblyName.GetAssemblyName(asmFile)));
                Console.WriteLine($"Loaded");
            }
            catch
            {
                Console.WriteLine($"FAILED");
            }
        }
        return assemblies.ToArray();
    }

    /// <summary>
    /// Finds local assemlbies referenced directly or indirectly by the app domain
    /// This method IS suitable for Blazor WASM projects
    /// </summary>
    /// <param name="prefixes">list of prefixes for assembly matching, only assemblies with a filename starting with a provided prefix will be included in the results</param>
    /// <param name="verbose">When true outputs the found assemblies to the console</param>
    public static Assembly[] LoadReferencedAssemblies(this AppDomain appDomain, string[] prefixes, bool verbose = true)
    {
        var loadedAssemblies = appDomain.GetAssemblies()
            .Where(_ => prefixes.Any(prefix => _.FullName?.StartsWith($"{prefix}.") ?? false))
            .ToDictionary(_ => _.FullName!, _ => _);
        if (verbose)
        {
            Console.WriteLine($"Found {loadedAssemblies.Count()} pre-loaded assemblies:");
            foreach (var assembly in loadedAssemblies.Values.OrderBy(_ => _.FullName))
                Console.WriteLine($"\t{assembly.FullName}");
        }

        var referencedAssemblies = loadedAssemblies.Values
            .SelectMany(_ => _.GetReferencedAssemblies())
            .Where(_ => prefixes.Any(prefix => _.FullName.StartsWith($"{prefix}.")) &&
                !loadedAssemblies.ContainsKey(_.FullName))
            .Distinct(_ => _?.FullName)
            .Select(Assembly.Load)
            .ToArray();
        if (verbose)
        {
            Console.WriteLine($"Found {referencedAssemblies.Count()} more referenced assemblies that are now loaded:");
            foreach (var assembly in referencedAssemblies.OrderBy(_ => _.FullName))
                Console.WriteLine($"\t{assembly.FullName}");
        }
        return loadedAssemblies.Values.Concat(referencedAssemblies).ToArray();
    }
}

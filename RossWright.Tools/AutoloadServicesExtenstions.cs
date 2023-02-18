using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace RossWright;

public static class AutoloadServicesExtenstions
{
    /// <summary>
    /// Discovers and registers classes implementing ISingleton<>, IScopedService<> and ITransientService<> in the service collection
    /// </summary>
    /// <param name="assemblies">The assenmblies to search for valid services</param>
    /// <param name="ignore">Concrete types to ignore as possible service implementations</param>
    /// <param name="entryAssembly">An alternate entry assembly to be used for precidence decisions uses Assembly.GetEntryAssembly if null</param>
    /// <param name="allowMultiple">Allow multiple services of the specified registration types</param>
    /// <param name="verbose">When true outputs the found services to the console</param>
    public static void AutoloadServices(this IServiceCollection services, Assembly[] assemblies, Type[]? ignore = null, Assembly? entryAssembly = null, Type[]? allowMultiple = null, bool verbose = true)
    {
        if (verbose) Console.WriteLine($"- Autoloading Services -");
        var foundServices = assemblies
            .SelectMany(s => s.GetTypes())
            .Where(p => !p.IsAbstract && p.GetInterfaces().Any(i => i.IsGenericType &&
                (typeof(ISingleton<>).IsAssignableFrom(i.GetGenericTypeDefinition())
                || typeof(IScopedService<>).IsAssignableFrom(i.GetGenericTypeDefinition())
                || typeof(ITransientService<>).IsAssignableFrom(i.GetGenericTypeDefinition()))))
            .ToArray();
        if (verbose) Console.WriteLine($"Found {foundServices.Length} services:");

        var candidates = new Dictionary<Type, List<(Type, Type)>>();

        var entryAsmName = entryAssembly?.FullName ?? Assembly.GetEntryAssembly()?.FullName;
        if (verbose) Console.WriteLine($"Entry Asm Name: {entryAsmName}");

        foreach (var serviceType in foundServices)
        {
            if (ignore?.Contains(serviceType) == true)
            {
                if (verbose) Console.WriteLine($"\t Ignored {serviceType} as it was found in the ");
                continue;
            }

            var serviceInterfaceTypes = serviceType.GetInterfaces()
                .Where(i => i.IsGenericType &&
                    (typeof(ISingleton<>).IsAssignableFrom(i.GetGenericTypeDefinition())
                    || typeof(IScopedService<>).IsAssignableFrom(i.GetGenericTypeDefinition())
                    || typeof(ITransientService<>).IsAssignableFrom(i.GetGenericTypeDefinition())))
                .ToList();

            foreach (var serviceInterfaceType in serviceInterfaceTypes)
            {
                var interfaceType = serviceInterfaceType.GetGenericArguments().First();

                if (!interfaceType.IsAssignableFrom(serviceType))
                {
                    if (verbose) Console.WriteLine($"\tERROR: Cannot register service {serviceType} as it does not implement {interfaceType}");
                }
                else if (allowMultiple?.Contains(interfaceType) == true)
                {
                    if (candidates.TryGetValue(interfaceType, out var existingCandidates))
                    {
                        existingCandidates.Add((serviceInterfaceType, serviceType));
                    }
                    else
                    {
                        candidates.Add(interfaceType, new List<(Type, Type)> { (serviceInterfaceType, serviceType) });
                    }
                }
                else if (candidates.ContainsKey(interfaceType))
                {
                    // service fight! determine who wins
                    var existingCandidate = candidates[interfaceType][0];
                    var existingServiceType = existingCandidate.Item2;
                    var existingImplAsmName = existingCandidate.Item2.Assembly.FullName;
                    if (existingImplAsmName == entryAsmName)
                    {
                        if (serviceType.Assembly.FullName == entryAsmName)
                        {
                            if (verbose) Console.WriteLine($"\tERROR: Failed to register {serviceType} because a service, {existingServiceType}, was found that already implements {interfaceType}. Both services have precedence since both are defined in the entry assembly.");
                        }
                        else
                        {
                            if (verbose) Console.WriteLine($"\tWARN: Skipped registering {serviceType} because a service, {existingServiceType}, was found that also implements {interfaceType}, and takes precendence since it is defined in the entry assembly.");
                        }
                    }
                    else if (serviceType.Assembly.FullName == entryAsmName)
                    {
                        if (verbose) Console.WriteLine($"\tWARN: Skipped registering {existingServiceType} because a service, {serviceType}, was found that also implements {interfaceType}, and takes precendence since it is defined in the entry assembly.");
                        candidates[interfaceType] = new List<(Type, Type)> { (serviceInterfaceType, serviceType) };
                    }
                    else
                    {
                        if (verbose) Console.WriteLine($"\tERROR: Failed to register {serviceType} because a service, {existingServiceType}, was found that already implements {interfaceType}. Neither services has precedence since neither are defined in the entry assembly.");
                    }
                }
                else
                {
                    candidates.Add(interfaceType, new List<(Type, Type)> { (serviceInterfaceType, serviceType) });
                }
            }
        }

        var singletonTypes = new Dictionary<Type, Type>();

        foreach (var (interfaceType, registrations) in candidates)
            foreach( var (serviceInterfaceType, serviceType) in registrations)
            {
                if (typeof(ISingleton<>).IsAssignableFrom(serviceInterfaceType.GetGenericTypeDefinition()))
                {
                    if (singletonTypes.ContainsKey(serviceType))
                    {
                        services.AddSingleton(interfaceType, sp => sp.GetRequiredService(singletonTypes[serviceType]));
                    }
                    else
                    {
                        singletonTypes.Add(serviceType, interfaceType);
                        services.AddSingleton(interfaceType, serviceType);
                    }
                    if (verbose) Console.WriteLine($"\tRegistered singleton service {serviceType} for {interfaceType}");
                }
                else if (typeof(IScopedService<>).IsAssignableFrom(serviceInterfaceType.GetGenericTypeDefinition()))
                {
                    services.AddScoped(interfaceType, serviceType);
                    if (verbose) Console.WriteLine($"\tRegistered scoped service {serviceType} for {interfaceType}");
                }
                else //if (typeof(ITransientService<>).IsAssignableFrom(serviceInterfaceType.GetGenericTypeDefinition()))
                {
                    services.AddTransient(interfaceType, serviceType);
                    if (verbose) Console.WriteLine($"\tRegistered transient service {serviceType} for {interfaceType}");
                }
            }
    }
}
public interface ISingleton<T> { }
public interface IScopedService<T> { }
public interface ITransientService<T> { }
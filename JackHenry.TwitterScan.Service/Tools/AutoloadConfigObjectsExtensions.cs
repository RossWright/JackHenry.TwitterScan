// Note: These tools are copied from my personal code reuse libraries - Ross Wright

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace RossWright;

public static class AutoloadConfigObjectsExtensions
{
    /// <summary>
    /// Automatically binds and injects configuration information to objects with the ConfigSection attribute found in the specified assemlies
    /// Note that config section classes implementing IValidatingConfigSection will have their ValidateOrDie method invoked during autoload
    /// </summary>
    /// <param name="config">The configuration repository for the app</param>
    /// <param name="assemblies">The assemblies to search for ConfigSection attributes</param>
    /// /// <param name="verbose">When true outputs the found configuration classes to the console</param>
    public static void AutoloadConfigObjects(this IServiceCollection services, IConfiguration config, Assembly[] assemblies, bool verbose = true)
    {
        if (verbose) Console.WriteLine($"- Autoloading Config Objects -");

        var foundConfigTypes = assemblies
            .SelectMany(s => s.GetTypes())
            .Where(type => type.GetCustomAttributes(typeof(ConfigSectionAttribute), true).Length > 0)
            .ToArray();
        if (verbose) Console.WriteLine($"Found {foundConfigTypes.Length} Config Objects:");
        foreach (var configType in foundConfigTypes)
        {
            var sectionAttributes = configType.GetCustomAttributes(typeof(ConfigSectionAttribute), false);
            try
            {
                var instance = Activator.CreateInstance(configType)!;

                bool hasRegisteredConcrete = false;
                foreach (ConfigSectionAttribute sectionAttribute in sectionAttributes)
                {
                    config.Bind(sectionAttribute.SectionTitle, instance);
                    if (instance is IValidatingConfigSection valCfg)
                        valCfg.ValidateOrDie();

                    if (sectionAttribute.RegisterAs is not null)
                        services.AddSingleton(sectionAttribute.RegisterAs, instance);
                    else if (!hasRegisteredConcrete)
                    {
                        services.AddSingleton(instance.GetType(), instance);
                        hasRegisteredConcrete = true;
                    }
                    var sectionTitles = sectionAttributes.Select(_ => ((ConfigSectionAttribute)_).SectionTitle).ToArray();
                    var sectionRegisterTypes = sectionAttributes.Select(_ => ((ConfigSectionAttribute)_).RegisterAs).Where(_ => _ is not null).ToList();
                    if (hasRegisteredConcrete) sectionRegisterTypes.Add(instance.GetType());
                    if (verbose) Console.WriteLine($"\tBound type {configType} to section(s) {string.Join(',', sectionTitles)} and registered as {string.Join(',', sectionRegisterTypes.Select(_ => _!.Name))}");
                }
            }
            catch (Exception ex)
            {
                if (verbose) Console.WriteLine($"\tFailed to bind type {configType}: {ex.ToBetterString()}");
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ConfigSectionAttribute : Attribute
{
    /// <summary>
    /// Specifies that an instance of this class should be injected as a singleton and bound the specific section of the app configuration
    /// </summary>
    /// <param name="sectionTitle">The configuration section to be bound to an instance of this class</param>
    /// <param name="registerAs">The type used for dependency injection if different than the decorated type</param>
    public ConfigSectionAttribute(string sectionTitle, Type? registerAs = null)
    {
        SectionTitle = sectionTitle;
        RegisterAs = registerAs;
    }
    public string SectionTitle { get; private set; }
    public Type? RegisterAs { get; private set; }
}

/// <summary>
/// Specifies this configuration section object should be validated after binding on autoload
/// </summary>
public interface IValidatingConfigSection
{
    /// <summary>
    /// Validate the config data bound to the object.
    /// This method may throw an appropriate exception which will prevent the config section from being injected
    /// </summary>
    void ValidateOrDie();
}

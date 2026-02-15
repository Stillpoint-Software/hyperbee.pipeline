using System.Collections;
using System.Reflection;

namespace Hyperbee.Pipeline.Commands;

/// <summary>
/// Scans assemblies for concrete classes implementing <see cref="ICommand"/> interfaces
/// (ICommandProcedure&lt;T&gt;, ICommandFunction&lt;T,R&gt;, ICommandFunction&lt;R&gt;).
/// </summary>
public class CommandScanner : IEnumerable<CommandScanner.CommandScanResult>
{
    private readonly IEnumerable<Type> _types;

    /// <summary>
    /// Creates a scanner that works on a sequence of types.
    /// </summary>
    public CommandScanner(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types;
    }

    /// <summary>
    /// Finds all command types in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="includeInternalTypes">Whether to include internal types. The default is false.</param>
    public static CommandScanner FindCommandsInAssembly(Assembly assembly, bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return new CommandScanner(includeInternalTypes ? GetExportedAndInternalTypes(assembly) : assembly.GetExportedTypes());
    }

    /// <summary>
    /// Finds all command types in the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="includeInternalTypes">Whether to include internal types. The default is false.</param>
    public static CommandScanner FindCommandsInAssemblies(IEnumerable<Assembly> assemblies, bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        var types = assemblies
            .SelectMany(x => includeInternalTypes ? GetExportedAndInternalTypes(x) : x.GetExportedTypes())
            .Distinct();

        return new CommandScanner(types);
    }

    /// <summary>
    /// Finds all command types in the assembly containing the specified type.
    /// </summary>
    public static CommandScanner FindCommandsInAssemblyContaining<T>(bool includeInternalTypes = false)
    {
        return FindCommandsInAssembly(typeof(T).Assembly, includeInternalTypes);
    }

    /// <summary>
    /// Finds all command types in the assembly containing the specified type.
    /// </summary>
    public static CommandScanner FindCommandsInAssemblyContaining(Type type, bool includeInternalTypes = false)
    {
        ArgumentNullException.ThrowIfNull(type);
        return FindCommandsInAssembly(type.Assembly, includeInternalTypes);
    }

    private static Type[] GetExportedAndInternalTypes(Assembly assembly)
    {
        // GetTypes() returns everything including private/protected nested types.
        // Filter to only public and internal types.
        return assembly.GetTypes()
            .Where(t => !t.IsNestedPrivate && !t.IsNestedFamily && !t.IsNestedFamANDAssem)
            .ToArray();
    }

    private IEnumerable<CommandScanResult> Execute()
    {
        var commandInterfaceType = typeof(ICommand);

        return _types
            .Where(type => type.IsClass && !type.IsAbstract)
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i != commandInterfaceType && commandInterfaceType.IsAssignableFrom(i))
                .Select(commandInterface => new CommandScanResult(commandInterface, type)));
    }

    /// <summary>
    /// Performs the specified action on all scan results.
    /// </summary>
    public void ForEach(Action<CommandScanResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        foreach (var result in this)
        {
            action(result);
        }
    }

    public IEnumerator<CommandScanResult> GetEnumerator() => Execute().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Result of scanning an assembly for command types.
    /// </summary>
    public class CommandScanResult
    {
        public CommandScanResult(Type interfaceType, Type commandType)
        {
            InterfaceType = interfaceType;
            CommandType = commandType;
        }

        /// <summary>
        /// The ICommand-derived interface type (e.g. ICommandFunction&lt;string, int&gt;).
        /// </summary>
        public Type InterfaceType { get; }

        /// <summary>
        /// The concrete command type that implements the interface (e.g. StringToIntCommand).
        /// </summary>
        public Type CommandType { get; }
    }
}

using System.Reflection;
using AeroTech.Framework.Core.Domain.Aggregates;
using AeroTech.Framework.Core.Domain.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;

namespace AeroTech.Ordering.Domain.ConformanceTests;

/// <summary>
/// Thin Stage-1 guardrail.
///
/// It asserts three things only: the exact Stage-1 domain type set, the exact
/// Stage-1 value-object set, and the numeric values of the enums Stage 1 freezes.
///
/// It deliberately does NOT assert namespaces, assemblies, folders, table or column
/// names, EF mapping strategy, lengths, precision or indexes. Those are repository
/// decisions. Whether Stage 1 is actually finished is decided by the Pack's section 24
/// acceptance behavior, not by this file.
/// </summary>
public sealed class Stage1TypeSetConformanceTests
{
    private readonly Manifest _manifest = LoadManifest();
    private readonly int _stage = LoadStage();

    [Fact]
    public void Stage0_harness_proves_itself()
    {
        Assert.Equal("4.6", _manifest.Version);
        Assert.NotEmpty(_manifest.Stage1.Types);
        Assert.NotEmpty(_manifest.Stage1.ValueObjects);
        Assert.NotEmpty(_manifest.Stage1.Enums);

        // The domain assembly must at least load before any production work starts.
        var loaderErrors = new List<Exception>();
        try
        {
            _ = DomainAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            loaderErrors.AddRange(ex.LoaderExceptions.Where(x => x is not null)!);
        }

        Assert.Empty(loaderErrors);
    }

    [Fact]
    public void Stage1_domain_type_set_is_exact()
    {
        if (_stage < 1) return;

        var expectedConcrete = _manifest.Stage1.Types
            .Except(_manifest.Stage1.AbstractTypes, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var actualConcrete = DomainModelTypes()
            .Where(t => !t.IsAbstract)
            .Select(t => t.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var missing = expectedConcrete.Except(actualConcrete, StringComparer.Ordinal).ToArray();
        var extra = actualConcrete.Except(expectedConcrete, StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"STAGE1_TYPE_SET_MISMATCH::missing=[{string.Join(",", missing)}]::extra=[{string.Join(",", extra)}]");

        foreach (var abstractName in _manifest.Stage1.AbstractTypes)
        {
            var type = FindDomainType(abstractName);
            Assert.True(type is not null, $"STAGE1_ABSTRACT_TYPE_MISSING::{abstractName}");
            Assert.True(type!.IsAbstract, $"STAGE1_TYPE_NOT_ABSTRACT::{abstractName}");
        }

        if (string.IsNullOrWhiteSpace(_manifest.Stage1.ServiceBase)) return;

        var serviceBase = FindDomainType(_manifest.Stage1.ServiceBase);
        Assert.True(serviceBase is not null, $"STAGE1_TYPE_MISSING::{_manifest.Stage1.ServiceBase}");

        foreach (var subtypeName in _manifest.Stage1.ServiceSubtypes)
        {
            var subtype = FindDomainType(subtypeName);
            Assert.True(subtype is not null, $"STAGE1_TYPE_MISSING::{subtypeName}");
            Assert.True(
                serviceBase!.IsAssignableFrom(subtype),
                $"STAGE1_SERVICE_SUBTYPE_NOT_DERIVED::{subtypeName}::expectedBase={_manifest.Stage1.ServiceBase}");
        }
    }

    [Fact]
    public void Stage1_value_object_set_is_exact()
    {
        if (_stage < 1) return;

        var expected = _manifest.Stage1.ValueObjects
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var valueObjectBase = DomainAssemblyTypes()
            .Concat(FrameworkTypes())
            .FirstOrDefault(t => t.IsClass && t.IsAbstract && t.Name == "ValueObject");

        if (valueObjectBase is null)
        {
            // The repository does not use a shared value-object base: only check presence.
            var absent = expected.Where(n => FindDomainType(n) is null).ToArray();
            Assert.True(absent.Length == 0, $"STAGE1_VALUE_OBJECT_MISSING::[{string.Join(",", absent)}]");
            return;
        }

        var actual = DomainAssemblyTypes()
            .Where(t => t.IsClass && !t.IsAbstract && valueObjectBase.IsAssignableFrom(t))
            .Select(t => t.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var missing = expected.Except(actual, StringComparer.Ordinal).ToArray();
        var extra = actual.Except(expected, StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"STAGE1_VALUE_OBJECT_SET_MISMATCH::missing=[{string.Join(",", missing)}]::extra=[{string.Join(",", extra)}]");
    }

    [Fact]
    public void Stage1_frozen_enum_values_are_exact()
    {
        if (_stage < 1) return;

        foreach (var (enumName, expectedMembers) in _manifest.Stage1.Enums)
        {
            var type = FindEnum(enumName);
            Assert.True(type is not null, $"STAGE1_ENUM_MISSING::{enumName}");

            var actual = Enum.GetNames(type!)
                .ToDictionary(name => name, name => Convert.ToInt64(Enum.Parse(type!, name)));

            Assert.True(
                expectedMembers.OrderBy(x => x.Key, StringComparer.Ordinal)
                    .SequenceEqual(actual.OrderBy(x => x.Key, StringComparer.Ordinal)),
                $"STAGE1_ENUM_MISMATCH::{enumName}::expected=[{Render(expectedMembers)}]::actual=[{Render(actual)}]");
        }
    }

    private static string Render(IDictionary<string, long> members)
        => string.Join(",", members.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}={x.Value}"));

    /// <summary>Concrete and abstract domain model types, wherever they live.</summary>
    private static IEnumerable<Type> DomainModelTypes()
        => DomainAssemblyTypes()
            .Where(t => t.IsClass)
            .Where(t => DerivesFromGeneric(t, typeof(Entity<>)) || DerivesFromGeneric(t, typeof(AggregateRoot<>)));

    private static Type? FindDomainType(string simpleName)
    {
        var matches = DomainAssemblyTypes()
            .Where(t => t.IsClass && string.Equals(t.Name, simpleName, StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        Assert.True(matches.Length <= 1,
            $"AMBIGUOUS_DOMAIN_TYPE::{simpleName}::candidates=[{string.Join(",", matches.Select(m => m.FullName))}]");

        return matches.SingleOrDefault();
    }

    /// <summary>
    /// Resolves an enum by simple name across the domain and the shared contracts assembly.
    /// The solution legitimately contains same-named enums in different areas, so ambiguity
    /// is reported rather than silently resolved.
    /// </summary>
    private static Type? FindEnum(string simpleName)
    {
        var matches = DomainAssemblyTypes()
            .Concat(ContractsAssemblyTypes())
            .Where(t => t.IsEnum && string.Equals(t.Name, simpleName, StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        if (matches.Length > 1)
        {
            // Prefer a domain-assembly definition when the name exists in both places.
            var domainMatch = matches.Where(t => t.Assembly == DomainAssembly()).ToArray();
            if (domainMatch.Length == 1) return domainMatch[0];

            Assert.Fail($"AMBIGUOUS_ENUM::{simpleName}::candidates=[{string.Join(",", matches.Select(m => m.FullName))}]");
        }

        return matches.SingleOrDefault();
    }

    private static IEnumerable<Type> DomainAssemblyTypes() => SafeTypes(DomainAssembly());

    private static IEnumerable<Type> ContractsAssemblyTypes()
    {
        var assembly = LoadAssembly("AeroTech.Messages");
        return assembly is null ? [] : SafeTypes(assembly);
    }

    private static IEnumerable<Type> FrameworkTypes()
    {
        var assembly = LoadAssembly("AeroTech.Framework.Core");
        return assembly is null ? [] : SafeTypes(assembly);
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        Type?[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types; }

        foreach (var type in types)
            if (type is not null) yield return type;
    }

    private static Assembly DomainAssembly()
        => LoadAssembly("AeroTech.Ordering.Domain")
           ?? throw new InvalidOperationException("AeroTech.Ordering.Domain could not be loaded.");

    private static Assembly? LoadAssembly(string name)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (loaded is not null) return loaded;

        try { return Assembly.Load(name); }
        catch (FileNotFoundException) { return null; }
        catch (BadImageFormatException) { return null; }
    }

    private static bool DerivesFromGeneric(Type type, Type genericBase)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == genericBase)
                return true;
        }
        return false;
    }

    private static Manifest LoadManifest()
    {
        var yaml = File.ReadAllText(Path.Combine(FindRepoRoot(), "canonical-stage1-v4.6.yaml"));

        return new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<Manifest>(yaml);
    }

    private static int LoadStage()
        => int.Parse(File.ReadAllText(Path.Combine(FindRepoRoot(), ".canonical-stage")).Trim());

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "canonical-stage1-v4.6.yaml")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root containing canonical-stage1-v4.6.yaml was not found.");
    }

    public sealed class Manifest
    {
        public string Version { get; set; } = "";
        public Stage1Spec Stage1 { get; set; } = new();
    }

    public sealed class Stage1Spec
    {
        public List<string> Types { get; set; } = [];
        public List<string> AbstractTypes { get; set; } = [];
        public string ServiceBase { get; set; } = "";
        public List<string> ServiceSubtypes { get; set; } = [];
        public List<string> ValueObjects { get; set; } = [];
        public Dictionary<string, Dictionary<string, long>> Enums { get; set; } = [];
    }
}

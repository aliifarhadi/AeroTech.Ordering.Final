using AeroTech.Framework.Core.Domain.ValueObjects;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;
using static AeroTech.Ordering.Domain.ConformanceTests.Stage1TypeSetConformanceTests;

namespace AeroTech.Ordering.Domain.ConformanceTests;

public sealed class Stage2TypeSetConformanceTests
{
    private const int Stage = 2;

    private readonly Manifest _manifest = LoadManifest();
    private readonly int _stage = LoadStage();

    [Fact]
    public void Stage2_manifest_is_complete()
    {
        Assert.Equal("1.1", _manifest.Version);
        Assert.NotEmpty(_manifest.Stage2.Types);
        Assert.NotEmpty(_manifest.Stage2.ValueObjects);
        Assert.NotEmpty(_manifest.Stage2.Enums);
    }

    [Fact]
    public void Stage2_domain_type_set_is_exact()
    {
        if (_stage != Stage) return;

        var expected = Sorted(_manifest.Stage2.Types.Except(_manifest.Stage2.AbstractTypes, StringComparer.Ordinal));
        var actual = Sorted(DomainModelTypes().Where(type => !type.IsAbstract).Select(type => type.Name));

        AssertExact("STAGE2_TYPE_SET_MISMATCH", expected, actual);

        foreach (var abstractName in _manifest.Stage2.AbstractTypes)
        {
            var type = FindDomainType(abstractName);
            Assert.True(type is { IsAbstract: true }, $"STAGE2_ABSTRACT_TYPE_MISSING::{abstractName}");
        }
    }

    [Fact]
    public void Stage2_value_object_set_is_exact()
    {
        if (_stage != Stage) return;

        var expected = Sorted(_manifest.Stage2.ValueObjects);
        var actual = Sorted(DomainAssemblyTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ValueObject).IsAssignableFrom(type))
            .Select(type => type.Name));

        AssertExact("STAGE2_VALUE_OBJECT_SET_MISMATCH", expected, actual);
    }

    [Fact]
    public void Stage2_frozen_enum_values_are_exact()
    {
        if (_stage < Stage) return;

        foreach (var (enumName, expectedMembers) in _manifest.Stage2.Enums)
        {
            var type = FindEnum(enumName);
            Assert.True(type is not null, $"STAGE2_ENUM_MISSING::{enumName}");

            var actual = Enum.GetNames(type!)
                .ToDictionary(name => name, name => Convert.ToInt64(Enum.Parse(type!, name)));

            Assert.True(
                Render(expectedMembers) == Render(actual),
                $"STAGE2_ENUM_MISMATCH::{enumName}::expected=[{Render(expectedMembers)}]::actual=[{Render(actual)}]");
        }
    }

    private static string[] Sorted(IEnumerable<string> names)
        => names.Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();

    private static void AssertExact(string failureCode, string[] expected, string[] actual)
    {
        var missing = expected.Except(actual, StringComparer.Ordinal).ToArray();
        var extra = actual.Except(expected, StringComparer.Ordinal).ToArray();

        Assert.True(
            missing.Length == 0 && extra.Length == 0,
            $"{failureCode}::missing=[{string.Join(",", missing)}]::extra=[{string.Join(",", extra)}]");
    }

    private static Manifest LoadManifest()
        => new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<Manifest>(File.ReadAllText(Path.Combine(FindRepoRoot(), "canonical-stage2-v1.1.yaml")));

    public sealed class Manifest
    {
        public string Version { get; set; } = "";

        public Stage2Spec Stage2 { get; set; } = new();
    }

    public sealed class Stage2Spec
    {
        public List<string> Types { get; set; } = [];

        public List<string> AbstractTypes { get; set; } = [];

        public List<string> ValueObjects { get; set; } = [];

        public Dictionary<string, Dictionary<string, long>> Enums { get; set; } = [];
    }
}

using AeroTech.Framework.Core.Domain.ValueObjects;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;
using static AeroTech.Ordering.Domain.ConformanceTests.Stage1TypeSetConformanceTests;

namespace AeroTech.Ordering.Domain.ConformanceTests;

public sealed class Stage6ATypeSetConformanceTests
{
    private const int Stage = 6;

    private readonly Manifest _manifest = LoadManifest();
    private readonly int _stage = LoadStage();

    [Fact]
    public void Stage6A_manifest_is_complete()
    {
        Assert.Equal("1.0", _manifest.Version);
        Assert.NotEmpty(_manifest.Stage6a.Types);
        Assert.NotEmpty(_manifest.Stage6a.ValueObjects);
        Assert.NotEmpty(_manifest.Stage6a.Enums);
    }

    [Fact]
    public void Stage6A_domain_type_set_is_exact()
    {
        if (_stage != Stage) return;

        var expected = Sorted(_manifest.Stage6a.Types.Except(_manifest.Stage6a.AbstractTypes, StringComparer.Ordinal));
        var actual = Sorted(DomainModelTypes().Where(type => !type.IsAbstract).Select(type => type.Name));

        AssertExact("STAGE6A_TYPE_SET_MISMATCH", expected, actual);

        foreach (var abstractName in _manifest.Stage6a.AbstractTypes)
        {
            var type = FindDomainType(abstractName);
            Assert.True(type is { IsAbstract: true }, $"STAGE6A_ABSTRACT_TYPE_MISSING::{abstractName}");
        }
    }

    [Fact]
    public void Stage6A_value_object_set_is_exact()
    {
        if (_stage != Stage) return;

        var expected = Sorted(_manifest.Stage6a.ValueObjects);
        var actual = Sorted(DomainAssemblyTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(ValueObject).IsAssignableFrom(type))
            .Select(type => type.Name));

        AssertExact("STAGE6A_VALUE_OBJECT_SET_MISMATCH", expected, actual);
    }

    [Fact]
    public void Stage6A_frozen_enum_values_are_exact()
    {
        if (_stage < Stage) return;

        foreach (var (enumName, expectedMembers) in _manifest.Stage6a.Enums)
        {
            var type = FindEnum(enumName);
            Assert.True(type is not null, $"STAGE6A_ENUM_MISSING::{enumName}");

            var actual = Enum.GetNames(type!)
                .ToDictionary(name => name, name => Convert.ToInt64(Enum.Parse(type!, name)));

            Assert.True(
                Render(expectedMembers) == Render(actual),
                $"STAGE6A_ENUM_MISMATCH::{enumName}::expected=[{Render(expectedMembers)}]::actual=[{Render(actual)}]");
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
            .Deserialize<Manifest>(File.ReadAllText(Path.Combine(FindRepoRoot(), "canonical-stage6a-v1.0.yaml")));

    public sealed class Manifest
    {
        public string Version { get; set; } = "";

        public Stage6aSpec Stage6a { get; set; } = new();
    }

    public sealed class Stage6aSpec
    {
        public List<string> Types { get; set; } = [];

        public List<string> AbstractTypes { get; set; } = [];

        public List<string> ValueObjects { get; set; } = [];

        public Dictionary<string, Dictionary<string, long>> Enums { get; set; } = [];
    }
}

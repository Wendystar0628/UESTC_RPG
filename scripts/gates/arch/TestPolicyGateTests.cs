using System.Text.Json;
using System.Text.RegularExpressions;
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class TestPolicyGateTests
{
    [TestCase]
    [TestCategory("Contract")]
    public void TestFixturesUseBehaviorAndRuntimeObservations() => SourceModel.RequireNoViolations(
        SourceModel.Tests.SelectMany(TestBehaviorPolicy.ReflectionAndAssertionViolations));

    [TestCase]
    [TestCategory("Contract")]
    public async Task ExtraTestDoubleMethodsHaveConsumers() => SourceModel.RequireNoViolations(
        await TestBehaviorPolicy.UnusedDoubleMethods(SourceModel.Tests, SourceModel.Solution));

    [TestCase]
    [TestCategory("Contract")]
    public void EveryTestCaseDeclaresExactlyOneReason()
    {
        var errors = new List<string>();
        var count = 0;
        foreach (var file in SourceModel.Tests)
        foreach (var method in file.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!Attributes(file, method, "TestCase").Any()) continue;
            count++;
            var reasons = Categories(file, method).Count(value => value is "Contract" or "Smoke" or "Regression");
            if (reasons != 1) errors.Add(file.At(method, "A TestCase needs exactly one Contract, Smoke or Regression category; Determinism/Slow are additional tags."));
        }
        if (count == 0) errors.Add("G4 found no TestCase declarations.");
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void AssertionTolerancesReferenceRulesInsteadOfNumericLiterals()
    {
        var errors = new List<string>();
        foreach (var file in SourceModel.Tests)
        {
            foreach (var comparison in file.Root.DescendantNodes().OfType<BinaryExpressionSyntax>().Where(AssertionCriteria.IsComparison))
            {
                if (IsDeterminism(file, comparison)) continue;
                var tolerance = AssertionCriteria.UpperErrorBound(file, comparison);
                if (tolerance is not null && AssertionCriteria.HasLiteralCriterion(file, tolerance))
                    errors.Add(file.At(tolerance, "Error bounds reference TestTolerance or production geometry rules; no local numeric tolerance."));
            }
            foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (IsDeterminism(file, call)) continue;
                var method = SourceModel.Method(file, call);
                var callName = SourceModel.CallName(call);
                var assertion = callName is "IsEqualApprox" or "IsEqual" or "IsWithin" or "Near" or "Close" or "AssertNear" or "AssertClose";
                if (!assertion) continue;
                foreach (var (argument, index) in call.ArgumentList.Arguments.Select((argument, index) => (argument, index)))
                {
                    var parameter = argument.NameColon?.Name.Identifier.ValueText ??
                        (method is not null && index < method.Parameters.Length ? method.Parameters[index].Name : "");
                    var isTolerance = parameter.Contains("tolerance", StringComparison.OrdinalIgnoreCase) ||
                        parameter.Equals("epsilon", StringComparison.OrdinalIgnoreCase) || parameter.Equals("delta", StringComparison.OrdinalIgnoreCase) ||
                        parameter.Equals("precision", StringComparison.OrdinalIgnoreCase) ||
                        callName == "IsEqualApprox" && index == 1 && argument.NameColon is null;
                    if (isTolerance && AssertionCriteria.HasLiteralCriterion(file, argument.Expression))
                        errors.Add(file.At(argument, "Assertion tolerance arguments reference shared or production rules."));
                }
            }
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void ExceptionAssertionsCheckTypesInsteadOfMessages()
    {
        var errors = new List<string>();
        foreach (var file in SourceModel.Tests)
        {
            foreach (var member in file.Root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (member.Name.Identifier.ValueText == "Message" && SourceModel.Inherits(file.Model.GetTypeInfo(member.Expression).Type, "System.Exception") &&
                    AssertionCriteria.IsAssertionOrGuard(member)) errors.Add(file.At(member, "Exception assertions check the exception type, not Message."));
            }
            foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                if (SourceModel.CallName(call).StartsWith("HasMessage", StringComparison.Ordinal) &&
                    call.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(inner => SourceModel.CallName(inner) == "AssertThrown"))
                    errors.Add(file.At(call, "Exception assertions check the exception type, not message text."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void RealEntityIdsComeFromCatalogsOrData()
    {
        var identities = RealIdentities();
        var tokens = new Regex(@"[A-Za-z0-9_-]+(?::[A-Za-z0-9_.-]+)+", RegexOptions.CultureInvariant);
        var errors = new List<string>();
        foreach (var file in SourceModel.Tests)
        {
            foreach (var literal in file.Root.DescendantNodes().OfType<LiteralExpressionSyntax>().Where(node => node.Token.Value is string))
                Inspect(literal, literal.Token.ValueText);
            foreach (var text in file.Root.DescendantNodes().OfType<InterpolatedStringTextSyntax>()) Inspect(text, text.TextToken.ValueText);
            foreach (var call in file.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var method = SourceModel.Method(file, call);
                if (method is null || SourceModel.Name(method.ContainingType) != "Campus.Gen.World.WorldFeatureIds" ||
                    method.Name is not ("Way" or "Relation" or "Node") || call.ArgumentList.Arguments.Count != 2) continue;
                var prefix = SourceModel.Constant(file.Model, call.ArgumentList.Arguments[0].Expression) as string;
                var number = call.ArgumentList.Arguments[1].Expression;
                if (prefix is not null && number.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any() &&
                    SourceModel.Constant(file.Model, number) is { } id && identities.Contains($"{prefix}:{method.Name.ToLowerInvariant()}:{id}"))
                    errors.Add(file.At(call, "Use the owning ID catalog instead of recreating a real OSM identity."));
            }
            void Inspect(SyntaxNode node, string value)
            {
                foreach (Match token in tokens.Matches(value))
                {
                    var candidate = token.Value;
                    for (var start = 0; start < candidate.Length;)
                    {
                        if (identities.Contains(candidate[start..]))
                        {
                            errors.Add(file.At(node, $"Real identity {candidate[start..]} must be read from its catalog or data."));
                            break;
                        }
                        var colon = candidate.IndexOf(':', start);
                        if (colon < 0) break;
                        start = colon + 1;
                    }
                }
            }
        }
        SourceModel.RequireNoViolations(errors);
    }

    private static HashSet<string> RealIdentities()
    {
        var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? ArchitectureTests.FindRepository();
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "game", "data", "maps"), "*.json", SearchOption.AllDirectories))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            Read(document.RootElement);
        }
        if (result.Count == 0) throw new InvalidOperationException("G4 could not find real map identities.");
        return result;

        void Read(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) Read(item);
            if (element.ValueKind != JsonValueKind.Object) return;
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name is "id" or "entityId" or "featureId" or "sourceId" && property.Value.ValueKind == JsonValueKind.String &&
                    property.Value.GetString() is { } id && id.Contains(':')) result.Add(id);
                Read(property.Value);
            }
        }
    }

    private static IEnumerable<AttributeSyntax> Attributes(SourceFile file, MethodDeclarationSyntax method, string name) =>
        method.AttributeLists.SelectMany(list => list.Attributes).Where(attribute =>
            file.Model.GetSymbolInfo(attribute).Symbol is IMethodSymbol constructor &&
                SourceModel.Name(constructor.ContainingType) == "GdUnit4." + name + "Attribute" ||
            attribute.Name.ToString().Split('.').Last() is var found && (found == name || found == name + "Attribute"));
    private static IEnumerable<string> Categories(SourceFile file, MethodDeclarationSyntax method) => Attributes(file, method, "TestCategory")
        .SelectMany(attribute => attribute.ArgumentList?.Arguments ?? default)
        .Select(argument => SourceModel.Constant(file.Model, argument.Expression)).OfType<string>();
    private static bool IsDeterminism(SourceFile file, SyntaxNode node) => node.Ancestors().OfType<MethodDeclarationSyntax>()
        .FirstOrDefault() is { } method && Categories(file, method).Contains("Determinism", StringComparer.Ordinal);
}

// Exercises the test-behavior rules through bound symbols and the built-in Roslyn reference finder.
// 用法：经 scripts/gate.ps1 的 G4 入口运行。
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class TestBehaviorPolicyRegressionTests
{
    [TestCase]
    [TestCategory("Regression")]
    public void ReflectionAndConstantRulesKeepTestBridgesAndAuthoritativeConstants()
    {
        var fixture = SemanticPolicyRegressionTests.Fixture("""
            namespace Campus.Probe {
            using System.Reflection;
            using static GdUnit4.Assertions;
            class Sample {
                void BadProduct() { _ = typeof(string).GetField("_firstChar", BindingFlags.NonPublic | BindingFlags.Instance); }
                void BadUnknown(object source) { _ = source.GetType().GetField("state", BindingFlags.NonPublic | BindingFlags.Instance); }
                void BadAliasedFlags() { const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static; _ = typeof(string).GetMethod("Hidden", flags); }
                void BadLiteral() { AssertThat(2 == 2).IsTrue(); }
                void BadLocalConstant() { const int input = 2; AssertThat(input).IsEqual(2); }
                void GoodTestBridge() { _ = typeof(Sample).GetMethod("GoodTestBridge", BindingFlags.NonPublic | BindingFlags.Instance); }
                void GoodPublic() { _ = typeof(string).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance); }
                void GoodRule() { AssertThat(Math.PI > 3).IsTrue(); }
                void GoodAliasedRule() { const double rule = Math.PI; AssertThat(rule > 3).IsTrue(); }
                void GoodEnum() { AssertThat((int)DayOfWeek.Friday).IsEqual(5); }
                void GoodObservation(int value) { AssertThat(value).IsEqual(2); }
            }
            }
            """, MetadataReference.CreateFromFile(typeof(Assertions).Assembly.Location));
        // The shared semantic fixture normally represents production; this case models an actual test source.
        var tree = fixture.Tree.WithFilePath("game/probe/tests/BehaviorTests.cs");
        var compilation = fixture.Model.Compilation.ReplaceSyntaxTree(fixture.Tree, tree);
        var file = new SourceFile(tree.FilePath, tree, compilation.GetSemanticModel(tree));
        var errors = TestBehaviorPolicy.ReflectionAndAssertionViolations(file).ToArray();
        foreach (var method in file.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            if (errors.Any(error => error.StartsWith(file.At(method, ""), StringComparison.Ordinal)) !=
                method.Identifier.ValueText.StartsWith("Bad", StringComparison.Ordinal))
                throw new InvalidOperationException("Test behavior rule regression: " + method.Identifier.ValueText);
    }

    [TestCase]
    [TestCategory("Regression")]
    public async Task UnusedControlsUseRealReferencesAndExcludeContractAndOverrideMethods()
    {
        var fixture = SemanticPolicyRegressionTests.Fixture("""
            namespace Campus.Contracts { interface IControl { void Required(); } }
            class Base { public virtual void Inherited() { } }
            class Double : Base, IControl, IDisposable {
                public void Required() { }
                public override void Inherited() { }
                public void Dispose() { }
                public static void Factory() { }
                public void Used() { }
                public void Unused() { }
                private void LocalOnly() { }
            }
            class Consumer { void Act(Double value) { value.Used(); } }
            """);
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestDoubleCase", LanguageNames.CSharp)
            .WithCompilationOptions(fixture.Model.Compilation.Options)
            .WithParseOptions(fixture.Tree.Options)
            .WithMetadataReferences(fixture.Model.Compilation.References);
        var document = project.AddDocument("BehaviorTests.cs", fixture.Tree.GetText(), filePath: "game/probe/tests/BehaviorTests.cs");
        var tree = await document.GetSyntaxTreeAsync() ?? throw new InvalidOperationException("Missing test-double fixture tree.");
        var compilation = await document.Project.GetCompilationAsync() ?? throw new InvalidOperationException("Missing test-double fixture compilation.");
        var file = new SourceFile(tree.FilePath, tree, compilation.GetSemanticModel(tree));
        var candidates = TestBehaviorPolicy.ExtraDoubleMethods([file]).Select(candidate => candidate.Method.Name).Order(StringComparer.Ordinal).ToArray();
        if (!candidates.SequenceEqual(new[] { "Unused", "Used" }))
            throw new InvalidOperationException("Test-double scope included contract, override, factory or private methods.");
        var errors = await TestBehaviorPolicy.UnusedDoubleMethods([file], document.Project.Solution);
        if (errors.Count != 1 || !errors[0].Contains("Unused has no consumer", StringComparison.Ordinal))
            throw new InvalidOperationException("Roslyn references did not distinguish the consumed and unused test controls.");
    }
}

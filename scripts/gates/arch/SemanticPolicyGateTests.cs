// Checks the semantic source rules in STD-CODE using the existing G4 Roslyn model.
// 用法：经 scripts/gate.ps1 的 G4 入口运行。
using System.Text.RegularExpressions;
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class SemanticPolicyGateTests
{
    [TestCase]
    [TestCategory("Contract")]
    public void ExceptionsReachThrowOrDocumentedFailureFlows()
    {
        var sources = SemanticPolicySources.Load();
        var documents = SemanticPolicySources.Documents();
        var errors = new List<string>();
        foreach (var file in sources.Where(file => !file.IsTest))
        {
            foreach (var parameter in file.Root.DescendantNodes().OfType<ParameterSyntax>())
            {
                if (file.Model.GetDeclaredSymbol(parameter) is not IParameterSymbol symbol) continue;
                foreach (var attribute in symbol.GetAttributes().Where(attribute => SemanticPolicySources.Is(attribute, "FailureHandler")))
                    if (!FailureFlow.IsHandler(symbol, documents))
                        errors.Add(file.At(parameter, "FailureHandler requires an exception-object parameter and one existing document code (STD-CODE)."));
            }
            foreach (var clause in file.Root.DescendantNodes().OfType<CatchClauseSyntax>())
                if (!FailureFlow.Delivered(file, clause, documents))
                    errors.Add(file.At(clause, "Every non-cancellation catch path must retain the caught exception in a throw or pass it to a documented FailureHandler parameter (STD-CODE)."));
        }
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void DataIdentityAndVersionDoNotSelectSpecialImplementations()
    {
        var errors = new List<string>();
        var sources = SemanticPolicySources.Load().Where(file => !file.IsTest).ToArray();
        foreach (var file in sources)
            errors.AddRange(new DataBranchPolicy(file).Violations());
        SourceModel.RequireNoViolations(errors);
    }

    [TestCase]
    [TestCategory("Contract")]
    public void SourceDoesNotRetainOldApisOrDisableTests()
    {
        var errors = new List<string>();
        foreach (var file in SemanticPolicySources.Load())
        {
            foreach (var attribute in file.Root.DescendantNodes().OfType<AttributeSyntax>())
            {
                var type = (file.Model.GetSymbolInfo(attribute).Symbol as IMethodSymbol)?.ContainingType;
                if (SourceModel.Name(type) == "System.ObsoleteAttribute")
                    errors.Add(file.At(attribute, "Remove replaced APIs instead of retaining Obsolete declarations (STD-CODE)."));
                if (!file.IsTest || type is null) continue;
                var space = type.ContainingNamespace.ToDisplayString();
                var testAttribute = space == "GdUnit4" || space.StartsWith("NUnit.Framework", StringComparison.Ordinal) ||
                    space.StartsWith("Xunit", StringComparison.Ordinal) || space.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting", StringComparison.Ordinal);
                if (!testAttribute) continue;
                if (type.Name.StartsWith("Ignore", StringComparison.Ordinal) || type.Name.StartsWith("Skip", StringComparison.Ordinal) || type.Name == "ExplicitAttribute" ||
                    attribute.ArgumentList?.Arguments.Any(argument =>
                        argument.NameEquals?.Name.Identifier.ValueText is { } setting &&
                        (setting.StartsWith("Skip", StringComparison.Ordinal) || setting.StartsWith("Ignore", StringComparison.Ordinal) || setting == "Explicit") &&
                        SourceModel.Constant(file.Model, argument.Expression) is not (false or null)) == true)
                    errors.Add(file.At(attribute, "Tests are executed or removed; Skip/Ignore is forbidden (STD-CODE)."));
            }
            foreach (var directive in file.Root.DescendantTrivia(descendIntoTrivia: true).Select(trivia => trivia.GetStructure()))
            {
                var condition = directive switch { IfDirectiveTriviaSyntax item => item.Condition, ElifDirectiveTriviaSyntax item => item.Condition, _ => null };
                if (condition is null) continue;
                foreach (var symbol in condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                    if (!EngineOrFrameworkSymbol(symbol.Identifier.ValueText))
                        errors.Add(file.At(symbol, "Conditional compilation uses only engine/.NET SDK symbols (STD-CODE)."));
            }
        }
        SourceModel.RequireNoViolations(errors);
    }

    private static bool EngineOrFrameworkSymbol(string symbol) => symbol is "DEBUG" or "TRACE" or "TOOLS" or "GODOT" or
        "GODOT_PC" or "GODOT_WINDOWS" or "GODOT_LINUXBSD" or "GODOT_OSX" or "GODOT_MACOS" or "GODOT_ANDROID" or
        "GODOT_IPHONE" or "GODOT_IOS" or "GODOT_WEB" or "GODOT_JAVASCRIPT" or "GODOT_HTML5" or "GODOT_WASM" or
        "GODOT_MOBILE" or "GODOT_REAL_T_IS_DOUBLE" ||
        Regex.IsMatch(symbol, @"^GODOT\d+(_\d+)*(_OR_GREATER)?$") ||
        Regex.IsMatch(symbol, @"^(NET|NETFRAMEWORK|NETSTANDARD|NETCOREAPP)(\d+(_\d+)*(_OR_GREATER)?)?$") ||
        Regex.IsMatch(symbol, @"^(WINDOWS|LINUX|MACOS|ANDROID|IOS|TVOS|MACCATALYST|BROWSER|FREEBSD)(\d+(_\d+)*(_OR_GREATER)?)?$");
}

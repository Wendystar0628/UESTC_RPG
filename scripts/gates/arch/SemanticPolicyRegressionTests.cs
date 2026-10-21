// Keeps semantic gates sensitive to object flow and declaration meaning rather than spelling.
// 用法：经 scripts/gate.ps1 的 G4 入口运行。
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class SemanticPolicyRegressionTests
{
    [TestCase]
    [TestCategory("Regression")]
    public void CatchPathsPreserveTheOriginalFailure()
    {
        var file = Fixture("""
            class Sample {
                static void Handle([FailureHandler("ENG-ARCH")] Exception error) { }
                static void Unknown([FailureHandler("DOES-NOT-EXIST")] Exception error) { }
                static void Log(string message) { }
                static void Run() { }
                sealed class StartupFailure(string message, Exception cause) : Exception(message, cause) {
                    public static StartupFailure Stalled(string text, Exception renamed) => new(text, renamed);
                }
                static Exception ChooseFailure(string message, Exception renamed, bool flag) {
                    var alias = renamed;
                    if(flag) return StartupFailure.Stalled(renamed: alias, text: message);
                    return new StartupFailure(message, alias);
                }
                static Exception MessageFailure(Exception cause) => new Exception(cause.Message);
                static Exception? NullableFailure(Exception? cause) => new InvalidOperationException("x", cause);
                static Exception UnrelatedFailure(Exception cause) => new Exception("x", new Exception());
                static Exception LosingFailure(Exception cause, bool flag) {
                    if(flag) return new Exception("x", cause);
                    return new Exception("x");
                }
                void GoodThrow() { try { Run(); } catch { throw; } }
                void GoodWrap() { try { Run(); } catch(Exception e) { throw new InvalidOperationException("x", e); } }
                void GoodFactory(bool flag) { try { Run(); } catch(Exception e) { throw ChooseFailure(flag: flag, renamed: e, message: "x"); } }
                void GoodNullableFactory() { try { Run(); } catch(Exception e) { throw NullableFailure(e)!; } }
                void GoodReceive() { try { Run(); } catch(Exception e) { var renamed = e; Handle(renamed); } }
                void GoodNamed() { try { Run(); } catch(Exception e) { Handle(error: e); } }
                int GoodInput() { try { Run(); } catch(System.IO.InvalidDataException e) { Handle(e); return 0; } return 1; }
                void GoodCollect() { var errors = new List<Exception>(); try { Run(); } catch(Exception e) { errors.Add(e); } if(errors.Count != 0) throw new AggregateException(errors); }
                void GoodCancel() { try { Run(); } catch(OperationCanceledException) { } }
                void GoodFilter() { try { Run(); } catch(Exception e) when(e is OperationCanceledException) { } }
                void BadBranch(bool flag) { try { Run(); } catch(Exception e) { if(flag) Handle(e); } }
                void BadThrow() { try { Run(); } catch(Exception e) { throw new InvalidOperationException("x"); } }
                void BadFactoryMessage() { try { Run(); } catch(Exception e) { throw MessageFailure(e); } }
                void BadFactoryUnrelated() { try { Run(); } catch(Exception e) { throw UnrelatedFailure(e); } }
                void BadFactoryBranch(bool flag) { try { Run(); } catch(Exception e) { throw LosingFailure(e, flag); } }
                void BadText() { try { Run(); } catch(Exception e) { Log(e.Message); } }
                void BadLambda() { try { Run(); } catch(Exception e) { Action later = () => Handle(e); } }
                void BadDocument() { try { Run(); } catch(Exception e) { Unknown(e); } }
                void BadCancel(bool flag) { try { Run(); } catch(Exception) when(flag) { } }
                void BadOverwrite() { try { Run(); } catch(Exception e) { var renamed = e; renamed = null; throw renamed; } }
                class Carrier { public Exception Failure; }
                void BadReceiver() { var left = new Carrier(); var right = new Carrier(); try { Run(); } catch(Exception e) { left.Failure = e; throw right.Failure; } }
            }
            """);
        var mismatches = new List<string>();
        foreach (var clause in file.Root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            var method = clause.Ancestors().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText;
            var actual = FailureFlow.Delivered(file, clause, new HashSet<string>(StringComparer.Ordinal) { "ENG-ARCH" });
            if (actual != method.StartsWith("Good", StringComparison.Ordinal))
                mismatches.Add(method + " (actual: " + actual + ")");
        }
        if (mismatches.Count != 0)
            throw new InvalidOperationException("Exception provenance regression: " + string.Join(", ", mismatches));
    }

    [TestCase]
    [TestCategory("Regression")]
    public void ConditionalBranchesKeepTheirDeclarationContexts()
    {
        var file = Fixture("""
            class Sample {
            #if GODOT4 && !TOOLS
                [Obsolete] void Replaced() { }
            #if TOOLS
                void Impossible() { }
            #endif
            #elif GODOT4
                void Editor() { }
            #else
                void Current() { }
            #endif
            }
            """);
        var names = SemanticBranchSources.Expand(file).SelectMany(source => source.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Select(method => method.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        if (!names.Contains("Replaced") || !names.Contains("Editor") || !names.Contains("Current") || names.Contains("Impossible"))
            throw new InvalidOperationException("Conditional source branch was hidden or a contradictory branch was included.");

        var directive = file.Root.DescendantTrivia(descendIntoTrivia: true).Select(trivia => trivia.GetStructure())
            .OfType<IfDirectiveTriviaSyntax>().First();
        var unsupportedTree = file.Tree.WithRootAndOptions(file.Root.ReplaceNode(directive,
            directive.WithCondition(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)))), file.Tree.Options);
        var unsupported = new SourceFile(file.Path, unsupportedTree,
            file.Model.Compilation.ReplaceSyntaxTree(file.Tree, unsupportedTree).GetSemanticModel(unsupportedTree));
        var rejected = false;
        try { SemanticBranchSources.Expand(unsupported).ToArray(); }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("An unknown conditional source expression was silently skipped.");
    }

    [TestCase]
    [TestCategory("Regression")]
    public void DataMarkersSurviveRenamingAndDoNotBanVersionRejection()
    {
        var file = Fixture("""
            [DataId] readonly record struct Key(string Value);
            class Sample {
                const string CatalogEntry = "tree";
                [DataVersion] const int Current = 2;
                bool GoodCatalog(Key renamed) => renamed.Value == CatalogEntry;
                bool GoodUnrelated(string id) => id == "tree";
                void GoodReject([DataVersion] int renamed) { if(renamed != Current) throw new Exception(); }
                bool BadIdentity(Key renamed) { var copy = renamed.Value; return copy == "tree"; }
                bool GoodIdentityPattern(Key renamed) => renamed.Value is CatalogEntry;
                bool BadIdentityPattern(Key renamed) => renamed.Value is "tree";
                int GoodIdentitySwitch(Key renamed) { switch(renamed.Value) { case CatalogEntry: return 1; default: return 0; } }
                int BadIdentitySwitch(Key renamed) { switch(renamed.Value) { case "tree": return 1; default: return 0; } }
                int GoodIdentityExpression(Key renamed) => renamed.Value switch { CatalogEntry => 1, _ => 0 };
                int BadIdentityExpression(Key renamed) => renamed.Value switch { "tree" => 1, _ => 0 };
                bool GoodEquals(Key renamed) => renamed.Value.Equals(CatalogEntry);
                bool BadEquals(Key renamed) => renamed.Value.Equals("tree");
                int GoodCompare(Key renamed) => string.Compare(renamed.Value, CatalogEntry, StringComparison.Ordinal);
                int BadCompare(Key renamed) => string.Compare(renamed.Value, "tree", StringComparison.Ordinal);
                int GoodCompareTo(Key renamed) => renamed.Value.CompareTo(CatalogEntry);
                int BadCompareTo(Key renamed) => renamed.Value.CompareTo("tree");
                bool GoodStartsWith(Key renamed) => renamed.Value.StartsWith(CatalogEntry, StringComparison.Ordinal);
                bool BadStartsWith(Key renamed) => renamed.Value.StartsWith("tree", StringComparison.Ordinal);
                bool GoodEndsWith(Key renamed) => renamed.Value.EndsWith(CatalogEntry, StringComparison.Ordinal);
                bool BadEndsWith(Key renamed) => renamed.Value.EndsWith("tree", StringComparison.Ordinal);
                bool GoodContains(Key renamed) => renamed.Value.Contains(CatalogEntry, StringComparison.Ordinal);
                bool BadContains(Key renamed) => renamed.Value.Contains("tree", StringComparison.Ordinal);
                int GoodIndexOf(Key renamed) => renamed.Value.IndexOf(CatalogEntry, StringComparison.Ordinal);
                int BadIndexOf(Key renamed) => renamed.Value.IndexOf("tree", StringComparison.Ordinal);
                int GoodLastIndexOf(Key renamed) => renamed.Value.LastIndexOf(CatalogEntry, StringComparison.Ordinal);
                int BadLastIndexOf(Key renamed) => renamed.Value.LastIndexOf("tree", StringComparison.Ordinal);
                void GoodVersionEqual([DataVersion] int renamed) { if(renamed == Current) { } else throw new Exception(); }
                bool BadVersionEqual([DataVersion] int renamed) => renamed == Current;
                bool BadVersionNotEqual([DataVersion] int renamed) => renamed != Current;
                void GoodVersionLess([DataVersion] int renamed) { if(renamed < Current) throw new Exception(); }
                bool BadVersionLess([DataVersion] int renamed) => renamed < Current;
                void GoodVersionLessOrEqual([DataVersion] int renamed) { if(renamed <= Current) throw new Exception(); }
                bool BadVersionLessOrEqual([DataVersion] int renamed) => renamed <= Current;
                void GoodVersionGreater([DataVersion] int renamed) { if(renamed > Current) throw new Exception(); }
                bool BadVersionGreater([DataVersion] int renamed) => renamed > Current;
                void GoodVersionGreaterOrEqual([DataVersion] int renamed) { if(renamed >= Current) throw new Exception(); }
                bool BadVersionGreaterOrEqual([DataVersion] int renamed) => renamed >= Current;
                void GoodVersionPattern([DataVersion] int renamed) { if(renamed is not Current) throw new Exception(); }
                void GoodVersionNamedPattern([DataVersion] int renamed) { if(renamed is Current) { } else throw new Exception(); }
                int BadVersionPattern([DataVersion] int renamed) => renamed is Current ? 10 : 20;
                int BadVersionNotPattern([DataVersion] int renamed) => renamed is not Current ? 10 : 20;
                int GoodVersionSwitch([DataVersion] int renamed) { switch(renamed) { case Current: return 20; default: throw new Exception(); } }
                int BadVersionSwitch([DataVersion] int renamed) { switch(renamed) { case 1: return 10; case Current: return 20; default: throw new Exception(); } }
                int GoodVersionExpression([DataVersion] int renamed) => renamed switch { Current => 20, _ => throw new Exception() };
                int BadVersionExpression([DataVersion] int renamed) => renamed switch { 1 => 10, Current => 20, _ => throw new Exception() };
                void GoodVersionCall([DataVersion] int renamed) { if(renamed.CompareTo(Current) != 0) throw new Exception(); }
                int BadVersionCall([DataVersion] int renamed) => renamed.CompareTo(Current);
                int GoodConditional([DataVersion] int renamed) => renamed == Current ? 20 : throw new Exception();
                int BadConditional([DataVersion] int renamed) => renamed == 1 ? 10 : 20;
                void GoodVersionFilter([DataVersion] int renamed) { try { throw new Exception(); } catch(Exception) when(renamed != Current) { throw; } }
                int BadVersionFilter([DataVersion] int renamed) { try { throw new Exception(); } catch(Exception) when(renamed == 1) { return 10; } catch(Exception) { return 20; } }
            }
            """);
        var errors = new DataBranchPolicy(file).Violations().ToArray();
        var mismatches = new List<string>();
        // Each case occupies one line so diagnostics can be matched to the case that owns the syntax.
        foreach (var method in file.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var actual = errors.Any(error => error.StartsWith(file.At(method, ""), StringComparison.Ordinal));
            if (actual != method.Identifier.ValueText.StartsWith("Bad", StringComparison.Ordinal))
                mismatches.Add(method.Identifier.ValueText + " (violation: " + actual + ")");
        }
        if (mismatches.Count != 0) throw new InvalidOperationException("Data declaration regression: " + string.Join(", ", mismatches));
    }

    private static SourceFile Fixture(string body)
    {
        var source = """
            #nullable enable
            using System;
            using System.Collections.Generic;
            using Campus.Contracts;
            namespace Campus.Contracts {
                [AttributeUsage(AttributeTargets.All)] sealed class DataIdAttribute : Attribute { }
                [AttributeUsage(AttributeTargets.All)] sealed class DataVersionAttribute : Attribute { }
                [AttributeUsage(AttributeTargets.Parameter)] sealed class FailureHandlerAttribute(string document) : Attribute { }
            }
            """ + body;
        var tree = CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), "game/semantic-case.cs");
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator)
            .Where(path => path.Length != 0).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("G4.SemanticCase", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        return new SourceFile("game/semantic-case.cs", tree, compilation.GetSemanticModel(tree));
    }
}

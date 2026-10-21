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
            #else
                void Current() { }
            #endif
            }
            """);
        var names = SemanticBranchSources.Expand(file).SelectMany(source => source.Root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Select(method => method.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        if (!names.Contains("Replaced") || !names.Contains("Current")) throw new InvalidOperationException("Conditional source branch was hidden.");
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
                int BadVersions([DataVersion] int renamed) => renamed switch { 1 => 10, 2 => 20, _ => throw new Exception() };
                int BadConditional([DataVersion] int renamed) => renamed == 1 ? 10 : 20;
            }
            """);
        var errors = new DataBranchPolicy(file).Violations().ToArray();
        if (errors.Length != 3) throw new InvalidOperationException("Data declaration regression: " + string.Join(Environment.NewLine, errors));
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

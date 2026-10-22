// Guards native wait ownership and HUD source dependencies through the production G4 policies.
// 用法：经 scripts/gate.ps1 的 G4 入口运行。
using GdUnit4;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Campus.Arch.Tests;

[TestSuite]
public sealed class ArchitectureSourceRegressionTests
{
    [TestCase]
    [TestCategory("Regression")]
    public void NativeSignalWaitRuleUsesTheBoundGodotSymbols()
    {
        var file = SemanticPolicyRegressionTests.Fixture("""
            namespace Campus.Probe {
                using System.Threading.Tasks;
                using Native = global::Godot.GodotObject;
                using Waiter = global::Godot.SignalAwaiter;
                class Ordinary {
                    public static Task ToSignal(object source, string signal) => Task.CompletedTask;
                    public sealed class SignalAwaiter { }
                }
                class Sample : Godot.Node {
                    async Task BadInherited() { await base.ToSignal(this, "ready"); }
                    async Task BadAlias(Native native) { await native.ToSignal(native, "ready"); }
                    async Task BadQualified(global::Godot.GodotObject native) { await native.ToSignal(native, "ready"); }
                    async Task BadLocal() { var native = GetTree(); await native.ToSignal(native, "process_frame"); }
                    async Task BadAsyncLambda(Native native) { Func<Task> wait = async () => { await native.ToSignal(native, "ready"); }; await wait(); }
                    void BadConstructor(Native native) { _ = new global::Godot.SignalAwaiter(native, "ready", native); }
                    void BadConstructorAlias(Native native) { _ = new Waiter(native, "ready", native); }
                    async Task BadAsyncConstructor(Native native) { await new Waiter(native, "ready", native); }
                    Waiter BadTargetTyped(Native native) => new(native, "ready", native);
                    async Task GoodOrdinary() { await Ordinary.ToSignal(this, "ready"); _ = new Ordinary.SignalAwaiter(); }
                    async Task GoodAsyncLambda() { Func<Task> wait = async () => { await Ordinary.ToSignal(this, "ready"); }; await wait(); }
                }
            }
            """, MetadataReference.CreateFromFile(typeof(Godot.GodotObject).Assembly.Location));
        var errors = ArchitectureTests.NativeSignalWaitViolations(file).ToArray();
        foreach (var method in file.Root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == "Sample").Members.OfType<MethodDeclarationSyntax>())
        {
            var rejected = errors.Any(error => error.StartsWith(file.At(method, ""), StringComparison.Ordinal));
            if (rejected != method.Identifier.ValueText.StartsWith("Bad", StringComparison.Ordinal))
                throw new InvalidOperationException("Native signal wait regression: " + method.Identifier.ValueText);
        }
    }

    [TestCase]
    [TestCategory("Regression")]
    public void HudSourceRuleRejectsKernelConstantsAndPreservesTheRegistryEntry()
    {
        const string declarations = """
            namespace Campus.Kernel.Boot { public static class BootTuning { public const double Seconds = 1; } }
            namespace Campus.Kernel.Registry {
                public static class Kernel { public static T Get<T>() => default!; }
                public static class Internal { public const double Seconds = 1; }
            }
            namespace Campus.Contracts { public interface IMarker { } public static class WaitTuning { public const double Seconds = 1; } }
            namespace Campus.Other { public static class BootTuning { public const double Seconds = 1; } }
            """;
        (string Source, bool Rejected)[] cases =
        [
            ("namespace Campus.Hud { class Probe { const double Seconds = global::Campus.Kernel.Boot.BootTuning.Seconds; } }", true),
            ("namespace Campus.Hud { using Limits = Campus.Kernel.Boot.BootTuning; class Probe { const double Seconds = Limits.Seconds; } }", true),
            ("namespace Campus.Hud { using static Campus.Kernel.Boot.BootTuning; class Probe { const double Value = Seconds; } }", true),
            ("namespace Campus.Hud { using Campus.Kernel.Boot; class Probe { } }", true),
            ("namespace Campus.Hud { class Probe { const double Seconds = Campus.Kernel.Registry.Internal.Seconds; } }", true),
            ("namespace Campus.Hud { using Campus.Kernel.Registry; class Probe { object? Read() => Kernel.Get<IMarker>(); } }", false),
            ("namespace Campus.Hud { using Registry = Campus.Kernel.Registry.Kernel; class Probe { object? Read() => Registry.Get<IMarker>(); } }", false),
            ("namespace Campus.Hud { class Probe { object? Read() => global::Campus.Kernel.Registry.Kernel.Get<IMarker>(); } }", false),
            ("namespace Campus.Hud { class Probe { const double Seconds = Campus.Contracts.WaitTuning.Seconds; } }", false),
            ("namespace Campus.Hud { using Campus.Other; class Probe { const double Seconds = BootTuning.Seconds; } }", false),
            ("namespace Campus.Other { class Probe { const double Seconds = Campus.Kernel.Boot.BootTuning.Seconds; } }", false)
        ];
        foreach (var (source, rejected) in cases)
        {
            var file = SemanticPolicyRegressionTests.Fixture(declarations + source);
            if (ArchitectureTests.HudKernelSourceViolations([file]).Any() != rejected)
                throw new InvalidOperationException("HUD source boundary regression: " + source);
        }
    }
}

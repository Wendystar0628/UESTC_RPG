using Campus.Audio.Core;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AudioSettingsTextTests
{
    [TestCase]
    public void ConstructorTokensWithWhitespaceAreRejectedBeforeVariantParsing()
    {
        foreach (var token in new[] { "Object", "Resource", "SubResource", "ExtResource" })
            foreach (var whitespace in new[] { "", " ", "\t", "\r\n", " \n\t " })
                if (!AudioSettingsText.ContainsObjectConstruction("[audio]\nunknown=" + token + whitespace + "(RefCounted,)"))
                    throw new InvalidOperationException("Unsafe constructor token passed the text guard.");
        if (AudioSettingsText.ContainsObjectConstruction("[audio]\nmaster=0.8\nui_sounds=true\noutput_device=\"Default\"\n"))
            throw new InvalidOperationException("Primitive settings were rejected.");
    }
}

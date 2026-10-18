using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Campus.Contracts;

/// <summary>CPU metadata source. Its presence does not imply that extension files or a GPU palette are ready.</summary>
public interface IPaletteCatalogSource
{
    PaletteCatalog Catalog { get; }
}

using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core.Library;

/// <summary>Allocates the shell's openings and roof glazing in their shared window grid.</summary>
internal sealed class LibraryOpeningLayout
{
    private readonly LibraryPlan _plan;

    public LibraryOpeningLayout(LibraryPlan plan)
    {
        _plan = plan;
        var next = new int[LibraryShell.Rows];
        int Allocate(int cells, IEnumerable<int> rows)
        {
            var list = rows.ToArray();
            var column = list.Max(r => next[r]);
            foreach (var r in list) next[r] = column + cells;
            return column;
        }
        var openings = new List<Opening>();
        foreach (var piece in plan.Pieces)
            openings.AddRange(Layout(piece, Allocate));
        Openings = openings;
        SkylightColumn = Allocate(1, [5]);
        MechanicalWindows = LibraryRoof.MechWindows(plan).Select(w => (w.Face, w.Hole, Allocate(1, [5]))).ToArray();
        Columns = next.Max();
    }

    public IReadOnlyList<Opening> Openings { get; }
    public IReadOnlyList<(Face Face, Rect Hole, int Column)> MechanicalWindows { get; }
    public int SkylightColumn { get; }
    public int Columns { get; }

    private IEnumerable<Opening> Layout(WallPiece piece, Func<int, IEnumerable<int>, int> allocate)
    {
        var f = Face.Of(piece);
        var length = piece.Length;
        if (length < S.MinWindowFace) return [];
        return piece.Block.Kind switch
        {
            BlockKind.Podium => Podium(piece, f, allocate),
            BlockKind.Wing => Ribbons(piece, f, allocate),
            BlockKind.Setback => Squares(piece, f, allocate),
            BlockKind.Tower when length >= 8f && piece.Y1 - piece.Y0 > 6f => Slits(piece, f, allocate),
            BlockKind.FrontTower when length >= 8f && piece.Y1 - piece.Y0 > 6f => Slot(piece, f, allocate),
            BlockKind.Tower or BlockKind.FrontTower or BlockKind.Core => [],
            BlockKind.Annex or BlockKind.Neighbour => throw new InvalidOperationException("The north blocks (16e) are not part of the library plan."),
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece.Block.Kind, "Unknown block kind.")
        };
    }

    private IEnumerable<Opening> Podium(WallPiece piece, Face f, Func<int, IEnumerable<int>, int> allocate)
    {
        var plan = _plan;
        var length = piece.Length;
        float y0 = plan.FloorY + S.PodiumSill, y1 = y0 + S.PodiumHeight;
        if (piece.Y0 > plan.FloorY + 0.01f || piece.Y1 < y1 + 0.3f || length < 2 * S.RibbonMargin + S.PodiumModule) yield break;
        float u0 = S.RibbonMargin, u1 = length - S.RibbonMargin;
        // lib.cafe (16d): the end of the ribbon at the store front becomes a door bay from the floor up, the door
        // open to the ribbon's transom line, its light above; a pier, then the rest of the ribbon.
        if (plan.CafeFace is { } cafe && ReferenceEquals(cafe.Piece, piece) && length > 2 * S.RibbonMargin + (S.CafeDoorCells + 2) * S.PodiumModule + S.CafeDoorPier)
        {
            var width = S.CafeDoorCells * S.PodiumModule;
            var bay = cafe.AtA ? new Rect(u0, u0 + width, plan.FloorY, y1) : new Rect(u1 - width, u1, plan.FloorY, y1);
            yield return new Opening(f, OpeningKind.Entry, bay, S.CafeDoorCells, [(plan.FloorY, y1, 0)], allocate(S.CafeDoorCells, [0]), S.CardReading)
            { Door = (0, S.CafeDoorCells, y0 + S.PodiumTransom) };
            if (cafe.AtA) u0 = bay.U1 + S.CafeDoorPier; else u1 = bay.U0 - S.CafeDoorPier;
        }
        var cells = Math.Max(1, (int)MathF.Round((u1 - u0) / S.PodiumModule));
        yield return new(f, OpeningKind.Podium, new(u0, u1, y0, y1), cells, [(y0, y1, 0)], allocate(cells, [0]), S.CardReading);
    }

    private IEnumerable<Opening> Ribbons(WallPiece piece, Face f, Func<int, IEnumerable<int>, int> allocate)
    {
        var plan = _plan;
        var length = piece.Length;
        if (length < 2 * S.RibbonMargin + S.RibbonModule) yield break;
        for (var k = 0; k < S.LibraryStoreys.Length - 1; k++)
        {
            float y0 = plan.StoreyY(k) + S.RibbonSill, y1 = y0 + S.RibbonHeight;
            if (plan.StoreyY(k) < piece.Y0 - 0.05f || y1 > piece.Y1 - 0.3f) continue;
            var cells = Math.Max(1, (int)MathF.Round((length - 2 * S.RibbonMargin) / S.RibbonModule));
            yield return new(f, OpeningKind.Ribbon, new(S.RibbonMargin, length - S.RibbonMargin, y0, y1), cells, [(y0, y1, k)], allocate(cells, [k]), S.CardReading);
        }
    }

    private IEnumerable<Opening> Squares(WallPiece piece, Face f, Func<int, IEnumerable<int>, int> allocate)
    {
        var plan = _plan;
        var length = piece.Length;
        var row = S.LibraryStoreys.Length - 1;
        var count = (int)MathF.Floor((length - 1.8f + (S.SquarePitch - S.SquareSize)) / S.SquarePitch);
        float y0 = plan.StoreyY(row) + S.SquareSill, y1 = y0 + S.SquareSize;
        for (var i = 0; i < count; i++)
        {
            var u = length * 0.5f + (i - (count - 1) * 0.5f) * S.SquarePitch;
            yield return new(f, OpeningKind.Square, new(u - S.SquareSize * 0.5f, u + S.SquareSize * 0.5f, y0, y1), 1, [(y0, y1, row)], allocate(1, [row]), S.CardReading);
        }
    }

    private IEnumerable<Opening> Slits(WallPiece piece, Face f, Func<int, IEnumerable<int>, int> allocate)
    {
        var plan = _plan;
        var length = piece.Length;
        var count = Math.Min(S.TowerSlits, (int)MathF.Floor((length - 2f) / S.SlitPitch) + 1);
        float y0 = MathF.Max(piece.Y0, plan.SecondFloorY) + 0.9f, y1 = piece.Y1 - S.SlitTopGap;
        var rows = StoreyRows(y0, y1);
        for (var i = 0; i < count; i++)
        {
            var u = length * 0.5f + (i - (count - 1) * 0.5f) * S.SlitPitch;
            yield return new(f, OpeningKind.Slit, new(u - S.SlitWidth * 0.5f, u + S.SlitWidth * 0.5f, y0, y1), 1, rows, allocate(1, rows.Select(r => r.Row)), S.CardStair);
        }
    }

    private IEnumerable<Opening> Slot(WallPiece piece, Face f, Func<int, IEnumerable<int>, int> allocate)
    {
        var plan = _plan;
        var length = piece.Length;
        float y0 = MathF.Max(piece.Y0, plan.SecondFloorY), y1 = piece.Y1 - S.SlotTopGap;
        var rows = StoreyRows(y0, y1);
        var cells = S.SlotMullions + 1;
        // The 2F front door (16c) opens the slot's middle cells below the door transom.
        var door = MathF.Abs(y0 - plan.SecondFloorY) < 0.01f ? ((cells - S.DoorCells) / 2, S.DoorCells, y0 + S.SlotDoorHeight) : ((int, int, float)?)null;
        var slot = new Opening(f, OpeningKind.Slot, new((length - S.SlotWidth) * 0.5f, (length + S.SlotWidth) * 0.5f, y0, y1), cells, rows,
            allocate(cells, rows.Select(r => r.Row)), S.CardReading);
        yield return slot with { Door = door };
    }

    /// <summary>The span y0…y1 cut at the storey floors it crosses, each part tagged with its storey (window row).</summary>
    private IReadOnlyList<(float Y0, float Y1, int Row)> StoreyRows(float y0, float y1)
    {
        var list = new List<(float, float, int)>();
        for (var k = 0; k < LibraryShell.Rows; k++)
        {
            float a = MathF.Max(y0, _plan.StoreyY(k)), b = MathF.Min(y1, _plan.StoreyY(k + 1));
            if (b - a > 0.3f) list.Add((a, b, k));
        }
        return list;
    }

}

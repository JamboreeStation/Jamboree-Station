using Robust.Shared.Serialization;

namespace Content.Shared._Jamboree.Mutations.GeneticMachines;

// ---------- Common scanner-status block shared by all genetic consoles ----------

[Serializable, NetSerializable]
public enum GeneticMachineScannerStatus : byte
{
    NoScannerLinked,
    ScannerOutOfRange,
    ScannerEmpty,
    ScannerOccupied,
}

// ---------- Mutator ----------

[Serializable, NetSerializable]
public enum MutatorUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class MutatorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly GeneticMachineScannerStatus Status;
    public readonly string OccupantName;
    public readonly bool Busy;
    /// <summary>
    ///     True when the scanner occupant already carries at least one mutation.
    ///     The Mutator refuses to operate in this case; the UI uses this flag to
    ///     disable the Activate button and surface a "subject already mutated"
    ///     message to the geneticist.
    /// </summary>
    public readonly bool OccupantAlreadyMutated;
    public readonly TimeSpan? StartedAt;
    public readonly TimeSpan? FinishesAt;
    public MutatorBoundUserInterfaceState(GeneticMachineScannerStatus status, string occupantName, bool busy,
        bool occupantAlreadyMutated, TimeSpan? startedAt = null, TimeSpan? finishesAt = null)
    {
        Status = status;
        OccupantName = occupantName;
        Busy = busy;
        OccupantAlreadyMutated = occupantAlreadyMutated;
        StartedAt = startedAt;
        FinishesAt = finishesAt;
    }
}

[Serializable, NetSerializable]
public sealed class MutatorActivateMessage : BoundUserInterfaceMessage { }

// ---------- Evolutionizer ----------

[Serializable, NetSerializable]
public enum EvolutionizerUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class EvolutionizerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly GeneticMachineScannerStatus Status;
    public readonly string OccupantName;
    public readonly bool Busy;
    public readonly bool Emagged;
    public readonly string CurrentDnaFingerprint;
    /// <summary>
    ///     True when an occupant is present but the Evolutionizer would refuse
    ///     to operate on them (non-humanoid, or a humanoid that isn't a
    ///     monkey/kobold and the machine isn't emagged).
    /// </summary>
    public readonly bool OccupantCannotEvolve;
    /// <summary>
    ///     True when the machine is emagged with a non-empty DNA fingerprint
    ///     that does not match any humanoid currently on the server.
    /// </summary>
    public readonly bool DnaDonorNotFound;
    public readonly TimeSpan? StartedAt;
    public readonly TimeSpan? FinishesAt;
    public EvolutionizerBoundUserInterfaceState(GeneticMachineScannerStatus status, string occupantName, bool busy,
        bool emagged, string currentDnaFingerprint, bool occupantCannotEvolve, bool dnaDonorNotFound,
        TimeSpan? startedAt = null, TimeSpan? finishesAt = null)
    {
        Status = status;
        OccupantName = occupantName;
        Busy = busy;
        Emagged = emagged;
        CurrentDnaFingerprint = currentDnaFingerprint;
        OccupantCannotEvolve = occupantCannotEvolve;
        DnaDonorNotFound = dnaDonorNotFound;
        StartedAt = startedAt;
        FinishesAt = finishesAt;
    }
}

[Serializable, NetSerializable]
public sealed class EvolutionizerActivateMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class EvolutionizerSetDnaMessage : BoundUserInterfaceMessage
{
    public readonly string Dna;
    public EvolutionizerSetDnaMessage(string dna) { Dna = dna; }
}

// ---------- Genetic Extractor ----------

[Serializable, NetSerializable]
public enum GeneticExtractorUiKey : byte { Key }

[Serializable, NetSerializable]
public enum GeneticExtractorMode : byte
{
    Cleanse,
    Isolate,
}

[Serializable, NetSerializable]
public sealed class GeneticExtractorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly GeneticMachineScannerStatus Status;
    public readonly string OccupantName;
    public readonly bool Busy;
    public readonly GeneticExtractorMode Mode;
    public readonly List<string> OccupantMutationIds;
    public readonly string? IsolatedMutationId;
    /// <summary>Optional label that will be applied to the produced Genepack on isolation.</summary>
    public readonly string GenepackName;
    public readonly TimeSpan? StartedAt;
    public readonly TimeSpan? FinishesAt;
    public GeneticExtractorBoundUserInterfaceState(GeneticMachineScannerStatus status, string occupantName, bool busy,
        GeneticExtractorMode mode, List<string> mutations, string? isolatedMutationId, string genepackName,
        TimeSpan? startedAt = null, TimeSpan? finishesAt = null)
    {
        Status = status;
        OccupantName = occupantName;
        Busy = busy;
        Mode = mode;
        OccupantMutationIds = mutations;
        IsolatedMutationId = isolatedMutationId;
        GenepackName = genepackName;
        StartedAt = startedAt;
        FinishesAt = finishesAt;
    }
}

[Serializable, NetSerializable]
public sealed class GeneticExtractorSetModeMessage : BoundUserInterfaceMessage
{
    public readonly GeneticExtractorMode Mode;
    public GeneticExtractorSetModeMessage(GeneticExtractorMode mode) { Mode = mode; }
}

[Serializable, NetSerializable]
public sealed class GeneticExtractorSetIsolatedMessage : BoundUserInterfaceMessage
{
    public readonly string? MutationId;
    public GeneticExtractorSetIsolatedMessage(string? mutationId) { MutationId = mutationId; }
}

[Serializable, NetSerializable]
public sealed class GeneticExtractorActivateMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class GeneticExtractorSetGenepackNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;
    public GeneticExtractorSetGenepackNameMessage(string name) { Name = name; }
}

// ---------- Recombiner ----------

[Serializable, NetSerializable]
public enum RecombinerUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class RecombinerLoadedGenepack
{
    public NetEntity Entity;
    public string Label = string.Empty;
    public List<string> MutationIds = new();
    public RecombinerLoadedGenepack() { }
}

[Serializable, NetSerializable]
public sealed class RecombinerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<RecombinerLoadedGenepack> LoadedGenepacks;
    /// <summary>Genepacks currently checked for inclusion.</summary>
    public readonly List<NetEntity> SelectedPacks;
    /// <summary>Deduplicated union of mutation ids contributed by the selected packs.</summary>
    public readonly List<string> UnionMutationIds;
    public readonly int Complexity;
    public readonly int ComplexityCap;
    public readonly int MetabolicEfficiency;
    public readonly int PositiveCount;
    public readonly int NegativeCount;
    public readonly bool Balanced;
    public readonly bool Busy;
    public readonly string GenotypeName;
    public readonly TimeSpan? StartedAt;
    public readonly TimeSpan? FinishesAt;
    public RecombinerBoundUserInterfaceState(List<RecombinerLoadedGenepack> loaded,
        List<NetEntity> selectedPacks, List<string> unionMutationIds,
        int complexity, int complexityCap, int metabolicEfficiency, int positive, int negative, bool balanced, bool busy, string genotype,
        TimeSpan? startedAt = null, TimeSpan? finishesAt = null)
    {
        LoadedGenepacks = loaded;
        SelectedPacks = selectedPacks;
        UnionMutationIds = unionMutationIds;
        Complexity = complexity;
        ComplexityCap = complexityCap;
        MetabolicEfficiency = metabolicEfficiency;
        PositiveCount = positive;
        NegativeCount = negative;
        Balanced = balanced;
        StartedAt = startedAt;
        FinishesAt = finishesAt;
        Busy = busy;
        GenotypeName = genotype;
    }
}

[Serializable, NetSerializable]
public sealed class RecombinerTogglePackMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Genepack;
    public RecombinerTogglePackMessage(NetEntity genepack) { Genepack = genepack; }
}

[Serializable, NetSerializable]
public sealed class RecombinerSetGenotypeNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;
    public RecombinerSetGenotypeNameMessage(string name) { Name = name; }
}

[Serializable, NetSerializable]
public sealed class RecombinerStartMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class RecombinerEjectMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Genepack;
    public RecombinerEjectMessage(NetEntity gp) { Genepack = gp; }
}

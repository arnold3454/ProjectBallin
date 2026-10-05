using System;

/// <summary>One upgrade the player can be offered: what it says and how to stack it.</summary>
public sealed class UpgradeDefinition
{
    public readonly string Name;
    public readonly string Description;

    /// <summary>0 means the upgrade can be picked any number of times.</summary>
    public readonly int MaxStacks;

    private readonly Func<int, string> totalAtStacks;
    private readonly Action<UpgradeManager> apply;

    public UpgradeDefinition(string name, string description, Func<int, string> totalAtStacks, Action<UpgradeManager> apply, int maxStacks = 0)
    {
        Name = name;
        Description = description;
        this.totalAtStacks = totalAtStacks;
        this.apply = apply;
        MaxStacks = maxStacks;
    }

    public bool IsMaxed(int stacks) => MaxStacks > 0 && stacks >= MaxStacks;

    /// <summary>The overall bonus after the given number of picks, e.g. "+25%".</summary>
    public string TotalAt(int stacks) => totalAtStacks(stacks);

    public void Apply(UpgradeManager manager) => apply(manager);
}

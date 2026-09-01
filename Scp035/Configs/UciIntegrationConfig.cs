using System.Collections.Generic;
using System.ComponentModel;

namespace Scp035.Configs;

public sealed class UciIntegrationConfig
{
    [Description("Whether the UncomplicatedCustomItems integration is enabled.\n# When enabled, SCP-035 is stopped from picking up UCI custom items.\n# Has no effect if UncomplicatedCustomItems is not installed.")]
    public bool Enabled { get; set; } = true;

    [Description("Whether SCP-035 is blocked from picking up EVERY UCI custom item.\n# When true, only the ids listed in 'allowed_custom_item_ids' can still be picked up.\n# When false, only the ids listed in 'blocked_custom_item_ids' are blocked.")]
    public bool BlockAllCustomItems { get; set; } = true;

    [Description("Custom item ids SCP-035 must not be able to pick up.\n# Only used when 'block_all_custom_items' is false.")]
    public List<uint> BlockedCustomItemIds { get; set; } = [];

    [Description("Custom item ids SCP-035 is still allowed to pick up.\n# Only used when 'block_all_custom_items' is true.")]
    public List<uint> AllowedCustomItemIds { get; set; } = [];

    [Description("Hint shown to SCP-035 when a custom item pickup is blocked. Leave empty to show nothing.")]
    public string BlockedHint { get; set; } = "<color=#C50000>SCP-035</color> cannot carry {item}.";

    [Description("How long (in seconds) the blocked pickup hint stays on screen.")]
    public float BlockedHintDuration { get; set; } = 2f;

    internal bool IsBlocked(uint customItemId)
    {
        return BlockAllCustomItems ? !AllowedCustomItemIds.Contains(customItemId) : BlockedCustomItemIds.Contains(customItemId);
    }
}
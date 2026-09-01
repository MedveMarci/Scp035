using System;
using System.Reflection;
using LabApi.Features.Wrappers;
using Scp035.ApiFeatures;
using Scp035.Configs;

namespace Scp035.Integrations;

internal static class UciIntegration
{
    private const string PluginName = "UncomplicatedCustomItems";

    private const string SummonedCustomItem = "UncomplicatedCustomItems.API.Features.SummonedCustomItem";

    private const string CustomItem = "UncomplicatedCustomItems.API.Interfaces.ICustomItem";

    private const string SummonedApiCustomItem = "UncomplicatedCustomItems.API.Features.CustomItemAPI.SummonedAPICustomItem";

    private const string ApiCustomItem = "UncomplicatedCustomItems.API.Features.CustomItemAPI.APICustomItem";

    private static readonly string[] SerialParameter = ["serial"];

    private static MethodInfo _getSummoned;
    private static MethodInfo _readSummonedItem;
    private static MethodInfo _itemId;
    private static MethodInfo _itemName;

    private static MethodInfo _getApiSummoned;
    private static FieldInfo _readSummonedApiItem;
    private static MethodInfo _apiItemId;
    private static MethodInfo _apiItemName;

    private static bool _broken;

    internal static bool IsInstalled { get; private set; }

    internal static void Initialise()
    {
        if (IsInstalled || _broken)
            return;

        Assembly assembly = DynamicInvoke.GetLabAPIAssembly(PluginName) ?? DynamicInvoke.GetExiledAssembly(PluginName);

        if (assembly is null)
        {
            LogManager.Debug("UncomplicatedCustomItems was not found, the integration stays inactive.");
            return;
        }

        try
        {
            _getSummoned = DynamicInvoke.GetMethod(PluginName, $"{SummonedCustomItem}.Get", methodCounter: 1, requiredParamNames: SerialParameter);
            _readSummonedItem = DynamicInvoke.GetMethod(PluginName, $"{SummonedCustomItem}.CustomItem_get");
            _itemId = DynamicInvoke.GetMethod(PluginName, $"{CustomItem}.Id_get");
            _itemName = DynamicInvoke.GetMethod(PluginName, $"{CustomItem}.Name_get");

            _getApiSummoned = DynamicInvoke.GetMethod(PluginName, $"{SummonedApiCustomItem}.Get", methodCounter: 1, requiredParamNames: SerialParameter);
            _readSummonedApiItem = assembly.GetType(SummonedApiCustomItem, false)?.GetField("CustomItem", BindingFlags.Public | BindingFlags.Instance);
            _apiItemId = DynamicInvoke.GetMethod(PluginName, $"{ApiCustomItem}.Id_get");
            _apiItemName = DynamicInvoke.GetMethod(PluginName, $"{ApiCustomItem}.Name_get");

            bool yamlItemsSupported = _getSummoned is not null && _readSummonedItem is not null && _itemId is not null;
            bool apiItemsSupported = _getApiSummoned is not null && _readSummonedApiItem is not null && _apiItemId is not null;

            if (!yamlItemsSupported && !apiItemsSupported)
            {
                _broken = true;
                LogManager.Warn("UncomplicatedCustomItems was found but its custom item API could not be read. The integration stays inactive - update Scp035 or UncomplicatedCustomItems.");
                return;
            }

            IsInstalled = true;
            LogManager.Info("UncomplicatedCustomItems integration is active.");
        }
        catch (Exception exception)
        {
            _broken = true;
            IsInstalled = false;
            LogManager.Error($"Failed to set up the UncomplicatedCustomItems integration.\n{exception}");
        }
    }

    internal static bool IsPickupBlocked(Pickup pickup, out string itemName)
    {
        itemName = null;

        if (!IsInstalled || pickup is null)
            return false;

        UciIntegrationConfig settings = Scp035.Singleton.Config.UciIntegration;
        if (!settings.Enabled)
            return false;

        if (!TryResolveCustomItem(pickup.Serial, out uint id, out itemName))
            return false;

        if (!settings.IsBlocked(id))
            return false;

        LogManager.Debug($"Blocked SCP-035 from picking up custom item {id} ({itemName ?? "unnamed"}).");

        return true;
    }

    internal static bool TryResolveCustomItem(ushort serial, out uint id, out string name)
    {
        id = 0;
        name = null;

        if (!IsInstalled)
            return false;

        object[] arguments = new object[] { serial };

        try
        {
            object summoned = _getSummoned?.Invoke(null, arguments);
            object definition = summoned is null ? null : _readSummonedItem?.Invoke(summoned, null);
            if (definition is not null && TryRead(definition, _itemId, _itemName, out id, out name))
                return true;

            summoned = _getApiSummoned?.Invoke(null, arguments);
            definition = summoned is null ? null : _readSummonedApiItem?.GetValue(summoned);
            return definition is not null && TryRead(definition, _apiItemId, _apiItemName, out id, out name);
        }
        catch (Exception exception)
        {
            _broken = true;
            IsInstalled = false;
            LogManager.Error($"The UncomplicatedCustomItems integration failed and was turned off.\n{exception}");
            return false;
        }
    }

    private static bool TryRead(object definition, MethodInfo idGetter, MethodInfo nameGetter, out uint id, out string name)
    {
        id = 0;
        name = null;

        if (idGetter?.Invoke(definition, null) is not uint value)
            return false;

        id = value;
        name = nameGetter?.Invoke(definition, null) as string;
        return true;
    }
}
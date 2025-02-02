using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using System.Drawing;
using System.Runtime.InteropServices;

public partial class Plugin : BasePlugin, IPluginConfig<Config>
{
    public void RegisterEvents()
    {
        RegisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        RegisterEventHandler<EventRoundStart>(EventRoundStart);
        RegisterEventHandler<EventPlayerTeam>(EventPlayerTeam);

        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(OnCanUse, HookMode.Pre);

        HookEntityOutput("trigger_multiple", "OnStartTouch", OnStartTouch, HookMode.Pre);
    }

    public void UnregisterEvents()
    {
        DeregisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        DeregisterEventHandler<EventRoundStart>(EventRoundStart);
        DeregisterEventHandler<EventPlayerTeam>(EventPlayerTeam);

        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Unhook(OnCanUse, HookMode.Pre);

        UnhookEntityOutput("trigger_multiple", "OnStartTouch", OnStartTouch, HookMode.Pre);
    }

    public Dictionary<CCSPlayerController, List<TeleportPair>> playerCooldowns = new Dictionary<CCSPlayerController, List<TeleportPair>>();

    HookResult OnStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        if (activator.DesignerName != "player") return HookResult.Continue;

        var pawn = activator.As<CCSPlayerPawn>();

        if (!pawn.IsValid) return HookResult.Continue;
        if (!pawn.Controller.IsValid || pawn.Controller.Value is null) return HookResult.Continue;

        var player = pawn.Controller.Value.As<CCSPlayerController>();

        if (!playerCooldowns.ContainsKey(player))
            playerCooldowns.Add(player, new List<TeleportPair>());

        if (player.IsBot) return HookResult.Continue;

        if (Teleports.Triggers.TryGetValue(caller, out CEntityInstance? teleport))
        {
            var pair = Teleports.teleportPairs.FirstOrDefault(pair => pair.Entry.Entity == teleport || pair.Exit.Entity == teleport);

            if (pair != null && pair.Entry != null && pair.Exit != null)
            {
                if (playerCooldowns[player].Contains(pair))
                    return HookResult.Continue;

                if (teleport.Entity!.Name.StartsWith("teleport_entry"))
                {
                    player.PlayerPawn.Value!.Teleport(pair.Exit.Entity.AbsOrigin);
                }

                playerCooldowns[player].Add(pair);

                AddTimer(0.5f, () => {
                    if (player != null && player.IsValid)
                        playerCooldowns[player].Remove(pair);
                });

            }
        }

        return HookResult.Continue;
    }

    private static readonly MemoryFunctionWithReturn<nint, string, int, int> SetBodygroupFunc = new(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "55 48 89 E5 41 56 49 89 F6 41 55 41 89 D5 41 54 49 89 FC 48 83 EC 08" : "40 53 41 56 41 57 48 81 EC 90 00 00 00 0F 29 74 24 70");
    private static readonly Func<nint, string, int, int> SetBodygroup = SetBodygroupFunc.Invoke;
    public void HidePlayer(CCSPlayerController player, bool status)
    {
        var pawn = player.PlayerPawn.Value;

        pawn!.Render = status ? Color.FromArgb(0, 0, 0, 0) : Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        var gloves = pawn.EconGloves;
        SetBodygroup(pawn.Handle, "default_gloves", status ? 0 : 1);
    }

    HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (RediePlayers.Contains(player.Slot))
            HidePlayer(player, false);

        return HookResult.Continue;
    }

    HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Teleports.ReplacePortals();

        RediePlayers.Clear();

        return HookResult.Continue;
    }

    HookResult EventPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (RediePlayers.Contains(player.Slot))
        {
            HidePlayer(player, false);
            RediePlayers.Remove(player.Slot);
        }

        return HookResult.Continue;
    }

    HookResult OnCanUse(DynamicHook hook)
    {
        var weaponservices = hook.GetParam<CCSPlayer_WeaponServices>(0);

        var player = new CCSPlayerController(weaponservices.Pawn.Value.Controller.Value!.Handle);

        if (RediePlayers.Contains(player.Slot))
        {
            hook.SetReturn(false);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }
}
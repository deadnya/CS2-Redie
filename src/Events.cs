using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using System;
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
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(TeleportHandler, HookMode.Pre);
    }

    public void UnregisterEvents()
    {
        DeregisterEventHandler<EventPlayerSpawn>(EventPlayerSpawn);
        DeregisterEventHandler<EventRoundStart>(EventRoundStart);
        DeregisterEventHandler<EventPlayerTeam>(EventPlayerTeam);

        VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Unhook(OnCanUse, HookMode.Pre);
        VirtualFunctions.CBaseTrigger_StartTouchFunc.Unhook(TeleportHandler, HookMode.Pre);
    }

    public Dictionary<CTriggerMultiple, Vector> teleportsList = new Dictionary<CTriggerMultiple, Vector>();

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
        var teleports = Utilities.FindAllEntitiesByDesignerName<CTriggerTeleport>("trigger_teleport");
        var destinations = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_teleport_destination");

        foreach (var teleport in teleports)
        {
            var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");

            if (trigger != null && trigger.IsValid)
            {
                trigger.Entity!.Name = Config.RedactedTeleportName + ';' + teleport.Target;
                trigger.Spawnflags = 1;
                trigger.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
                trigger.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
                trigger.Collision.SolidFlags = 0;
                trigger.Collision.CollisionGroup = 2;
                trigger.Collision.CollisionAttribute.CollisionGroup = 2;
                trigger.SetModel(teleport.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                trigger.DispatchSpawn();
                trigger.Teleport(teleport.AbsOrigin, teleport.AbsRotation);
                trigger.AcceptInput("Enable");

                foreach (var destination in destinations)
                {
                    if (destination.Entity!.Name == teleport.Target)
                    {
                        teleportsList.Add(trigger, destination.AbsOrigin!);
                    }
                }

                teleport.Remove();
            }
        }

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

    HookResult TeleportHandler(DynamicHook hook)
    {
        var entity = hook.GetParam<CBaseEntity>(1);
        if (!entity.IsValid || entity.DesignerName != "player") return HookResult.Continue;

        var player = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
        var trigger = hook.GetParam<CTriggerMultiple>(0);

        var name = trigger.Entity!.Name.Split(';')[0];

        if (name == Config.RedactedTeleportName)
        {
            if (teleportsList.TryGetValue(trigger, out Vector? value)) player.PlayerPawn.Value!.Teleport(value);
        }

        hook.SetReturn(false);
        return HookResult.Handled;
    }
}
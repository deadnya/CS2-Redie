using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace Redie
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {
        private void RegisterEvents()
        {
            RegisterEventHandler<EventPlayerSpawn>(OnEventPlayerSpawn);
            RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
            RegisterEventHandler<EventPlayerTeam>(OnEventPlayerTeam);
            RegisterEventHandler<EventPlayerDeath>(OnEventPlayerDeath);

            VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Hook(OnCanUse, HookMode.Pre);
            VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(OnTriggerStartTouch, HookMode.Pre);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

            HookUserMessage(208, CMsgSosStartSoundEvent, HookMode.Pre);
        }

        private void UnregisterEvents()
        {
            DeregisterEventHandler<EventPlayerSpawn>(OnEventPlayerSpawn);
            DeregisterEventHandler<EventRoundStart>(OnEventRoundStart);
            DeregisterEventHandler<EventPlayerTeam>(OnEventPlayerTeam);
            DeregisterEventHandler<EventPlayerDeath>(OnEventPlayerDeath);

            VirtualFunctions.CCSPlayer_WeaponServices_CanUseFunc.Unhook(OnCanUse, HookMode.Pre);
            VirtualFunctions.CBaseTrigger_StartTouchFunc.Unhook(OnTriggerStartTouch, HookMode.Pre);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

            UnhookUserMessage(208, CMsgSosStartSoundEvent, HookMode.Pre);
        }

        private readonly Dictionary<CTriggerMultiple, Vector> teleportsList = [];

        public static void HidePlayer(CCSPlayerController player, bool status)
        {
            var pawn = player.PlayerPawn.Value;

            pawn!.Render = status ? Color.FromArgb(0, 0, 0, 0) : Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        }

        HookResult CMsgSosStartSoundEvent(UserMessage um)
        {
            int entIndex = um.ReadInt("source_entity_index");

            var entHandle = NativeAPI.GetEntityFromIndex(entIndex);

            var pPawn = new CBasePlayerPawn(entHandle);
            if (pPawn == null || !pPawn.IsValid) return HookResult.Continue;

            var pController = pPawn.Controller?.Value?.As<CCSPlayerController>();
            if (pController == null || !pController.IsValid) return HookResult.Continue;

            if (RediePlayers.Contains(pController.Slot))
            {
                var players = Utilities.GetPlayers();
                foreach (var player in players)
                {
                    if (!player.IsValid || player.Connected != PlayerConnectedState.PlayerConnected)
                        continue;

                    if (player.Slot == pController.Slot)
                        continue;

                    _ = um.Recipients.Remove(player);
                }
            }

            return HookResult.Continue;
        }


        HookResult OnEventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (player == null || !player.IsValid)
                return HookResult.Continue;

            if (RediePlayers.Contains(player.Slot))
                HidePlayer(player, false);

            return HookResult.Continue;
        }

        HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            ReplaceMapTeleports();

            RediePlayers.Clear();

            return HookResult.Continue;
        }

        HookResult OnEventPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
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

        HookResult OnEventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var player = @event.Userid;

            if (player == null || !player.IsValid)
                return HookResult.Continue;

            if (Config.SendInfoMessages)
                player.PrintToChat($"{Config.Prefix} {Config.Message_PlayerDeath}");

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

        HookResult OnTriggerStartTouch(DynamicHook hook)
        {
            var entity = hook.GetParam<CBaseEntity>(1);
            if (!entity.IsValid || entity.DesignerName != "player") return HookResult.Continue;
            var player = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value!.Handle);
            var trigger = hook.GetParam<CTriggerMultiple>(0);

            if (trigger == null || trigger.Entity == null || trigger.Entity.Name == null) return HookResult.Continue;

            var name = trigger.Entity!.Name.Split(';')[0];

            if (name == Config.RedactedTeleportName)
            {
                if (teleportsList.TryGetValue(trigger, out Vector? value)) player.PlayerPawn.Value!.Teleport(value);
            }

            hook.SetReturn(true);
            return HookResult.Continue;
        }

        HookResult OnTakeDamage(DynamicHook hook)
        {
            var pawn = hook.GetParam<CCSPlayerPawn>(0);
            if (pawn.DesignerName != "player") return HookResult.Continue;
            if (pawn == null) return HookResult.Continue;
            if (pawn.Controller == null) return HookResult.Continue;
            if (pawn.Controller.Value == null) return HookResult.Continue;
            var player = pawn?.Controller.Value?.As<CCSPlayerController>();

            if (player == null) return HookResult.Continue;
            if (pawn == null) return HookResult.Continue;
            if (!RediePlayers.Contains(player.Slot)) return HookResult.Continue;

            hook.SetReturn(false);
            return HookResult.Handled;
        }
    }
}
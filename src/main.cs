using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;

namespace Redie
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "Redie";
        public override string ModuleVersion => "1.3.0";
        public override string ModuleAuthor => "deadnya";
        public override string ModuleDescription => "Redie for Surf";

        private readonly HashSet<int> RediePlayers = [];

        public override void Load(bool hotReload)
        {
            RegisterEvents();

            foreach (var cmd in Config.RedieCommands.Split(','))
                AddCommand(cmd, "Redie Command", (player, command) => CommandRedie(player));

            AddCommand(Config.RedieNoclipCommand, "Toggle noclip in redie", (player, command) => CommandRedieNoclip(player));
            AddCommand(Config.RedieSaveposCommand, "Save current position in redie", (player, command) => CommandRedieSavePos(player));
            AddCommand(Config.RedieLoadposCommand, "Load saved position in redie", (player, command) => CommandRedieLoadPos(player));
            AddCommand(Config.RedieHelpCommand, "Print help message", (player, command) => CommandRedieHelp(player));

            if (hotReload)
            {
                LoadReplacedTeleportsFromMap();

                var players = Utilities.GetPlayers().Where(p => p.Team != CsTeam.None && p.Team != CsTeam.Spectator); ;

                foreach (var player in players)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn == null) continue;

                    if (pawn.LifeState == (byte)LifeState_t.LIFE_DYING)
                    {
                        RediePlayers.Add(player.Slot);
                    }
                }
            }
        }

        public override void Unload(bool hotReload)
        {
            UnregisterEvents();

            foreach (var cmd in Config.RedieCommands.Split(','))
                RemoveCommand(cmd, (player, command) => CommandRedie(player));

            RemoveCommand(Config.RedieNoclipCommand, (player, command) => CommandRedieNoclip(player));
            RemoveCommand(Config.RedieSaveposCommand, (player, command) => CommandRedieSavePos(player));
            RemoveCommand(Config.RedieLoadposCommand, (player, command) => CommandRedieLoadPos(player));
            RemoveCommand(Config.RedieHelpCommand, (player, command) => CommandRedieHelp(player));
        }

        public Config Config { get; set; } = new Config();
        public void OnConfigParsed(Config config)
        {
            Config = config;
            Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);
        }

        public void CommandRedie(CCSPlayerController? player)
        {
            if (player == null || player.PlayerPawn == null) return;
            if (!player.IsValid || player.PawnIsAlive || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            {
                player.PrintToChat($"{Config.Prefix} {Config.Message_ErrorRedie}");
                return;
            }

            var playerPawn = player.PlayerPawn.Value!;

            if (!RediePlayers.Contains(player.Slot))
            {
                RediePlayers.Add(player.Slot);

                player.Respawn();
                player.RemoveWeapons();
                HidePlayer(player, true);

                AddTimer(0.2f, () =>
                {
                    player.RemoveWeapons(); //might fix an issue with guns idk
                });

                AddTimer(Config.RedieDelay, () =>
                {
                    player.RemoveWeapons();
                    playerPawn.LifeState = (byte)LifeState_t.LIFE_DYING;
                    playerPawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                    playerPawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

                    HidePlayer(player, true);
                });

                if (Config.SendInfoMessages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_Redie}");
            }

            else if (RediePlayers.Contains(player.Slot))
            {
                RediePlayers.Remove(player.Slot);

                if (player.PlayerPawn.Value!.TeamNum == (byte)CsTeam.Terrorist)
                {
                    player.ChangeTeam(CsTeam.CounterTerrorist);
                    player.ChangeTeam(CsTeam.Terrorist);
                }

                else if (player.PlayerPawn.Value!.TeamNum == (byte)CsTeam.CounterTerrorist)
                {
                    player.ChangeTeam(CsTeam.Terrorist);
                    player.ChangeTeam(CsTeam.CounterTerrorist);
                }

                if (Config.SendInfoMessages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_UnRedie}");
            }
        }

        public void CommandRedieNoclip(CCSPlayerController? player)
        {
            if (player == null || player.PlayerPawn.Value == null) return;
            if (!RediePlayers.Contains(player.Slot) && !(player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && Config.AllowNoclipForAlive)) return;

            var pawn = player.PlayerPawn.Value;

            if (pawn.MoveType == MoveType_t.MOVETYPE_WALK)
            {
                pawn.MoveType = MoveType_t.MOVETYPE_NOCLIP;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", MoveType_t.MOVETYPE_NOCLIP);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");

                if (Config.SendInfoMessages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_NoclipOn}");
            }
            else
            {
                pawn.MoveType = MoveType_t.MOVETYPE_WALK;
                Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", MoveType_t.MOVETYPE_WALK);
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");

                if (Config.SendInfoMessages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_NoclipOff}");
            }
        }

        private readonly Dictionary<int, SavedPlayerPosition> SavedPositions = [];

        public void CommandRedieSavePos(CCSPlayerController? player)
        {
            if (player == null || player.PlayerPawn.Value == null || player.PlayerPawn.Value.AbsOrigin == null) return;
            if (!RediePlayers.Contains(player.Slot) && !(player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && Config.AllowSaveLoadPosForAlive)) return;

            var pawn = player.PlayerPawn.Value;

            SavedPositions[player.Slot] = new SavedPlayerPosition(
                new Vector(
                    pawn.AbsOrigin.X,
                    pawn.AbsOrigin.Y,
                    pawn.AbsOrigin.Z),
                new Vector(
                    pawn.AbsVelocity.X,
                    pawn.AbsVelocity.Y,
                    pawn.AbsVelocity.Z)
                );

            if (Config.SendInfoMessages)
                player.PrintToChat($"{Config.Prefix} {Config.Message_SavePos}");
        }

        public void CommandRedieLoadPos(CCSPlayerController? player)
        {
            if (player == null || player.PlayerPawn.Value == null) return;
            if (!RediePlayers.Contains(player.Slot) && !(player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE && Config.AllowSaveLoadPosForAlive)) return;

            if (SavedPositions.TryGetValue(player.Slot, out SavedPlayerPosition? playerPos))
            {
                var pawn = player.PlayerPawn.Value;

                pawn.Teleport(playerPos.AbsOrigin, velocity: playerPos.AbsVelocity);

                if (Config.SendInfoMessages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_LoadPos}");
            }
            else
            {
                if (Config.SendInfoMessages)
                    player.PrintToChat($"{Config.Prefix} {Config.Message_LoadPosError}");
            }
        }

        public void CommandRedieHelp(CCSPlayerController? player)
        {
            if (player == null || player.PlayerPawn.Value == null) return;

            player.PrintToChat($"{Config.Prefix} Available commands:");
            player.PrintToChat($"{Config.Prefix} {Config.RedieCommands.Replace(',', '/')} - Toggle ghost mode");
            player.PrintToChat($"{Config.Prefix} {Config.RedieNoclipCommand} - Toggle noclip mode");
            player.PrintToChat($"{Config.Prefix} {Config.RedieSaveposCommand} - Save current position");
            player.PrintToChat($"{Config.Prefix} {Config.RedieLoadposCommand} - Teleport to saved position");
        }
    }

    class SavedPlayerPosition(Vector absOrigin, Vector absVelocity)
    {
        public Vector AbsOrigin { get; private set; } = absOrigin;
        public Vector AbsVelocity { get; private set; } = absVelocity;
    }
}
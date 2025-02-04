using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API;

namespace Redie
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "Redie";
        public override string ModuleVersion => "1.1.1";
        public override string ModuleAuthor => "deadnya";
        public override string ModuleDescription => "Redie for Combat Surf";

        private readonly HashSet<int> RediePlayers = [];

        public override void Load(bool hotReload)
        {
            RegisterEvents();

            foreach (var cmd in Config.Commands.Split(','))
                AddCommand(cmd, "Redie Command", (player, command) => CommandRedie(player));

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

            foreach (var cmd in Config.Commands.Split(','))
                RemoveCommand(cmd, (player, command) => CommandRedie(player));
        }

        public Config Config { get; set; } = new Config();
        public void OnConfigParsed(Config config)
        {
            Config = config;
            Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);
        }

        public void CommandRedie(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.PawnIsAlive || player.Team == CsTeam.Spectator || player.Team == CsTeam.None) return;
            if (player.PlayerPawn == null) return;

            var playerPawn = player.PlayerPawn.Value!;

            if (!RediePlayers.Contains(player.Slot))
            {
                RediePlayers.Add(player.Slot);

                player.Respawn();
                player.RemoveWeapons();

                HidePlayer(player, true);

                playerPawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix
                playerPawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING; //noblock fix

                //fix for custom player models
                //0.2 is for our playermodel color change plugin, feel free to change
                AddTimer(0.2f, () =>
                {
                    player.RemoveWeapons();
                    HidePlayer(player, true);


                    AddTimer(0.0f, () =>
                    {
                        playerPawn.LifeState = (byte)LifeState_t.LIFE_DYING;
                        playerPawn.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                        playerPawn.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
                    });
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
    }
}
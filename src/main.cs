using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities.Constants;

public partial class Plugin : BasePlugin, IPluginConfig<Config>
{
    public override string ModuleName => "Redie";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "deadnya";
    public override string ModuleDescription => "Redie for Combat Surf";

    public static Plugin Instance { get; set; } = new();

    HashSet<int> RediePlayers = new HashSet<int>();

    public override void Load(bool hotReload)
    {
        Instance = this;

        RegisterEvents();

        foreach (var cmd in Config.Commands.Split(','))
            AddCommand(cmd, "Redie Command", (player, command) => Command_Redie(player));
    }

    public override void Unload(bool hotReload)
    {
        UnregisterEvents();

        foreach (var cmd in Config.Commands.Split(','))
            RemoveCommand(cmd, (player, command) => Command_Redie(player));
    }

    public Config Config { get; set; } = new Config();
    public void OnConfigParsed(Config config)
    {
        Config = config;
        Config.Prefix = StringExtensions.ReplaceColorTags(config.Prefix);
    }

    public void Command_Redie(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid || player.PawnIsAlive || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

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

                AddTimer(0.0f, () => {
                    playerPawn.LifeState = (byte)LifeState_t.LIFE_DYING;
                    playerPawn.Collision.CollisionGroup = 8;
                });
            });

            if (Config.Messages)
                player.PrintToChat($"{Config.Prefix} {Config.Message_Redie}");
        }

        else if (RediePlayers.Contains(player.Slot))
        {
            RediePlayers.Remove(player.Slot);
            player.PlayerPawn.Value!.LifeState = (byte)LifeState_t.LIFE_ALIVE;
            player.CommitSuicide(false, true);

            if (Config.Messages)
                player.PrintToChat($"{Config.Prefix} {Config.Message_UnRedie}");
        }
    }
}
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

public partial class Teleports
{
    private static Plugin instance = Plugin.Instance;

    public static List<TeleportPair> teleportPairs = new List<TeleportPair>();

    private static bool isNextTeleportEntry = true;

    public static void Create(Vector portalPos, QAngle portalRot, CBaseEntity entity, string modelName)
    {
        var position = new Vector(portalPos.X, portalPos.Y, portalPos.Z);
        var rotation = new QAngle(portalRot.X, portalRot.Y, portalRot.Z);

        try
        {
            string teleportType = isNextTeleportEntry ? "entry" : "exit";
            var teleportData = CreateTeleport(position, rotation, teleportType, entity, modelName);

            if (teleportData != null)
            {
                if (!isNextTeleportEntry)
                {
                    var incompletePair = teleportPairs.FirstOrDefault(p => p.Exit == null);

                    if (incompletePair != null)
                    {
                        incompletePair.Exit = teleportData;
                    }
                    else
                    {
                        teleportPairs.Add(new TeleportPair(null!, teleportData));
                    }
                }
                else teleportPairs.Add(new TeleportPair(teleportData, null!));

                isNextTeleportEntry = !isNextTeleportEntry;
            }
        }
        catch (Exception ex)
        {
            instance.Logger.LogError($"Exception: {ex}");
        }
    }

    public static TeleportsData? CreateTeleport(Vector position, QAngle rotation, string name, CBaseEntity entity, string modelName)
    {
        var teleport = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");

        if (teleport != null && teleport.IsValid)
        {
            teleport.Entity!.Name = "teleport_" + name;
            teleport.EnableUseOutput = true;

            teleport.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            teleport.ShadowStrength = 0.0f;
            teleport.Render = new System.Drawing.Color();

            teleport.SetModel(modelName);
            
            teleport.DispatchSpawn();

            teleport.Collision.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;
            teleport.Collision.CollisionAttribute.CollisionGroup = (byte)CollisionGroup.COLLISION_GROUP_DISSOLVING;

            teleport.AcceptInput("DisableMotion", teleport, teleport);
            teleport.Teleport(position, rotation);

            CreateTrigger(teleport);

            var teleportData = new TeleportsData(
                teleport,
                name == "entry" ?
                "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl" :
                "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl",
                name
                );

            return teleportData;
        }
        else
        {
            instance.Logger.LogError("(CreateTeleport) Failed to create teleport");
            return null;
        }
    }

    public static void ReplacePortals()
    {
        var entities = Utilities.FindAllEntitiesByDesignerName<CTriggerTeleport>("trigger_teleport");
        var destinations = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_teleport_destination");

        foreach (var entity in entities)
        {
            foreach (var destination in destinations)
            {
                if (destination.Entity!.Name == entity.Target)
                {
                    Create(entity.AbsOrigin!, entity.AbsRotation!, entity, entity.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                    entity.Remove();

                    Create(destination.AbsOrigin!, destination.AbsRotation!, destination, entity.CBodyComponent.SceneNode.GetSkeletonInstance().ModelState.ModelName);
                    destination.Remove();
                }
            }
        }
    }

    public static Dictionary<CEntityInstance, CEntityInstance> Triggers = new Dictionary<CEntityInstance, CEntityInstance>();
    public static void CreateTrigger(CBaseEntity teleport)
    {
        var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");

        if (trigger != null && trigger.IsValid)
        {
            trigger.Entity!.Name = "teleport_" + teleport.Entity!.Name + "_trigger";
            trigger.Spawnflags = 1;
            trigger.CBodyComponent!.SceneNode!.Owner!.Entity!.Flags &= ~(uint)(1 << 2);
            trigger.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
            trigger.Collision.SolidFlags = 0;
            trigger.Collision.CollisionGroup = 14;

            trigger.SetModel(teleport.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
            trigger.DispatchSpawn();
            trigger.Teleport(teleport.AbsOrigin, teleport.AbsRotation);
            trigger.AcceptInput("FollowEntity", teleport, trigger, "!activator");
            trigger.AcceptInput("Enable");

            Triggers.Add(trigger, teleport);
        }

        else instance.Logger.LogError("(CreateTrigger) Failed to create trigger");
    }
}

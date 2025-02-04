using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Redie
{
    public partial class Plugin : BasePlugin, IPluginConfig<Config>
    {
        private void LoadReplacedTeleportsFromMap()
        {
            var triggers = Utilities.FindAllEntitiesByDesignerName<CTriggerMultiple>("trigger_multiple");
            var destinations = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_teleport_destination");

            foreach (var trigger in triggers)
            {
                if (trigger == null || trigger.Entity == null || trigger.Entity.Name == null) continue;

                var teleportData = trigger.Entity.Name.Split(Config.RedactedTeleportNameSeparator);

                if (teleportData[0] == Config.RedactedTeleportName)
                {
                    foreach (var destination in destinations)
                    {
                        if (destination.Entity == null) continue;
                        if (destination.AbsOrigin == null) continue;

                        if (destination.Entity!.Name == teleportData[1])
                        {
                            teleportsList.Add(trigger, destination.AbsOrigin);
                        }
                    }
                }
            }
        }

        private void ReplaceMapTeleports()
        {
            teleportsList.Clear();
            var teleports = Utilities.FindAllEntitiesByDesignerName<CTriggerTeleport>("trigger_teleport");
            var destinations = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("info_teleport_destination");

            foreach (var teleport in teleports)
            {
                var trigger = Utilities.CreateEntityByName<CTriggerMultiple>("trigger_multiple");

                if (trigger != null && trigger.IsValid)
                {
                    trigger.Entity!.Name = Config.RedactedTeleportName + Config.RedactedTeleportNameSeparator + teleport.Target;
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
        }
    }
}

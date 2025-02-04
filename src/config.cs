using CounterStrikeSharp.API.Core;

namespace Redie
{
    public class Config : BasePluginConfig
    {
        public string Prefix { get; set; } = "{purple}[Redie]{grey}";
        public string Commands { get; set; } = "css_redie,css_ghost";
        public bool SendInfoMessages { get; set; } = true;
        public string Message_Redie { get; set; } = "You are now a ghost";
        public string Message_UnRedie { get; set; } = "You are no longer a ghost";
        public string Message_PlayerDeath { get; set; } = "Type /redie to respawn as a ghost";
        public string RedactedTeleportName { get; private set; } = "redactedtpto"; //stored as redactedtpto{sep}target
        public string RedactedTeleportNameSeparator { get; private set; } = ";";
    }
}
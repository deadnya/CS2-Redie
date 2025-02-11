using CounterStrikeSharp.API.Core;

namespace Redie
{
    public class Config : BasePluginConfig
    {
        public string Prefix { get; set; } = "{purple}[Redie]{grey}";

        public string RedieCommands { get; set; } = "css_redie,css_ghost";
        public string RedieNoclipCommand { get; set; } = "css_redienoclip";
        public string RedieSaveposCommand { get; set; } = "css_rediesavepos";
        public string RedieLoadposCommand { get; set; } = "css_redieloadpos";
        public string RedieHelpCommand { get; set; } = "css_rediehelp";

        public bool AllowNoclipForAlive { get; set; } = false;
        public bool AllowSaveLoadPosForAlive { get; set; } = false;

        public bool SendInfoMessages { get; set; } = true;
        public string Message_Redie { get; set; } = "You are now a ghost";
        public string Message_UnRedie { get; set; } = "You are no longer a ghost";
        public string Message_PlayerDeath { get; set; } = "Type /redie to respawn as a ghost, /rediehelp for help";
        public string Message_ErrorRedie { get; set; } = "You must be dead and in a team to become a ghost";
        public string Message_NoclipOn { get; set; } = "Noclip: On";
        public string Message_NoclipOff { get; set; } = "Noclip: Off";
        public string Message_SavePos { get; set; } = "Position saved";
        public string Message_LoadPos { get; set; } = "Position loaded";
        public string Message_LoadPosError { get; set; } = "No saved position detected!";

        public float RedieDelay { get; set; } = 0.7f; //Seems like with this it works more stable
        public string RedactedTeleportName { get; private set; } = "redactedtpto"; //stored as redactedtpto{sep}target
        public string RedactedTeleportNameSeparator { get; private set; } = ";";
    }
}
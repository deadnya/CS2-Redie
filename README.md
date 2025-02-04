# cs2-redie
**A plugin that allows players to go in Redie/Ghost mode**

**This version is based on [excludera](https://github.com/exkludera)'s [redie](https://github.com/exkludera/cs2-redie) plugin. Its currently used on CS2 Combat Surf server (62.122.214.145:27015)**

### List of changes:
- Allows ghosts to use map teleports (replaced all trigger_teleport with trigger_multiple 💀💀💀)
- Exiting redie silently removes player instead of suiciding (just quick team rejoin)
- Ghosts don't take damage from players
- Added noclip and save/load pos for ghosts

### To do:
- Prob disable steps sounds someday

<br>

## information:

### requirements
- [MetaMod](https://github.com/alliedmodders/metamod-source)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp)


### Commands
- `css_redie` / `css_ghost` - Toggle ghost mode
- `css_redienoclip` - Toggle noclip
- `css_rediesavepos` - Save current position
- `css_redieloadpos` - Load saved position
- `css_rediehelp` - Get list of available commands

<br>

> [!CAUTION]
>player in redie still makes walking/jumping sounds (would be nice if anyone knows how to fix that)
>
> behaviour with tp entities other than teleport_trigger and info_teleport_destination was not tested and may result in unexpected stuff
> 
>player in redie can pickup defuse kits
>
>player in redie can activate triggers and hold doors (e.g. ski_3 jail cube, 4fun jail door, etc)

this plugin is still bad but it gets the job done lol

<br>

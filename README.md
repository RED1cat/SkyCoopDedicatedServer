# SkyCoop Dedicated Server
Dedicated server for the [SkyCoop](https://github.com/Filigrani/SkyCoop) mod for the game The Long Dark.

# How to build
## Windows
* First you need to install dotnet 6 and visual studio.
* Download the project, then open the project file `SkyCoopDedicatedServer.sln`.
* After opening the project, press Ctrl+ Shift+B and the project will be compiled in the Output folder.
## Linux
* First you need to install dotnet 6.
* Download the project, then open the project folder in the console and run the `dotnet build --configuration debug` command, after which the project will be compiled in the output folder.

# How to use
* To start the server, you will need to pre-install [.NET Runtime 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
* Download the latest release [here](https://github.com/RED1cat/SkyCoopDedicatedServer/releases/latest).
  * to start the server on linux, use the command `./SkyCoopDedicatedServer`
## Configuring `server.json`:
  * `SaveSlot` - Unused or outdated.
  * `ItemDupes` - Enabling or disabling duplicate items for each player.
    * `true` or `false` 
  * `ContainersDupes` - Enabling or disabling duplicate container contents for each player.
    * `true` or `false` 
  * `SpawnStyle` - Setting the spawn type.
    * `0` - Region where host is currently is but random position in limit of region.
    * `1` - Client select region for spawn.
    * `2` - Random region, random position.
    * `3` - The spawn location will be just the same at all times for everyone(including region, and position). But it is selected once when starting the server for the first time.
  * `MaxPlayers` - The maximum number of players.
  * `UsingSteam` - Unused or outdated.
  * `Ports` - The port that will be used for the server.
  * `WhiteList` - Unused or outdated.
  * `ServerName` - Server Name.
  * `Cheats` - Setting permission to use cheats.
    * `0` - No one can use cheat.
    * `1` - Only server owner can use cheat commands.
    * `2` - Any player can use cheat commands. 
  * `SteamServerAccessibility` - Unused or outdated.
  * `RCON` - Password for rcon.
  * `DropUnloadPeriod` - The period in seconds after which the discarded items will be saved.
  * `SaveScamProtection` - Protection against changes saving players before connecting to the server.
    * `true` or `false` 
  * `ModValidationCheck` - Checking for the same mods that are located in the `Mods` folder next to the server executable.
  * `ExperienceMode` - Setting the difficulty level.
    * `0` - Pilgrim
    * `1` - Voyageur
    * `2` - Stalker
    * `3` - Story
    * `4` - ChallengeRescue
    * `5` - ChallengeHunted
    * `6` - ChallengeWhiteout
    * `7` - ChallengeNomad
    * `8` - ChallengeHuntedPart2
    * `9` - Interloper
    * `10` - Custom
    * `11` - StoryFresh
    * `12` - StoryHardened
    * `13` - FourDaysOfNight
    * `14` - ChallengeArchivist
    * `15` - ChallengeDeadManWalking
    * `16` - EventWintersEmbrace
    * `17` - ChallengeNowhereToHide
    * `18` - NUM_MODES
  * `StartRegion` - Setting the starting location.
    * `0` -  LakeRegion
    * `1` - CoastalRegion
    * `2` - WhalingStationRegion
    * `3` - RuralRegion
    * `4` - CrashMountainRegion
    * `5` - MarshRegion
    * `6` - RandomRegion
    * `7` - FutureRegion
    * `8` - MountainTownRegion
    * `9` - TracksRegion
    * `10` - RiverValleyRegion
    * `11` - CanneryRegion
    * `12` - AshCanyonRegion
    * `13` - BlackrockRegion
  * `Seed` - Setting the seed.
  * `PVP` - Setting the PVP mode.
    * `true` or `false`
  * `SavingPeriod` - The period after which the server data will be saved.
  * `RestartPerioud` - Unused or outdated.
## Configuring Server bot `botconfig`:
The server has the ability to send various notifications about its status at the expense of the discord bot.
  * `token` - The bot's discord token.
  * `infochannelid` - The ID of the channel where the server will send information about itself.
  * `feedchannelid` - The ID of the channel where the server will send information about the connected players and statistics.
  * `timetoupdatemessage` - The time in minutes after which the server information will be updated.
## Server Commands:
  * `dsbot` - Sends a message via the bot's discord.
  * `botstats` - Sends the current server statistics via the bot's discord.
  * `crashsite` - Causes an airplane to crash in a random region.
  * `whencrashsite` - Displays when the next plane crash or the statistics of the current one.
  * `canclecrashsite` - Cancels the current crash site.
  * `shutdown` - Stops the server operation.
  * `ip` - It's unfinished at the moment.
  * `trafficdebug` or `traffic` or `trafictrace` or `trafficcheck` - It is used for debugging.
  * `savediag` or `unloaddiag` - It is used for debugging.
  * `players` or `playerslist` or `clients` - Display all connected players.
  * `say` - Send a global message on behalf of the server.
    * Example: `say 'text'` or `say Hello World`
  * `skip` - Skip a certain number of hours.
    * Example: `skip 'hours'` or `skip 5` 
  * `rpc` - Execute a [console command](https://the-long-dark-modding.fandom.com/wiki/Console_commands) on behalf of a certain player.
    * Example: `rpc 'player id' 'console command'` or `rpc 1 set_condition 0`  
  * `save` - Save the server data.
  * `today` or `showstats` or `stats` or `statistics` or `statistic` - Displays statistics for today.
  * `globalstats` or `global stats` or `stats global` or `statsglobal` or `alltime` - Displays global statistics.
  * `kick` - Kick out a specific player.
    * Example: `kick 'player id'` or `kick 1` 
  * `ban` - Ban a specific player.
    * Example: `ban 'player id'` or `ban 1`
  * `banmac` - Ban a specific player at the mac address.
    * Example: `banmac 'player mac address'` 
  * `unbanmac` or `unban` - Unban the player.
    * Example: `unbanmac 'player mac address'`
  * `addloottoscene` - It's unfinished at the moment.
  * `addloot` - It's unfinished at the moment.
  * `exp` - It's unfinished at the moment.
  * `exps` - It's unfinished at the moment.
  * `expsfull` - It's unfinished at the moment.
  * `next_weather` or `next weather` - Moves to the next state of the weather in the current set.
  * `next_weatherset` or `next weatherset` or `next weather set` - Moves to the next weather set.

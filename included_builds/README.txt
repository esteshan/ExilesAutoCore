ExilesAutoCore — bundled builds
================================

Drop exported profile .json files into the matching Class/Ascendancy folder below,
for example:

    included_builds/Warrior/Titan/my-pillar-leveling.json

Anything in here is zipped into the plugin DLL at build time and, when a user first
runs the plugin, copied into their builds folder:

    C:\ExileCore\config\ExilesAutoCore\builds\<Class>\<Ascendancy>\

Seeding never overwrites a file the user already has.
To create a build file: set up a profile in-game, then click "Export profile" and save
it into the matching folder here.

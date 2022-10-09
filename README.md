# SteveNES
Steve's Nintendo Emulator

I've always wanted to make an emulator. It hits so many of my interests: music, programming, video games, following rules and solving puzzles. I didn't feel programming something like that was quite in reach until I saw javidx's YouTube video series on making a NES emulator. He did great at breaking it down enough to feel like it was something I could tackle.

My goal here is to niavely follow javidx's videos and make what he made. Then I'd like to go back and make it my own in some way - even if it's just cleaning up the code and writing it how I would like to see it along with comments in my own words to seal in the learning.

What I'd like to implement
* Sound
* ~~USB Controller~~
* Illegal Op Codes
* Reasonable mappers, I want to emulate my favorite games
  * ~~Mario 2 & 3 (Mapper 4)~~
  * Castlevania 1-3 (Mapper 2, 1, 5) 1 & 2 done!
  * ~~Mega Man 2 & 3 (Mapper 1, 4)~~
  * Metalstorm (mapper 4)
  * ~~Duck Tails (mapper 2)~~
  * ~~Final Fantasy (Mapper 1)~~
  * ~~Guardian Legend (mapper 2)~~
  * ~~Metroid (mapper 1)~~
  * ~~Zelda 1 and 2 (mapper 1)~~
  * ~~Ninja Gaiden 1-3 (mappers 1 and 4)~~
  * Star Tropics (mapper 4)

I'm on a Mac using Visual Studio 2022 - I'm using **C# dotnet 6**. For graphics, I'm using the SDL library with the C# bindings provided.

I have a "Display Engine" project that is sort of javidx's Pixel Game Engine, or it does the task. It will handle game engine tasks, graphics and sound, and provide a way for the NES to do those things.

**Update**
* CPU Done and tested (legal codes only)
* PPU Done
* Mapper 000 games are now playable, sprites and sprite zero detection added
    * Tested with: Super Mario Bros, Donkey Kong, 1942, Excitebike, Kung Fu 
* Mapper 002 and 003 added!
    * Tested 002 with: Castlevania 1, DuckTales, Guardian Legend
    * Tested 003 with: Gradius, Mickey Mousecapade
* Mapper 001 added
    * Tested Legend of Zelda, Tetris, Zelda II, Castlevania II, Mega Man 2, Final Fantasy, Metroid, Ninja Gaiden, Adventures in the Magic Kingdom, Chip 'n Dale Rescue Rangers, Monster Party
    * All games have palette issues, zelda freezes, many of these unplayable right now
* Made fixes to Mappers 001 and 004, more playable games
* Added saving for games with battery - saving to romname.sav
* Joystick/Gamepad working, specifcally set up for mine, but working with XBox One controller :)

**Next items**
* Sound
* GUI update - load rom (select rom folder), see rom information, try for separate windows or change existing win dims
* Mapper 005 - Casvan III
* Mapper 007 - Battletoads, Wizards and Warriors 1-3, Marble Madness, Solar Jetman, RC ProAm, 
* Mapper 009 - Mike Tyson!
* CPU Illegal op codes (not sure if this is worth it or just move on to next emu)

**Issues**
* Noticing edge of screen artifacts that probably should not be there.
* Mapper 001
    * Zelda II calls out of bounds memory
    * Not passing tests
* Mapper 004
    * Metalstorm no background when playing
    * Star tropics sort of freezes when starting
    * Not passing tests
* Mapper 002
    * Guardian Legend - GUI item on left side of screen is wonky
* Need to look at speed, but speed is really good when running in release mode, totally playable at 60FPS.


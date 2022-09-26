# SteveNES
Steve's Nintendo Emulator

I've always wanted to make an emulator. It hits so many of my interests: music, programming, video games, following rules and solving puzzles. I didn't feel programming something like that was quite in reach until I saw javidx's YouTube video series on making a NES emulator. He did great at breaking it down enough to feel like it was something I could tackle.

My goal here is to niavely follow javidx's videos and make what he made. Then I'd like to go back and make it my own in some way - even if it's just cleaning up the code and writing it how I would like to see it along with comments in my own words to seal in the learning.

What I'd like to implement
* Sound
* USB Controller
* Illegal Op Codes
* Reasonable mappers, I want to emulate my favorite games
  * Mario 2 & 3
  * Castlevania 3
  * Mega Man 2 & 3

I'm on a Mac using Visual Studio 2022 - I'm using **C# dotnet 6**. For graphics, I'm using the SDL library with the C# bindings provided.

I have a "Display Engine" project that is sort of javidx's Pixel Game Engine, or it does the task. It will handle game engine tasks, graphics and sound, and provide a way for the NES to do those things.

The NES is comprised of
* CPU
* PPU
* APU
* Main Bus (this kinda becomes the NES)
* Mappers
* Cart


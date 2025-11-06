Vi, or Voxel Island, is a game about digging downwards. It uses C# and monogame (specifically the compute branch by cpt-max). It is fully functional and has basic progression implemented.

I decided to open source this project as I don't believe I'm interested in continuing to develop this project. There's a couple of reasons why, and none of them are because I'm not interested in the idea; there's simply some things that would take essentially a complete rewrite, and I'm not willing to put in the time to do that.

In particular, I believe a game like this would be best suited as a multiplayer game. However, the game was built as a singleplayer game, and converting a singleplayer game into a multiplayer game is a huge effort. It's usually easier to write the game with multiplayer in mind from the very beginning. Thus, the rewrite.

Wishlist/TODO/Goals:
- multiplayer
- fix CSM (it's very shimmery)
- fix multithreaded chunk meshing (unoptimized)
- rework "layer" loading

Building
========

Install Visual Studio 2022 (later/earlier versions not tested)

Run the following commands:
```
git clone git@github.com:BlueTRaven/ViMG.git
cd ViMG
git submodule update --init --recursive
```

Open the solution in Visual Studio 2022

Build (Ctrl+Shift+B)
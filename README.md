# Voxelmetric

Voxelmetric an open source voxel framework for unity3d. It's meant to be an easy to use, easy to extend solution for voxel games. Voxelmetric is currently in alpha so expect breaking changes and incomplete documentation. I am currently mainating it alone and making changes as I learn but if anyone else wants to help they should feel free to make a pull request. Also use the issues to highlight bugs and things so that need to be adressed.

For implementation and usage view the wiki.

## Features

##### Terrain Generation
Generate realistic looking terrain with caves and landmarks like trees.

##### Ambient Occlusion
Darkening in the corners between blocks makes the terrain look more realistically lit.

##### Saving and Loading
Save and load your changes to the world at will.

##### Infinite Terrain
Terrain generates around a given object and is removed when you move too far away, there are no borders or limits.

##### Threading
Threading chunk updates and terrain loading means the voxels take full advantage of your hardware to generate fast without hurting framerate.

##### Pathfinding
3d voxel aligned pathfinding for units makes it possible for AI to move around the terrain.

## Plans
Lighting is currently in development but is too slow to enable by default just yet. There should also be some documentation implementing the framework and extending common things like block types.

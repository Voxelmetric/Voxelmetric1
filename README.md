# Voxelmetric/Voxe

[![Join the chat at https://gitter.im/richardbiely/Voxelmetric](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/richardbiely/Voxelmetric?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is a VoxelMetrics's fork that is slowly being merged with https://github.com/richardbiely/Voxe in order to make it more powerful. Once merging of all features is completed, Voxe will be discontinued and all further work will be put into this project. Even though it is a fork, it is almost a complete rewrite of the original Voxelmetric.

I liked how Voxelmetric was not just another voxel generator. It had some very neat features like support for customizable block types, simple client-server networking (very rather basic, though) and pathfinding. On the other hand, it was weak in terms of performance, code design and architecture. This came handy, however, because I had some fresh new ideas I wanted to try. So I tried (and still keep trying) and this is the result :)

Voxelmetric/Voxe is an open source voxel framework for Unity3d. It's meant to be an easy to use, easy to extend solution for voxel games. Voxelmetric/Voxe is currently in alpha so expect breaking changes and incomplete documentation. If anyone wants to help they are more then welcome and should feel free to make a pull request. Also use the issues to highlight bugs and suggest features.

## Features

### World management

##### Terrain Generation
Generate realistic looking terrain with caves and landmarks like trees.

##### Saving and Loading
Save and load your changes to the world at will.

##### Infinite Terrain
Terrain generates around a given object and is removed when you move too far away, there are no borders or limits.

### Special features

##### Threading
Using a custom threadpool, chunks are generated on multiple threads taking full advantage of you hardware. Voxe uses an event-driven model for chunk generation. Upon creation, each chunk registers to its neighbors and from this moment on everything is automatic. The system is build in a way that no synchronization is necessary.

##### Memory pooling
Voxe tries to waste as little memory as possible. It sports a memory pool manager that stores and reuses objects as necessary to improve performance.

##### Ambient Occlusion
Darkening in the corners between blocks makes the terrain look more realistically lit.

##### Define New Block Types
Define new blocks in the scene with your own textures and even your own 3d mesh for the block's geometry.

##### Pathfinding
3d voxel aligned pathfinding for units makes it possible for AI to move around the terrain.

## Development
Voxelmetric is in development and is not yet at a 1.0 release, this means things are liable to change fast without notice or be broken and documentation will struggle to keep up.

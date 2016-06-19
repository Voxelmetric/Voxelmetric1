# Voxe(lmetric)

[![Join the chat at https://gitter.im/richardbiely/Voxelmetric](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/richardbiely/Voxelmetric?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Repo Size](https://reposs.herokuapp.com/?path=richardbiely/Voxelmetric)](https://github.com/richardbiely/Voxelmetric)
[![License](https://img.shields.io/badge/Licence-GNU-blue.svg)](https://github.com/richardbiely/Voxelmetric/blob/alpha_3/licence)
[![Stories in progress](https://badge.waffle.io/richardbiely/Voxelmetric.png?label=In Progress&title=In Progress)](https://waffle.io/richardbiely/Voxelmetric)

This is a VoxelMetrics's fork that is slowly being merged with Voxe (https://github.com/richardbiely/Voxe) in order to make it more powerful. Once merging of all features is completed, Voxe will be discontinued and all further work will be put into this project. Even though it is a fork, it is an almost complete rewrite of the original Voxelmetric.

I liked how Voxelmetric was not just another voxel generator. It had some very neat features like support for customizable block types, simple client-server networking (very rather basic, though) and pathfinding. On the other hand, it was weak in terms of performance, code design and architecture. This came handy, however, because I had some fresh new ideas I wanted to try. So I tried (and still keep trying) and this is the result :)

Voxe(lmetric) is an open source voxel framework for Unity3d. It is meant to be an easy to use, easy to extend solution for voxel games. It is currently in alpha so expect breaking changes and incomplete documentation. If anyone wants to help they are more then welcome and should feel free to make a pull request. Also use the issues to highlight bugs and suggest features.

![alt tag](https://i.imgsafe.org/6e7f59856b.jpg)

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
Using a custom threadpool, chunks are generated on multiple threads taking full advantage of you hardware. Voxe(lmetric) uses an event-driven model for chunk generation. Upon creation, each chunk registers to its neighbors and from this moment on everything is automatic. The system is build in a way that no synchronization is necessary and can utilize all available CPU cores.

##### Memory pooling
Voxe(lmetric) tries to waste as little memory as possible. It sports a custom memory pool manager that stores and reuses objects as necessary to improve performance.

##### Ambient Occlusion
Darkening in the corners between blocks makes the terrain look more realistically lit.

##### Define New Block Types
Define new blocks in the scene with your own textures and even your own 3d mesh for the block's geometry.

##### Pathfinding
3d voxel aligned pathfinding for units makes it possible for AI to move around the terrain.

## Development
Voxel(metric) is in development and still has a log way to go. This means things are liable to change fast without notice or be broken and documentation will struggle to keep up.

# Voxe(lmetric)

[![Join the chat at https://gitter.im/richardbiely/Voxelmetric](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/richardbiely/Voxelmetric?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Repo Size](https://reposs.herokuapp.com/?path=richardbiely/Voxelmetric)](https://github.com/richardbiely/Voxelmetric)
[![License](https://img.shields.io/badge/Licence-GNU-blue.svg)](https://github.com/richardbiely/Voxelmetric/blob/alpha_3/licence)

Voxe(lmetric) is an open source voxel framework for Unity3d. It is meant to be an easy to use, easy to extend solution for voxel games. It is currently in alpha so expect breaking changes and incomplete documentation. Any help with the project is more then welcome. Feel free to create a pull request, ask questions or suggest new features.

This project is a result of mergin my original project Voxe (https://github.com/richardbiely/Voxe) with Voxelmetric (https://github.com/Voxelmetric/Voxelmetric1). It takes the best from both worlds - Voxe's performance and Voxelmetric's features. A lot of work has been put into this project and it barely resembles its predecessors now.

![alt tag](https://github.com/richardbiely/Voxelmetric/blob/alpha_3/voxelmetric.jpg)

## Features

### World management

##### Terrain Generation
Generate realistic looking terrain with caves and landmarks like trees.

##### Saving and Loading
Save and load your changes to the world at will.

##### Infinite Terrain
Terrain generates around a given object and is removed when you move too far away, there are no borders or limits neither in horizontal nor vertical direction unless you deliberately configure them.

#### Structures
Not only terrain but user defined structures are possible with Voxel(metric) as well. There is no limit to their size (although it's recommended to keep them spreading over just a few chunks at most). Be it buildings or clutter only your imagination is the limit.

### Special features

##### Threading
Using a custom threadpool chunks are generated on multiple threads taking full advantage of you hardware. Voxe(lmetric) uses an event-driven model for chunk generation. Upon creation, each chunk registers to its neighbors and from this moment on everything is automatic. The system is build in a way that no synchronization is necessary and can utilize all available CPU cores.

##### Memory pooling
Voxe(lmetric) tries to waste as little memory as possible. It sports a custom memory pool manager that stores and reuses objects as necessary to improve performance.

##### Ambient Occlusion
Darkening in the corners between blocks makes the terrain look more realistically lit.

##### Define New Block Types
Define new blocks in the scene with your own textures and even your own 3d mesh for the block's geometry.

##### Pathfinding
3d voxel aligned pathfinding for units makes it possible for AI to move around the terrain.

## Development note
Voxel(metric) is in development and still has a log way to go. This means things are liable to change fast without notice or be broken and documentation will struggle to keep up.

## How to run the project
There are several ways to open the project.

##### Open project
The easiest one is to select "Open" in Unity3D dashboard or "Open Project..." from the editor and selecting "VoxeUnity". This will work right of the box with no further steps required on your side.

##### Import to an existing project
If you already have an existing project running and want to import Voxe(lmetric) into it you can simply drag VoxeUnity folder and drop it to your project's folder. However, two more steps are necessary in this case. You need to select .NET 4.6 as your scripting runtime (Edit->Project Settings->Player) and you need to update the ResourcesFolder variable in Config.cs. By default it is set to "Assets/Voxelmetric/Resources" but depending on where you place you project you might need to change it to, e.g. "Assets/VoxeUnity/Assets/Voxelmetric/Resources". Without pointing Voxe(lmetric) into a proper folder it would not be able to find its resource files which would result in all kinds of errors on project's startup.

##### Opening example scenes
Once you imported your project successfuly there is nothing more holding you back from trying one of the example scenes bundled with the project. You can find them in VoxeUnity/Assest/Voxelmetric/Examples. You can use them as a template to build your own scenes.
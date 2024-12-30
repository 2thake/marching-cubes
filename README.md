# marching-cubes
Marching cubes algorithm implemented in C# and Unity.

<img width="639" alt="image" src="https://github.com/user-attachments/assets/ccc3aa62-f25f-4349-bcdb-13e9d47e9579" />

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Usage](#usage)
- [How It Works](#how-it-works)
- [Acknowledgements](#acknowledgements)

## Features

- **Marching Cubes Algorithm**: Efficiently generates meshes from scalar fields using the Marching Cubes technique.
- **3D Perlin Noise**: Incorporates 3D Perlin noise for natural and varied terrain generation.
- **Dynamic Mesh Generation**: Generates and updates meshes in real-time based on configurable parameters.
- **Multithreaded Processing**: Utilizes parallel processing to accelerate grid generation and mesh creation.
- **Customizable Parameters**:
  - Grid Size
  - Noise Scale
  - Surface Level Threshold
- **Inspector Integration**: Easy-to-use interface within the Unity Inspector for tweaking settings on the fly.

## Getting Started

### Prerequisites

- **Unity**: 6000.0.29f1 or later recommended.

### Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/2thake/marching-cubes.git

2.	**Open in Unity:**
	- Launch Unity Hub.
	- Click on “Add” and navigate to the cloned repository folder.
	- Open the project in Unity.

### Usage
1. Open the "Marching Cubes Demo" scene. This contains a basic demonstration of the algorithm in action.
2.	Configure Parameters:
  - Select the MarchingCubes GameObject in the Hierarchy.
	-	In the Inspector, adjust the following parameters:
	-	Grid Size: Determines the resolution of the voxel grid.
	-	Noise Scale: Adjusts the frequency of the Perlin noise.
	-	Surface Level: Surface threshold for the Marching Cubes algorithm.
	-	Enable Scroll: Turns mesh scrolling on or off.
	-	Scroll Speed: Controls the speed of the scrolling effect.
3. Play the Scene:
	-	Enter Play mode to visualize the generated mesh.
	-	Observe the dynamic scrolling effect if enabled

 ## How It Works

The **Marching Cubes** algorithm is a widely used technique for extracting a polygonal mesh of an isosurface from a three-dimensional scalar field (voxels). Here's a detailed overview of how it's implemented in this project:

1. **Scalar Field Generation**:
    - A **1D float array** (`grid`) represents the 3D scalar field, where each element corresponds to a voxel's density or occupancy.
    - The grid is generated using **3D Perlin noise**, which provides smooth and natural variations. This noise is scaled and modified with a height penalty to create intricate and interesting shapes.

2. **Cube Index Calculation**:
    - The 3D grid is iterated through, and for each cube (composed of 8 neighboring voxels), the algorithm determines which corners of the cube are inside the isosurface based on the `surfaceLevel` threshold.
    - Each corner is assigned a binary value (1 if the scalar value is above the `surfaceLevel`, otherwise 0). These values are combined to form a cube index that uniquely identifies the configuration of the isosurface within the cube.

3. **Vertex Interpolation**:
    - For each edge of the cube that intersects the isosurface (determined by the cube index), the exact position of the vertex on the edge is calculated using **linear interpolation** between the two corner points.

4. **Triangle Generation**:
    - Using a **predefined triangle lookup table** (`TriangleConnectionTable`), the algorithm maps each cube index to a set of triangles that form the mesh for that particular cube configuration.
    - These triangles are defined by the interpolated vertices, effectively constructing the mesh geometry that represents the isosurface.

5. **Mesh Assembly**:
    - All generated vertices and triangles are compiled into a Unity `Mesh` object.
    - The mesh is then optimized and assigned to the `MeshFilter` and `MeshCollider` components, enabling both rendering and collision detection within the Unity environment.

6. **Parallel Processing**:
    - To optimize performance, especially for larger grid sizes, the grid generation process uses **parallel processing** using `Parallel.For`.
    - Triangles are added to a thread-safe concurrect bag which is then turned into an array when all parallel operations have been completed.
    - The 1-dimensional arrays were used with future compute shader integration in mind which would allow larger scale real-time generation.

  ## Acknowledgements

- **[dwilliamson](https://gist.github.com/dwilliamson/c041e3454a713e58baf6e4f8e5fffecd)**: I used the triangulation tables from dwilliamson's JavaScript marching cubes implementation.

# Cloth Physics

![Simulation Demo][gif]

This monogame demo shows off a simple simulation of rope and cloth physics.
The simulation is performed by applying downward acceleration to each of the points over time and solving their positions for the constrained length of the lines between them.

## Building
I highly recommend using the dotnet CLI to build this monogame project. You can clone this repo and follow the steps in the [monogame docs][docs]. You can also open the folder in Visual Studio Code and run it from there.

## Usage
Left click to create points.

Right click to lock points in place.

Hold control and left click to delete points.

Left click on a point and drag to another point and release to create a line constraint between them.

Press the space bar to start or pause the simulation.

Left click and drag during the simulation to delete lines.

Press F to toggle between full screen and windowed mode.

## Things to try
* Change the value of gravity and see how the simulation behaves.
* Try turning on wind.
* Change the number of iterations used to solve the simulation and see how it changes.


The code in this repository is CC0, public domain.

[gif]: cloth.gif "Simulation Demo"
[docs]: https://docs.monogame.net/articles/packaging_games.html
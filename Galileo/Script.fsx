#I @"../build/"

#r @"Galileo.dll"

open System
open Galileo

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
Galileo.init ()

// ------------------------------------------------------------------------- //

Galileo.spawnRedTriangle ()
Galileo.spawnBlueTriangle ()
Galileo.spawnOctahedron ()
//Galileo.spawnDefaultRedTriangle () |> Async.RunSynchronously
//Galileo.spawnDefaultBlueTriangle () |> Async.RunSynchronously

// ------------------------------------------------------------------------- //

//Galileo.spawnDefaultOctahedron () |> Async.RunSynchronously
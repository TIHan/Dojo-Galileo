﻿#I @"../build/"

#r @"System.Numerics.Vectors.dll"
#r @"Galileo.dll"

open System
open System.Numerics
open Galileo
open Game

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
Runtime.GCSettings.LatencyMode <- Runtime.GCLatencyMode.Batch
Galileo.init ()

// ------------------------------------------------------------------------- //

let node = Galileo.spawnSphere ()
let nodes = Galileo.spawnMultipleSpheres ()

// ------------------------------------------------------------------------- //

let gameUnit = game { () }
nodes
|> Array.iter (fun node ->
    node
    |> Node.setUpdate (fun time state -> gameUnit
    ))

printfn "%A" nodes.Length

let node2 = Galileo.spawnSphere ()
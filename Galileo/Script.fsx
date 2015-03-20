#I @"../build/"

#r @"System.Numerics.Vectors.dll"
#r @"Galileo.dll"

open System
open System.Numerics
open Galileo

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
Galileo.init ()

// ------------------------------------------------------------------------- //

let node = Galileo.spawnSphere ()
let nodes = Galileo.spawnMultipleSpheres ()

// ------------------------------------------------------------------------- //

nodes
|> Array.iter (fun node ->
    node
    |> Node.setUpdate (fun time state -> 
        { state with color = (0.f, 0.f, 1.f) }
    ))

printfn "%A" nodes.Length

let node2 = Galileo.spawnSphere ()
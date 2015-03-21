open System
open System.Numerics
open Galileo
open Game

Runtime.GCSettings.LatencyMode <- Runtime.GCLatencyMode.Batch
Galileo.init ()

// ------------------------------------------------------------------------- //

let node = Galileo.spawnSphere ()
let nodes = Galileo.spawnMultipleSpheres ()

// ------------------------------------------------------------------------- //

nodes
|> Array.iter (fun node ->
    node
    |> Node.setUpdate (fun time state -> 
        state.r <~ 0.f
    ))

[<EntryPoint>]
let main argv = 
    Console.ReadLine() |> ignore
    0

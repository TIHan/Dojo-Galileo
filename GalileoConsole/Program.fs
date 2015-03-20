open System
open System.Numerics
open Galileo

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
        { state with color = (0.f, 0.f, 1.f) }
    ))

[<EntryPoint>]
let main argv = 
    Console.ReadLine() |> ignore
    0

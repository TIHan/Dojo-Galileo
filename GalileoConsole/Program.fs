open System
open System.Numerics
open Galileo
open Game

Runtime.GCSettings.LatencyMode <- Runtime.GCLatencyMode.Batch
Galileo.init ()

// ------------------------------------------------------------------------- //

let node = Galileo.spawnSphere ()

// ------------------------------------------------------------------------- //

let node2 = Galileo.spawnSphere ()

node
|> Node.setUpdate (fun time sphere ->
    sphere.scale <~ Matrix4x4.CreateScale(3.f)

)

node2
|> Node.setUpdate (fun time sphere ->
    sphere.scale <~ Matrix4x4.CreateScale(1.f)
    sphere.rotation <~ Matrix4x4.CreateRotationZ(cos(single time.TotalSeconds))
)

[<EntryPoint>]
let main argv = 
    Console.ReadLine() |> ignore
    0

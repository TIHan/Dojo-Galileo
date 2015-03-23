#I @"../build/"

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

// ------------------------------------------------------------------------- //

let node2 = Galileo.spawnSphere ()

node
|> Node.setUpdate (fun time sphere ->
    sphere.scale <~ Matrix4x4.CreateScale(3.f)

)

node2
|> Node.setUpdate (fun time sphere ->
    sphere.scale <~ Matrix4x4.CreateScale(0.f)

    sphere.rotation <~ Matrix4x4.CreateRotationZ(cos(single time.TotalSeconds))
)
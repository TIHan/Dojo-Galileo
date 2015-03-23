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
    sphere.r <~ 0.f
    sphere.g <~ 0.f
    sphere.b <~ 1.f
)

node2
|> Node.setUpdate (fun time sphere ->
    sphere.scale <~ Matrix4x4.CreateScale(0.5f)
    sphere.translation <~ Matrix4x4.CreateTranslation(Vector3(5.f, 5.f, 0.f))
    sphere.rotation <~ Matrix4x4.CreateRotationY(cos(single time.TotalSeconds))
)
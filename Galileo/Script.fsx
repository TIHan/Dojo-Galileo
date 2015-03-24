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
    let rotationAmount = sphere.rotationAmount.Value + 0.1f
    sphere.scale <~ Matrix4x4.CreateScale(0.5f)
    sphere.translation <~ Matrix4x4.CreateTranslation(Vector3(10.f, 0.f, 0.f))
    sphere.rotationAmount <~ rotationAmount
    sphere.rotation <~ Matrix4x4.CreateRotationY(rotationAmount)
)
#I @"../build/"

#r @"System.Runtime.dll"
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
let node3 = Galileo.spawnSphere ()
let node4 = Galileo.spawnSphere ()

node
|> Node.setUpdate (fun time sphere ->
    sphere.scale <~ Matrix4x4.CreateScale(3.f)
    sphere.r <~ 1.f
    sphere.g <~ 0.f
    sphere.b <~ 0.f
)

node2
|> Node.setUpdate (fun time sphere ->

    let rotationAmount = sphere.rotationAmount.Value + 0.1f
    sphere.scale <~ Matrix4x4.CreateScale(0.5f,Vector3(7.f,0.f,-5.f))
    sphere.translation <~ Matrix4x4.CreateTranslation(Vector3(10.f, 10.f, 0.f))
    sphere.rotationAmount <~ rotationAmount
    sphere.rotation <~ Matrix4x4.CreateRotationZ(rotationAmount)
)

node3
|> Node.setUpdate (fun time sphere ->

    let rotationAmount = sphere.rotationAmount.Value + 0.1f
    sphere.scale <~ Matrix4x4.CreateScale(0.5f,Vector3(1.f,2.f,-5.f))
    sphere.translation <~ Matrix4x4.CreateTranslation(Vector3(10.f, 10.f, 0.f))
    sphere.rotationAmount <~ rotationAmount
    sphere.rotation <~ Matrix4x4.CreateRotationZ(rotationAmount)
)

node4
|> Node.setUpdate (fun time sphere ->

    let rotationAmount = sphere.rotationAmount.Value + 0.2f
    sphere.scale <~ Matrix4x4.CreateScale(0.5f,Vector3(1.f,2.f,2.f))
    sphere.translation <~ Matrix4x4.CreateTranslation(Vector3(0.f, 10.f, 0.f))
    sphere.rotationAmount <~ rotationAmount
    sphere.rotation <~ Matrix4x4.CreateRotationX(rotationAmount)
)

let rng = System.Random ()
let number () = float32 (10. * rng.NextDouble () - 5.)
let u () = rng.NextDouble () |> float32

for i in 0 .. 100 do 
    let n = Galileo.spawnSphere ()
    n
    |> Node.setUpdate (fun time sphere ->
        sphere.r <~ u ()
        sphere.g <~ u ()
        sphere.b <~ u ()
        let rotationAmount = sphere.rotationAmount.Value + number ()
        sphere.scale <~ Matrix4x4.CreateScale(number (), Vector3(number (),number (),number ()))
        sphere.translation <~ Matrix4x4.CreateTranslation(Vector3(number (), number (), number ()))
        sphere.rotationAmount <~ rotationAmount
        sphere.rotation <~ Matrix4x4.CreateRotationZ(rotationAmount))


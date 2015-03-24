#I @"../build/"

#r @"System.Numerics.Vectors.dll"
#r @"Galileo.dll"

open System
open System.Numerics
open Galileo
open Game

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
Galileo.init ()

// ------------------------------------------------------------------------- //

let entity = Galileo.spawnSphere ()

entity
|> GameEntity.setUpdate (fun time sphere ->
    { sphere with
        scale = Matrix4x4.CreateScale(3.f)
        b = 1.f
    }
)

// ------------------------------------------------------------------------- //

let entity2 = Galileo.spawnSphere ()

entity2
|> GameEntity.setUpdate (fun time sphere ->
    let rotationAmount = sphere.rotationAmount + 0.1f
    { sphere with
        rotationAmount = rotationAmount
        scale = Matrix4x4.CreateScale(0.5f)
        translation = Matrix4x4.CreateTranslation(Vector3(10.f, 0.f, 0.f))
        rotation = Matrix4x4.CreateRotationY(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

let entities = Galileo.spawnSpheres 100
entities
|> Array.iter (fun entity ->
    entity
    |> GameEntity.setUpdate (fun time sphere -> { sphere with translation = Matrix4x4.Identity }
    )
)
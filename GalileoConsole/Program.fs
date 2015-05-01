open System
open System.Numerics
open Galileo
open Game

Galileo.init ()

let earthSize = 6371.0f
let moonSize = 1737.10f

// ------------------------------------------------------------------------- //

let entity = Galileo.spawnSphere "earth.jpg"

entity
|> GameEntity.setUpdate (fun time sphere ->
    let sphere =
        if Galileo.isMouseButtonPressed MouseButtonType.Right
        then { sphere with rotationAmount = sphere.rotationAmount + 0.5f }
        else sphere

    let rotationAmount = sphere.rotationAmount
    { sphere with
        scale = Matrix4x4.CreateScale(earthSize)
        rotation = Matrix4x4.CreateRotationZ(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

let entity2 = Galileo.spawnSphere "moon.jpg"

entity2
|> GameEntity.setUpdate (fun time sphere ->

    let sphere =
        if Galileo.isMouseButtonPressed MouseButtonType.Left
        then { sphere with rotationAmount = sphere.rotationAmount + 0.5f }
        else sphere

    let rotationAmount = sphere.rotationAmount
    { sphere with
        scale = Matrix4x4.CreateScale(moonSize)
        translation = Matrix4x4.CreateTranslation(Vector3(Galileo.LunarDistance, 0.f, 0.f)) * Matrix4x4.CreateRotationZ(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

[<EntryPoint>]
let main argv = 
    Console.ReadLine() |> ignore
    0

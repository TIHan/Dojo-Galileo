open System
open System.Numerics
open Galileo
open Input
open Game

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

    let sphere =
        if Input.isMouseButtonPressed MouseButtonType.Left
        then { sphere with rotationAmount = sphere.rotationAmount + 0.5f }
        else sphere

    let rotationAmount = sphere.rotationAmount
    { sphere with
        scale = Matrix4x4.CreateScale(0.5f)
        translation = Matrix4x4.CreateTranslation(Vector3(10.f, 0.f, 0.f))
        rotation = Matrix4x4.CreateRotationY(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

[<EntryPoint>]
let main argv = 
    Console.ReadLine() |> ignore
    0

open System
open System.Numerics
open Galileo
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

    let input = Galileo.getInputState ()

    let sphere =
        input.Events
        |> List.fold (fun sphere evt ->
            
            match evt with
            | Input.InputEvent.MouseButtonPressed t ->               
                { sphere with rotationAmount = sphere.rotationAmount + 0.1f }

            | _ -> sphere
        ) sphere

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

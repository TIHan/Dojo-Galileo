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

let entity = Galileo.spawnSphere "earth.jpg"

entity
|> GameEntity.setUpdate (fun time sphere ->
    { sphere with
        scale = Matrix4x4.CreateScale(3.f)
        b = 1.f
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
        scale = Matrix4x4.CreateScale(0.5f)
        translation = Matrix4x4.CreateTranslation(Vector3(10.f, 0.f, 0.f))
        rotation = Matrix4x4.CreateRotationZ(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //
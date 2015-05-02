#I @"../build/"

#r @"System.Runtime.dll"
#r @"System.Numerics.Vectors.dll"
#r @"Galileo.dll"

open System
open System.Numerics
open Galileo
open Game

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

Galileo.init ()

let earthSize = 6371.0f
let moonSize = 1737.10f

// ------------------------------------------------------------------------- //

let earth = Galileo.spawnPlanet "earth.jpg"

earth.SetUpdate (fun time interval planet ->
    let sphere =
        if Galileo.isMouseButtonPressed MouseButtonType.Right
        then { planet with rotationAmount = planet.rotationAmount + 0.5f }
        else planet

    let rotationAmount = sphere.rotationAmount
    { sphere with
        scale = Matrix4x4.CreateScale(earthSize)
        rotation = Matrix4x4.CreateRotationY(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

let moon = Galileo.spawnPlanet "moon.jpg"

moon.SetUpdate (fun time interval planet ->
    let planet =
        if Galileo.isMouseButtonPressed MouseButtonType.Left
        then { planet with rotationAmount = planet.rotationAmount + 0.05f }
        else planet

    let rotationAmount = planet.rotationAmount
    { planet with
        scale = Matrix4x4.CreateScale(moonSize)
        translation = Matrix4x4.CreateTranslation(Vector3(0.f, 0.f, -Galileo.LunarDistance)) * Matrix4x4.CreateRotationY(rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

Galileo.setUpdateCameraPosition (fun () -> 
    Vector3 (0.f, 80000.f, 7000.f * 100.f)
)

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
    { planet with
        Scale = Matrix4x4.CreateScale(earthSize)
    }
)

// ------------------------------------------------------------------------- //

let moon = Galileo.spawnPlanet "moon.jpg"

let rotationAmount = ref 0.f
moon.SetUpdate (fun time interval planet ->
    rotationAmount := !rotationAmount + 0.05f
    { planet with
        Scale = Matrix4x4.CreateScale(moonSize)
        Translation = Matrix4x4.CreateTranslation(Vector3(0.f, 0.f, -Galileo.LunarDistance)) * Matrix4x4.CreateRotationY(!rotationAmount)
    }
)

// ------------------------------------------------------------------------- //

Galileo.setUpdateCameraPosition (fun () -> 
    Vector3 (0.f, 80000.f, 7000.f * 100.f)
)

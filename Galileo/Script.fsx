#I @"../build/"

#r @"System.Numerics.Vectors.dll"
#r @"Galileo.dll"

open System
open System.Numerics
open Galileo

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
Galileo.init ()

// ------------------------------------------------------------------------- //

let node = Galileo.spawnSphere ()
let nodes = Galileo.spawnMultipleSpheres ()

// ------------------------------------------------------------------------- //

node
|> Node.setUpdate (fun time state ->
    { state with 
        translation = Matrix4x4.Identity * Matrix4x4.CreateTranslation (Vector3.One + (Vector3.UnitZ * cos(single time.TotalSeconds)))
        //position = Vector3.One * -1.5f
    }
)

let node2 = Galileo.spawnSphere ()
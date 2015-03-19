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


// ------------------------------------------------------------------------- //

node
|> Node.setUpdate (fun time state ->
    { state with 
        translation = Matrix4x4.CreateTranslation (Vector3.One)
        //position = Vector3.One * -1.5f
    }
)

let node2 = Galileo.spawnSphere ()
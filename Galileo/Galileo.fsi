namespace Galileo

open System
open System.Numerics

open Game

[<NoComparison; ReferenceEquality>]
type Planet =
    {
        Translation: Matrix4x4
        Rotation: Matrix4x4
        Scale: Matrix4x4
    }


[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnPlanet : string -> Entity<Planet>

    val setUpdateCameraPosition : (unit -> Vector3) -> unit

    val setUpdateLookAtPosition : (unit -> Vector3) -> unit

    val entitiesIter : (IEntity -> unit) -> unit

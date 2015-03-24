namespace Galileo

open System
open System.Numerics

open Game

[<NoComparison; ReferenceEquality>]
type Sphere =
    {
        translation: Matrix4x4
        rotation: Matrix4x4
        scale: Matrix4x4
        rotationAmount: single
        r: float32
        g: float32
        b: float32
    }


[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnSphere : unit -> GameEntity<Sphere>

    val spawnSpheres : int -> GameEntity<Sphere> []

module GameEntity =

    val setUpdate : (TimeSpan -> 'T -> 'T) -> GameEntity<'T> -> unit
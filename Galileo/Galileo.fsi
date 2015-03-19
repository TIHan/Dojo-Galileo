namespace Galileo

open System
open System.Numerics

[<Sealed>]
type Node<'T>

type Sphere =
    {
        translation: Matrix4x4
        rotation: Matrix4x4
        color: float32 * float32 * float32
    }

[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnSphere : unit -> Node<Sphere>

    val spawnMultipleSpheres : unit -> Node<Sphere> []

module Node =

    val setUpdate : (TimeSpan -> 'T -> 'T) -> Node<'T> -> unit
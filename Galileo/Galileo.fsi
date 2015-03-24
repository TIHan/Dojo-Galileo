﻿namespace Galileo

open System
open System.Numerics

open Game

[<NoComparison; ReferenceEquality>]
type Sphere =
    {
        translation: GameField<Matrix4x4>
        rotation: GameField<Matrix4x4>
        scale: GameField<Matrix4x4>
        rotationAmount: GameField<single>
        r: GameField<float32>
        g: GameField<float32>
        b: GameField<float32>
    }

[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnSphere : unit -> Node<Sphere>

    val spawnMultipleSpheres : unit -> Node<Sphere> []

module Node =

    val setUpdate : (TimeSpan -> 'T -> unit) -> Node<'T> -> unit
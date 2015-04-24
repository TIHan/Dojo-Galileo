namespace Galileo

open System
open System.Numerics

open Game
open Input

type MouseState = Input.MouseState
type MouseButtonType = Input.MouseButtonType

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

    val getInputEvents : unit -> InputEvent list

    val getMouse : unit -> MouseState

    val isKeyPressed : char -> bool

    val isMouseButtonPressed : MouseButtonType -> bool

module GameEntity =

    val setUpdate : (TimeSpan -> 'T -> 'T) -> GameEntity<'T> -> unit
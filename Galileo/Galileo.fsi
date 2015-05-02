namespace Galileo

open System
open System.Numerics

open Game
open Input

type MouseState = Input.MouseState
type MouseButtonType = Input.MouseButtonType

[<NoComparison; ReferenceEquality>]
type Planet =
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

    val LunarDistance : single

    val init : unit -> unit

    val spawnPlanet : string -> Entity<Planet>

    val getInputEvents : unit -> InputEvent list

    val getMouse : unit -> MouseState

    val isKeyPressed : char -> bool

    val isMouseButtonPressed : MouseButtonType -> bool

    val setUpdateCameraPosition : (unit -> Vector3) -> unit

    val setUpdateLookAtPosition : (unit -> Vector3) -> unit

    val entitiesIter : (IEntity -> unit) -> unit

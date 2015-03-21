namespace Game

open System
open System.Collections.Generic

type Node = interface end

[<Sealed>]
type Node<'T> =
    member SetUpdate : (TimeSpan -> 'T -> unit) -> unit

    interface Node

type GameEnvironment =
    {
        nodes: (Node option) []
        updates: ((unit -> unit) option) []
        renders: ((float32 -> unit) option) []
        mutable length: int
        mutable time: TimeSpan
        mutable defaultShaderProgram: int
    }

    static member Create : unit -> GameEnvironment

    member CreateNode<'T> : 'T * (GameEnvironment -> 'T -> unit) * (Lazy<GameEnvironment -> float32 -> 'T -> 'T -> unit>) -> Node<'T>

    member UpdateNodes : unit -> unit

    member RenderNodes : float32 -> unit

[<Sealed>]
type GameField<'T when 'T : unmanaged> =
    new : 'T -> GameField<'T>

    member Value : 'T

    member History : 'T []

    static member (<~) : GameField<'T> * 'T -> unit

module GameLoop =

    val start : pre: (unit -> unit) -> update: (int64 -> int64 -> unit) -> render: (float32 -> unit) -> 'T
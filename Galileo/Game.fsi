namespace Game

open System
open System.Collections.Generic

[<Sealed>]
type NodeCollection =
    static member Create<'T> : unit -> NodeCollection

[<Sealed>]
type Node<'T> =
    member SetUpdate : (TimeSpan -> 'T -> 'T) -> unit

type GameEnvironment =
    {
        mutable time: TimeSpan
        mutable defaultShaderProgram: int
        mutable nodeDict: Dictionary<Type, NodeCollection>
    }

    static member Create : unit -> GameEnvironment

    member CreateNode<'T> : 'T * (GameEnvironment -> 'T -> 'T) * (Lazy<GameEnvironment -> float32 -> 'T -> 'T -> unit>) -> Node<'T>

    member UpdateNodes : unit -> unit

    member RenderNodes : float32 -> unit

module GameLoop =

    val start : pre: (unit -> unit) -> update: (int64 -> int64 -> unit) -> render: (float32 -> unit) -> 'T
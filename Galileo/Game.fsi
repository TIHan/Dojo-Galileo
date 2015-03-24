namespace Game

open System
open System.Collections.Generic

type GameEntity = interface end

[<Sealed>]
type GameEntity<'T> =
    member SetUpdate : (TimeSpan -> 'T -> 'T) -> unit

    interface GameEntity

[<NoComparison; ReferenceEquality>]
type GameEnvironment =
    {
        entities: (GameEntity option) []
        updates: ((unit -> unit) option) []
        renders: ((float32 -> unit) option) []
        mutable length: int
        mutable time: TimeSpan
        mutable defaultShaderProgram: int
    }

    static member Create : unit -> GameEnvironment

    member CreateEntity<'T> : 'T * (GameEnvironment -> 'T -> 'T) * (Lazy<GameEnvironment -> float32 -> 'T -> 'T -> unit>) -> GameEntity<'T>

    member UpdateEntities : unit -> unit

    member RenderEntities : float32 -> unit

module GameLoop =

    val start : pre: (unit -> unit) -> update: (int64 -> int64 -> unit) -> render: (float32 -> unit) -> 'T
namespace Game

open System
open System.Collections.Generic

type GameEntityUpdate<'T> = GameEnvironment -> 'T -> 'T

and GameEntityRender<'T> = GameEnvironment -> 'T -> 'T -> unit

and IGameEntity =
    abstract Id : int
    abstract Update : GameEnvironment -> unit
    abstract Render : GameEnvironment -> unit

and [<Sealed>]
    GameEntity<'T> =

    member SetUpdate : (TimeSpan -> 'T -> 'T) -> unit

    interface IGameEntity

and [<NoComparison; ReferenceEquality>]
    GameEnvironment =
    {
        entities: (IGameEntity option) []
        mutable length: int
        mutable time: TimeSpan
        mutable renderDelta: float32
        mutable planetShaderProgram: int
        mutable backgroundShaderProgram: int
    }

    static member Create : unit -> GameEnvironment

    member CreateEntity<'T> : 'T * GameEntityUpdate<'T> * GameEntityRender<'T> -> GameEntity<'T>

    member UpdateEntities : unit -> unit

    member RenderEntities : unit -> unit

module GameLoop =

    val start : pre: (unit -> unit) -> update: (int64 -> int64 -> unit) -> render: (float32 -> unit) -> 'T
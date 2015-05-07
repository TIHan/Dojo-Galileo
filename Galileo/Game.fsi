namespace Game

open System
open System.Collections.Generic

type GameEntityUpdate<'T> = GameEnvironment -> 'T -> 'T

and GameEntityRender<'T> = GameEnvironment -> 'T -> 'T -> unit

and IEntity =
    abstract Id : int
    abstract Update : GameEnvironment -> unit
    abstract CommitUpdate : unit -> unit
    abstract Render : GameEnvironment -> unit

and [<Sealed>]
    Entity<'T> =

    member SetUpdate : (TimeSpan -> TimeSpan -> 'T -> 'T) -> unit

    member LastKnownState : 'T

    interface IEntity

and [<NoComparison; ReferenceEquality>]
    GameEnvironment =
    {
        entities: (IEntity option) []
        mutable length: int
        mutable time: TimeSpan
        mutable interval: TimeSpan
        mutable renderDelta: float32
        mutable planetShaderProgram: int
        mutable backgroundShaderProgram: int
    }

    static member Create : unit -> GameEnvironment

    member CreateEntity<'T> : 'T * GameEntityUpdate<'T> * GameEntityRender<'T> -> Entity<'T>

    member CreateEntityWithoutAdding<'T> : 'T * GameEntityUpdate<'T> * GameEntityRender<'T> -> Entity<'T>

    member UpdateEntities : unit -> unit

    member CommitUpdateEntities : unit -> unit

    member RenderEntities : unit -> unit

module GameLoop =

    val start : pre: (unit -> unit) -> update: (int64 -> int64 -> unit) -> render: (float32 -> unit) -> 'T
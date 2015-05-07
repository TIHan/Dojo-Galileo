namespace Game

open System
open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent

type GameEntityUpdate<'T> = GameEnvironment -> 'T -> 'T

and GameEntityRender<'T> = GameEnvironment -> 'T -> 'T -> unit

and IEntity =
    abstract Id : int
    abstract Update : GameEnvironment -> unit
    abstract CommitUpdate : unit -> unit
    abstract Render : GameEnvironment -> unit

and [<NoComparison; ReferenceEquality>]
    Entity<'T> =
    {
        id: int
        env: GameEnvironment
        mutable prevModel: 'T
        mutable model: 'T
        mutable nextModel: 'T
        mutable update: GameEntityUpdate<'T>
        mutable render: GameEntityRender<'T>
    }

    member this.SetUpdate update =
        this.update <- fun env x -> update env.time env.interval x

    member this.LastKnownState = this.model

    interface IEntity with

        member this.Id = this.id

        member this.Update env =
            this.prevModel <- this.model
            this.nextModel <- this.update env this.model

        member this.CommitUpdate () =
            this.model <- this.nextModel

        member this.Render env =
            this.render env this.prevModel this.model
            

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

    static member Create () =
        {
            entities = Array.init (65536) (fun _ -> None)
            length = 0
            renderDelta = 0.f
            time = TimeSpan.Zero
            interval = TimeSpan.Zero
            planetShaderProgram = 0
            backgroundShaderProgram = 0
        }

    member this.CreateEntity<'T> (model: 'T, update, render) =
        let entity =
            {
                id = this.length
                env = this
                prevModel = model
                model = model
                nextModel = model
                update = update
                render = render
            }
        this.AddEntity entity
        entity

    member this.CreateEntityWithoutAdding<'T> (model: 'T, update, render) =
        let entity =
            {
                id = this.length
                env = this
                prevModel = model
                model = model
                nextModel = model
                update = update
                render = render
            }
        entity

    member this.AddEntity<'T> (entity: Entity<'T>) =
        this.entities.[this.length] <- Some (entity :> IEntity)
        this.length <- this.length + 1

    member this.UpdateEntities () =
        this.entities
        |> Array.Parallel.iter (fun ent ->
            match ent with
            | None -> ()
            | Some ent -> ent.Update this)

    member this.CommitUpdateEntities () =
        this.entities
        |> Array.iter (fun ent ->
            match ent with
            | None -> ()
            | Some ent -> ent.CommitUpdate ())

    member this.RenderEntities () =
        this.entities
        |> Array.iter (fun ent ->
            match ent with
            | None -> ()
            | Some ent -> ent.Render this)

// http://gafferongames.com/game-physics/fix-your-timestep/
module GameLoop =
    type private GameLoop<'T> = { 
        LastTime: int64
        UpdateTime: int64
        UpdateAccumulator: int64
        RenderAccumulator: int64
        RenderFrameCount: int
        RenderFrameCountTime: int64
        RenderFrameLastCount: int }

    let start (pre: unit -> unit) (update: int64 -> int64 -> unit) (render: float32 -> unit) =
        let targetUpdateInterval = (1000. / 30.) * 10000. |> int64
        let targetRenderInterval = (1000. / 12000.) * 10000. |> int64
        let skip = (1000. / 5.) * 10000. |> int64

        let stopwatch = Stopwatch.StartNew ()
        let inline time () = 
            stopwatch.Elapsed.Ticks

        let rec loop gl =
            let currentTime = time ()
            let deltaTime =
                match currentTime - gl.LastTime with
                | x when x > skip -> skip
                | x -> x

            let updateAcc = gl.UpdateAccumulator + deltaTime

            // We do not want our render accumulator going out of control,
            // so let's put a limit of its interval.
            let renderAcc = 
                match gl.RenderAccumulator with
                | x when x > targetRenderInterval -> targetRenderInterval
                | x -> x + deltaTime

            let rec processUpdate gl =
                if gl.UpdateAccumulator >= targetUpdateInterval
                then
                    update gl.UpdateTime targetUpdateInterval

                    processUpdate
                        { gl with 
                            UpdateTime = gl.UpdateTime + targetUpdateInterval
                            UpdateAccumulator = gl.UpdateAccumulator - targetUpdateInterval }
                else
                    gl

            let processRender gl =
                if gl.RenderAccumulator >= targetRenderInterval then
                    render (single gl.UpdateAccumulator / single targetUpdateInterval)

                    let renderCount, renderCountTime, renderLastCount =
                        if currentTime >= gl.RenderFrameCountTime + (10000L * 1000L) then
                            printfn "FPS: %A" gl.RenderFrameLastCount
                            1, gl.RenderFrameCountTime + (10000L * 1000L), gl.RenderFrameCount
                        else
                            gl.RenderFrameCount + 1, gl.RenderFrameCountTime, gl.RenderFrameLastCount

                    { gl with 
                        LastTime = currentTime
                        RenderAccumulator = gl.RenderAccumulator - targetRenderInterval
                        RenderFrameCount = renderCount
                        RenderFrameCountTime = renderCountTime
                        RenderFrameLastCount = renderLastCount }
                else
                    { gl with LastTime = currentTime }

            pre ()
       
            { gl with UpdateAccumulator = updateAcc; RenderAccumulator = renderAcc }
            |> processUpdate
            |> processRender
            |> loop

        loop
            {
              LastTime = 0L
              UpdateTime = 0L
              UpdateAccumulator = targetUpdateInterval
              RenderAccumulator = 0L
              RenderFrameCount = 0
              RenderFrameCountTime = 0L
              RenderFrameLastCount = 0 }


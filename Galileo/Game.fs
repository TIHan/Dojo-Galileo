namespace Game

open System
open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent

type Node = interface end

and [<NoComparison; ReferenceEquality>]
    Node<'T> =
    {
        id: int
        mutable model: 'T
        mutable update: GameEnvironment -> 'T -> unit
        render: (GameEnvironment -> float32 -> 'T -> 'T -> unit) Lazy
    }

    member this.Update env =
        this.update env this.model

    member this.SetUpdate update =
        this.update <- fun env x -> update env.time x

    interface Node

and [<NoComparison; ReferenceEquality>]
    GameEnvironment =
    {
        nodes: (Node option) []
        updates: ((unit -> unit) option) []
        renders: ((float32 -> unit) option) []
        mutable length: int
        mutable time: TimeSpan
        mutable defaultShaderProgram: int
    }

    static member Create () =
        {
            nodes = Array.init (65536) (fun _ -> None)
            updates = Array.init (65536) (fun _ -> None)
            renders = Array.init (65536) (fun _ -> None)
            length = 0
            time = TimeSpan.Zero
            defaultShaderProgram = 0
        }

    member this.CreateNode<'T> (model: 'T, update, render) =
        let node =
            {
                id = 0
                model = model
                update = update
                render = render
            }
        this.AddNode node
        node

    member this.AddNode<'T> (node: Node<'T>) =
        this.nodes.[this.length] <- Some (node :> Node)
        this.updates.[this.length] <- Some (fun () -> node.Update this)
        this.renders.[this.length] <- Some (fun t -> node.render.Force() this t node.model node.model)
        this.length <- this.length + 1

    member this.UpdateNodes () =
        this.updates
        |> Array.iter (fun x ->
            match x with
            | None -> ()
            | Some update -> update ())

    member this.RenderNodes t =
        this.renders
        |> Array.iter (fun x ->
            match x with
            | None -> ()
            | Some render -> render t)
and
    GameField<'T when 'T : unmanaged> =
        {
            history: 'T []
            mutable index: int
            mutable value: 'T
        }

        member this.Value = this.value

        member this.History = this.history |> Array.copy

        static member Create (value: 'T) =
            {
                history = Array.zeroCreate 30
                index = 0
                value = value
            }

        static member (<~) (gf: GameField<'T>, value: 'T) = gf.value <- value   

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

namespace Game

open System
open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent

type NodeCollection =
    {
        mutable nodes: obj
        mutable length: int
        update: GameEnvironment -> int -> unit
        render: GameEnvironment -> float32 -> int -> unit
    }

    static member Create<'T> () =
        let nodes : (Node<'T> option) [] = Array.init (int (Int32.MaxValue / 256)) (fun _ -> None)

        let update =
            fun env currentLength ->
                for i = 0 to currentLength - 1 do
                    match nodes.[i] with
                    | None -> ()
                    | Some node -> 
                        node.Update env //<- Some (node.Update env)

        let render =
            fun env timeDiff currentLength ->
                for i = 0 to currentLength - 1 do
                    match nodes.[i] with
                    | None -> ()
                    | Some node -> 
                        let f = node.render.Force ()
                        f env timeDiff node.previousModel node.currentModel

        {
            nodes = nodes
            length = 0
            update = update
            render = render
        }

    member this.Add<'T> (node: Node<'T>) =
        let nodes = this.nodes :?> ((Node<'T> option) [])
        nodes.[this.length] <- Some node
        this.length <- this.length + 1

    member this.UpdateAll env = this.update env this.length
    member this.RenderAll env timeDiff = this.render env timeDiff this.length

and Node<'T> =
    {
        id: int
        mutable previousModel: 'T
        mutable currentModel: 'T
        mutable update: GameEnvironment -> 'T -> 'T
        render: (GameEnvironment -> float32 -> 'T -> 'T -> unit) Lazy
    }

    member this.Update env =
        this.currentModel <- this.update env this.currentModel
        this.previousModel <- this.currentModel
        ()

    member this.SetUpdate update =
        this.update <- fun env x -> update env.time x

and GameEnvironment =
    {
        mutable time: TimeSpan
        mutable defaultShaderProgram: int
        mutable nodeDict: Dictionary<Type, NodeCollection>
    }

    static member Create () =
        {
            time = TimeSpan.Zero
            defaultShaderProgram = 0
            nodeDict = Dictionary ()
        }

    member this.Time = this.time

    member this.CreateNode<'T> (model: 'T, update, render) =
        let node =
            {
                id = 0
                currentModel = model
                previousModel = model
                update = update
                render = render
            }
        this.AddNode node
        node

    member this.AddNode<'T> (node: Node<'T>) =
        let type' = typeof<'T>

        if this.nodeDict.ContainsKey type' 
        then
            this.nodeDict.[type'].Add node
        else
            let nodes = NodeCollection.Create<'T> ()
            nodes.Add node
            this.nodeDict.[type'] <- nodes

    member this.UpdateNodes () =
        this.nodeDict
        |> Seq.iter (fun x -> 
            x.Value.UpdateAll this)

    member this.RenderNodes timeDiff =
        this.nodeDict
        |> Seq.iter (fun x ->
            x.Value.RenderAll this timeDiff)

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
        let inline time () = stopwatch.Elapsed.Ticks

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
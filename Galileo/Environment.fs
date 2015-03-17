namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic

type NodeCollection =
    {
        mutable nodes: obj
        mutable length: int
        update: Environment -> int -> unit
        render: Environment -> float32 -> int -> unit
    }

    static member Create<'T> () =
        let nodes : (Node<'T> option) [] = Array.init (int Int16.MaxValue) (fun _ -> None)

        let update =
            fun env currentLength ->
                for i = 0 to currentLength - 1 do
                    match nodes.[i] with
                    | None -> ()
                    | Some node -> nodes.[i] <- Some (node.Update env)

        let render =
            fun env timeDiff currentLength ->
                for i = 0 to currentLength - 1 do
                    match nodes.[i] with
                    | None -> ()
                    | Some node -> 
                        let f = node.render.Force ()
                        f env timeDiff node.model

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
        model: 'T
        update: Environment -> 'T -> 'T
        render: (Environment -> float32 -> 'T -> unit) Lazy
    }

    member this.Update env =
        { this with
            model = this.update env this.model
        }

and Environment =
    {
        mutable time: TimeSpan
        mutable defaultShaderProgram: Shader
        mutable nodeDict: Dictionary<Type, NodeCollection>
    }

    static member Create () =
        {
            time = TimeSpan.Zero
            defaultShaderProgram = Shader.Empty
            nodeDict = Dictionary ()
        }

    member this.CreateNode<'T> (model: 'T, update, render) =
        {
            id = 0
            model = model
            update = update
            render = render
        }
        |> this.AddNode

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

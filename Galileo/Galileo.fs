namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic

open Ferop
open Game

type Sphere =
    {
        translation: Matrix4x4
        rotation: Matrix4x4
        color: float32 * float32 * float32
    }

[<RequireQualifiedAccess>]
module Galileo =

    let inline lerp x y t = x + (y - x) * t

    let octahedron_vtx = 
        [|
           Vector3 (0.0f, -1.0f,  0.0f)
           Vector3 (1.0f,  0.0f,  0.0f)
           Vector3 (0.0f,  0.0f,  1.0f)
           Vector3 (-1.0f, 0.0f,  0.0f)
           Vector3 (0.0f,  0.0f, -1.0f)
           Vector3 (0.0f,  1.0f,  0.0f)
        |]

    let octahedron_idx =
        [|
            0; 1; 2;
            0; 2; 3;
            0; 3; 4;
            0; 4; 1;
            1; 5; 2;
            2; 5; 3;
            3; 5; 4;
            4; 5; 1;
        |]

    let mutable lazyF : Lazy<_ * _ * _> = Unchecked.defaultof<Lazy<_ * _ * _>>
    let spawnSphereHandler (env: GameEnvironment) : Node<Sphere> =
        if lazyF = Unchecked.defaultof<Lazy<_ * _ * _>> then
            lazyF <-
                lazy 
                    let vertices =
                        octahedron_idx
                        |> Array.map (fun i -> octahedron_vtx.[i])

                    let trianglesLength = vertices.Length / 3
                    let triangles = Array.zeroCreate<Vector3 * Vector3 * Vector3> trianglesLength

                    for i = 0 to trianglesLength - 1 do
                        let v1 = vertices.[0 + (i * 3)]
                        let v2 = vertices.[1 + (i * 3)]
                        let v3 = vertices.[2 + (i * 3)]
                        triangles.[i] <- (v1, v2, v3)
                   

                    let rec buildSphere n triangles =
                        match n with
                        | 3 -> triangles
                        | _ ->
                            triangles
                            |> Array.map (fun (v1: Vector3, v2: Vector3, v3: Vector3) ->                               
                                let v1 = v1 |> Vector3.Normalize
                                let v2 = Vector3.Normalize v2
                                let v3 = Vector3.Normalize v3
                                let v12 = v2 * 0.5f + v1 * 0.5f |> Vector3.Normalize
                                let v13 = v1 * 0.5f + v3 * 0.5f |> Vector3.Normalize
                                let v23 = v2 * 0.5f + v3 * 0.5f |> Vector3.Normalize
                                [|
                                (v1, v12, v13)
                                (v2, v23, v12)
                                (v3, v13, v23)
                                (v12, v23, v13)
                                |]
                            )
                            |> Array.reduce Array.append
                            |> buildSphere (n + 1)

                    let triangles = buildSphere 0 triangles

                    let vertices =
                        triangles
                        |> Array.map (fun (x, y, z) -> [|x;y;z|])
                        |> Array.reduce Array.append

                    let triangleNormal (v1, v2, v3) = Vector3.Cross (v2 - v1, v3 - v1) |> Vector3.Normalize

                    let normals =
                        vertices
                        |> Array.map (fun v ->
                            match triangles |> Array.filter (fun (v1, v2, v3) -> v.Equals v1 || v.Equals v2 || v.Equals v3) with
                            | trs ->
                                trs
                                |> Array.map triangleNormal
                                |> Array.reduce ((+))
                                |> Vector3.Normalize
                        )

                    let ent : Sphere =
                        {
                            translation = Matrix4x4.Identity
                            rotation = Matrix4x4.Identity
                            color = (0.f, 1.f, 0.f)
                        }

                    let x = fun _ x -> x
                    let y =
                        lazy
                            let nbo = R.CreateVBO normals
                            let vbo = R.CreateVBO vertices

                            fun env t prev curr ->
                                let translation = lerp prev.translation curr.translation t
                                let rotation = lerp prev.rotation curr.rotation t


                                R.SetModel env.defaultShaderProgram (translation * rotation)

                                let r, g, b = curr.color
                                R.SetColor env.defaultShaderProgram r g b

                                let (VBO (nbo, _)) = nbo
                                R.DrawVBOAsTrianglesWithNBO vbo nbo

                    ent, x, y

        env.CreateNode (lazyF.Force())

    let spawnMultipleSpheresHandler env =
        Array.init 100 (fun _ -> spawnSphereHandler env)

    [<RequireQualifiedAccess>]
    type Command =
        | SpawnSphere of AsyncReplyChannel<Node<Sphere>>
        | SpawnMultipleSpheres of AsyncReplyChannel<Node<Sphere>[]>

    let window = ref IntPtr.Zero
    let proc = new MailboxProcessor<Command> (fun inbox ->
        let window = !window

        let env = GameEnvironment.Create ()

        let handleMessages =
            function
            | Command.SpawnSphere (ch) -> spawnSphereHandler env |> ch.Reply
            | Command.SpawnMultipleSpheres (ch) -> spawnMultipleSpheresHandler env |> ch.Reply

        let rec loop () = async {
            let rec executeCommands () =
                match inbox.CurrentQueueLength with
                | 0 -> ()
                | _ ->
                    handleMessages (inbox.Receive () |> Async.RunSynchronously)
                    executeCommands ()

            let r = R.Init window
            let shaderProgram = R.LoadShaders ()

            env.defaultShaderProgram <- shaderProgram
            GameLoop.start id
                // server/client
                (fun time interval ->
                    env.time <- TimeSpan.FromTicks time
                    GC.Collect (0, GCCollectionMode.Forced, true)

                    executeCommands ()

                    env.UpdateNodes ()
                )
                // client/render
                (fun t ->
                    R.Clear ()

                    let cameraPosition = Vector3 (0.f, 0.f, 8.f)

                    let projection = Matrix4x4.CreatePerspectiveFieldOfView (90.f * 0.0174532925f, (400.f / 400.f), 0.1f, 100.f)
                    let view = Matrix4x4.CreateLookAt (cameraPosition, Vector3 (0.f, 0.f, 0.f), Vector3 (0.f, 1.f, 0.f))
                    let model = Matrix4x4.Identity

                    R.SetProjection shaderProgram projection
                    R.SetView shaderProgram view
                    R.SetModel shaderProgram model
                    R.SetCameraPosition shaderProgram cameraPosition

                    env.RenderNodes (t)

                    R.Draw r
                )
        }
        loop ())

    let init () =
        printfn "Begin Initializing Galileo"
        window := R.CreateWindow ()
        proc.Start ()
        proc.Error.Add (fun ex -> printfn "%A" ex)
        ()

    let spawnSphere () =
        proc.PostAndReply (fun ch -> Command.SpawnSphere (ch))

    let spawnMultipleSpheres () =
        proc.PostAndReply (fun ch -> Command.SpawnMultipleSpheres (ch))

module Node =
    let setUpdate f (node: Node<'T>) =
        node.SetUpdate f
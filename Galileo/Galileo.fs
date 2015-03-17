namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic

type Environment = Galileo.Environment

type Triangle =
    {
        vertices: Vector3 []
        color: float32 * float32 * float32
    }

type Octahedron =
    {
        vertices: Vector3 []
        indices: int []
        color: float32 * float32 * float32
    }

type Sphere = Sphere of unit

[<RequireQualifiedAccess>]
module Galileo =

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

    [<RequireQualifiedAccess>]
    type Command =
        | SpawnRedTriangle
        | SpawnBlueTriangle
        | SpawnOctahedron

    let window = ref IntPtr.Zero
    let proc = new MailboxProcessor<Command> (fun inbox ->
        let window = !window

        let env = Environment.Create ()

        let handleMessages =
            function
            | Command.SpawnRedTriangle ->
                let ent : Triangle = 
                    {
                        vertices = [|Vector3 (0.f, 1.f, 0.f); Vector3 (-1.f, -1.f, 0.f); Vector3 (1.f, -1.f, 0.f)|]
                        color = (1.f, 0.f, 0.f)
                    }
                
                let x = fun _ x -> x
                let y = 
                    lazy
                        let vbo = R.CreateVBO (ent.vertices)
                        fun (env: Environment) timeDiff model ->
                            let r, g, b = ent.color
                            //R.SetColor env.defaultShaderProgram r g b

                            let value = cos(single env.time.Seconds)
                            R.SetVBO (ent.vertices |> Array.map (fun x -> Vector3 (x.X * value, x.Y, x.Z)), vbo)
                            R.DrawVBOAsTriangles vbo
                env.CreateNode<Triangle> (ent, x, y)

            | Command.SpawnBlueTriangle ->
                let ent : Triangle = 
                    {
                        vertices = [|Vector3 (0.f, -1.f, 0.f); Vector3 (1.f, 1.f, 0.f); Vector3 (-1.f, 1.f, 0.f)|]
                        color = (0.f, 0.f, 1.f)
                    }

                let x = fun _ x -> x
                let y = 
                    lazy
                        let vbo = R.CreateVBO (ent.vertices)
                        fun (env: Environment) timeDiff model ->
                            let r, g, b = ent.color
                            //R.SetColor env.defaultShaderProgram r g b
                            R.DrawVBOAsTriangles vbo
                env.CreateNode<Triangle> (ent, x, y)

            | Command.SpawnOctahedron ->
                let ent : Octahedron =
                    {
                        vertices = octahedron_vtx
                        indices = octahedron_idx
                        color = (0.f, 1.f, 0.f)
                    }

                let y =
                    lazy
                        let vertices =
                            ent.indices
                            |> Array.map (fun i -> ent.vertices.[i])

                        let trianglesLength = vertices.Length / 3
                        let triangles = Array.zeroCreate<Vector3 * Vector3 * Vector3> trianglesLength

                        for i = 0 to trianglesLength - 1 do
                            let v1 = vertices.[0 + (i * 3)]
                            let v2 = vertices.[1 + (i * 3)]
                            let v3 = vertices.[2 + (i * 3)]
                            triangles.[i] <- (v1, v2, v3)

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

                        let nbo = R.CreateVBO normals
                        let vbo = R.CreateVBO vertices

                        fun env timeDiff model ->
                            let r, g, b = ent.color
                            ()
                            //R.SetColor env.defaultShaderProgram r g b

                let x = fun _ x -> x
                env.CreateNode (ent, x, y)

        let rec loop () = async {
            let rec executeCommands () =
                match inbox.CurrentQueueLength with
                | 0 -> ()
                | _ ->
                    handleMessages (inbox.Receive () |> Async.RunSynchronously)

            let r = R.Init window
            R.CreateVAO () // fixme:
            let shader = R.CreateShader "v.vertex" "f.fragment" ["uni_mvp"; "uni_view"; "uni_model"; "uni_cameraPosition"; "uni_color"]
            env.defaultShaderProgram <- shader

            GameLoop.start id
                // server/client
                (fun time interval ->
                    GC.Collect (0, GCCollectionMode.Forced, true)
                    env.time <- TimeSpan.FromTicks (time)

                    executeCommands ()

                    env.UpdateNodes ()
                )
                // client/render
                (fun t ->
                    R.Clear ()

                    let cameraPosition = Vector3 (2.f, 2.f, 3.f)

                    let projection = Matrix4x4.CreatePerspectiveFieldOfView (90.f * 0.0174532925f, (400.f / 400.f), 0.1f, 100.f) |> Matrix4x4.Transpose
                    let view = Matrix4x4.CreateLookAt (cameraPosition, Vector3 (0.f, 0.f, 0.f), Vector3 (0.f, 1.f, 0.f)) |> Matrix4x4.Transpose
                    let model = Matrix4x4.Identity
                    let mvp = projection * view * model |> Matrix4x4.Transpose

                    //R.SetMVP shaderProgram mvp
                    //R.SetView shaderProgram view
                    //R.SetModel shaderProgram model
                    //R.SetCameraPosition shaderProgram cameraPosition

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

    let spawnRedTriangle () =
        proc.Post (Command.SpawnRedTriangle)

    let spawnBlueTriangle () =
        proc.Post (Command.SpawnBlueTriangle)

    let spawnOctahedron () =
        proc.Post (Command.SpawnOctahedron)
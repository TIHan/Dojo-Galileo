namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic

open Ferop
open Game
open Input

type MouseState = Input.MouseState
type MouseButtonType = Input.MouseButtonType

[<NoComparison; ReferenceEquality>]
type Planet =
    {
        translation: Matrix4x4
        rotation: Matrix4x4
        scale: Matrix4x4
        rotationAmount: single
        r: float32
        g: float32
        b: float32
    }

[<RequireQualifiedAccess>]
module Galileo =

    let LunarDistance = 384400.f

    let prevCameraPosition = ref Vector3.Zero
    let cameraPosition = ref Vector3.Zero
    let updateCameraPosition = ref (fun () -> Vector3.Zero)

    let prevLookAtPosition = ref Vector3.Zero
    let lookAtPosition = ref Vector3.Zero
    let updateLookAtPosition = ref (fun () -> Vector3.Zero)

    let prevView = ref Matrix4x4.Identity
    let view = ref Matrix4x4.Identity

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

    let spawnSphereHandler textureFileName (env: GameEnvironment) : Entity<Planet> =
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

        let ent : Planet =
            {
                translation = Matrix4x4.Identity
                rotation = Matrix4x4.Identity
                scale = Matrix4x4.Identity
                rotationAmount = 0.f
                r = 0.f
                g = 1.f
                b = 0.f
            }

        let textureId = R.CreateTexture textureFileName
        let nbo = R.CreateVBO normals
        let vbo = R.CreateVBO vertices

        let x = fun _ x -> x
        let y =
            fun env prev curr ->
                let translation = Matrix4x4.Lerp (prev.translation, curr.translation, env.renderDelta)
                let q1 = Quaternion.CreateFromRotationMatrix (prev.rotation)
                let q2 = Quaternion.CreateFromRotationMatrix (curr.rotation)
                let rotation = Quaternion.Slerp (q1, q2, env.renderDelta)
                let rotation = Matrix4x4.CreateFromQuaternion (rotation)
                let scale = Matrix4x4.Lerp (prev.scale, curr.scale, env.renderDelta)

                R.SetModel env.planetShaderProgram (scale * Matrix4x4.CreateRotationX(-single Math.PI / 2.f) * rotation * translation)

                let r = curr.r
                let g = curr.g
                let b = curr.b
                R.SetColor env.planetShaderProgram r g b

                R.SetTexture env.planetShaderProgram textureId

                R.BindTexture textureId
                let (VBO (nbo, _)) = nbo

                R.DrawVBOAsTrianglesWithNBO vbo nbo

        env.CreateEntity (ent, x, y)

    let spawnSpheresHandler env amount =
        Array.init amount (fun _ -> spawnSphereHandler env)

    [<RequireQualifiedAccess; NoComparison; ReferenceEquality>]
    type Command =
        | SpawnSphere of string * AsyncReplyChannel<Entity<Planet>>
        | SpawnBackground

    let window = ref IntPtr.Zero
    let env = ref Unchecked.defaultof<GameEnvironment>
    let proc = new MailboxProcessor<Command> (fun inbox ->
        let window = !window

        env := GameEnvironment.Create ()
        let env = !env

        let backgroundEntity : Ref<IEntity> = ref Unchecked.defaultof<IEntity>

        let handleMessages =
            function
            | Command.SpawnSphere (textureFileName, ch) -> spawnSphereHandler textureFileName env |> ch.Reply
            | Command.SpawnBackground ->
                let textureId = R.CreateTexture "background.jpg"
                let vbo = 
                    R.CreateVBO ([|
                                    Vector3 (-1.f, -1.f, 0.1f)
                                    Vector3 (-1.f, 1.f, 0.1f)
                                    Vector3 (1.f, -1.f, 0.1f)
                                    Vector3 (1.f, -1.f, 0.1f)
                                    Vector3 (-1.f, 1.f, 0.1f)
                                    Vector3 (1.f, 1.f, 0.1f)
                    |] |> Array.rev)

                let x = fun _ x -> x
                let y =
                    fun env prev curr ->
                        let translation = Matrix4x4.Identity

                        R.SetModel env.backgroundShaderProgram (translation)

                        R.SetTexture env.backgroundShaderProgram textureId

                        R.BindTexture textureId

                        R.DrawVBOAsTriangles2 vbo

                backgroundEntity := env.CreateEntityWithoutAdding ((), x, y) :> IEntity

        let rec loop () = async {
            let rec executeCommands () =
                match inbox.CurrentQueueLength with
                | 0 -> ()
                | _ ->
                    handleMessages (inbox.Receive () |> Async.RunSynchronously)
                    executeCommands ()

            let r = R.Init window
            let planetShaderProgram = R.LoadShaders ("planet_vertex.glsl", "planet_fragment.glsl")
            let backgroundShaderProgram = R.LoadShaders ("background_vertex.glsl", "background_fragment.glsl")
            let vao = R.CreateVao ()

            env.planetShaderProgram <- planetShaderProgram
            env.backgroundShaderProgram <- backgroundShaderProgram

            inbox.Post (Command.SpawnBackground)

            GameLoop.start
                (fun () ->
                    Input.poll ()
                )
                // server/client
                (fun time interval ->
                    Input.clearEvents ()

                    env.time <- TimeSpan.FromTicks time
                    env.interval <- TimeSpan.FromTicks interval
                    GC.Collect (0, GCCollectionMode.Forced, true)

                    executeCommands ()

                    env.UpdateEntities ()
                    env.CommitUpdateEntities ()

                    prevCameraPosition := !cameraPosition
                    cameraPosition := (!updateCameraPosition)()

                    prevLookAtPosition := !lookAtPosition
                    lookAtPosition := (!updateLookAtPosition)()

                    prevView := !view
                    view := Matrix4x4.CreateLookAt (!cameraPosition, !lookAtPosition, Vector3.UnitY)
                )
                // client/render
                (fun renderDelta ->
                    env.renderDelta <- renderDelta

                    let cameraPosition = Vector3.Lerp (!prevCameraPosition, !cameraPosition, renderDelta)
                    let view = Matrix4x4.Lerp (!prevView, !view, renderDelta)

                    R.Clear ()

                    R.UseProgram (env.backgroundShaderProgram)
                    let projection = Matrix4x4.CreateOrthographic (1.f, 1.f, 0.1f, Single.MaxValue)
                    R.SetProjection backgroundShaderProgram projection

                    backgroundEntity.Value.Render env

                    R.UseProgram (env.planetShaderProgram)
                    let projection = Matrix4x4.CreatePerspectiveFieldOfView (90.f * 0.0174532925f, (1280.f / 720.f), 100.f, Single.MaxValue)
                    let view = view
                    R.SetProjection planetShaderProgram projection
                    R.SetView planetShaderProgram view
                    R.SetCameraPosition planetShaderProgram cameraPosition

                    R.EnableDepth ()
                    env.RenderEntities ()
                    R.DisableDepth ()

                    R.Draw r
                )
        }
        loop ())

    let init () =
        printfn "Begin Initializing Galileo"

        R.InitSDL ()
        window := R.CreateWindow ()

        proc.Start ()
        proc.Error.Add (fun ex -> printfn "%A" ex)

    let spawnPlanet textureFileName =
        proc.PostAndReply (fun ch -> Command.SpawnSphere (textureFileName, ch))

    let getInputEvents () = Input.getEvents ()

    let getMouse () = Input.getMouse ()

    let isKeyPressed key = Input.isKeyPressed key

    let isMouseButtonPressed btn = Input.isMouseButtonPressed btn

    let setUpdateCameraPosition f = updateCameraPosition := f

    let setUpdateLookAtPosition f = updateLookAtPosition := f

    let entitiesIter f = (!env).entities |> Array.iter (fun x -> match x with | None -> () | Some x -> f x)

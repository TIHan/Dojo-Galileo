namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics

open Ferop

[<Struct>]
type Renderer =
    val Window : nativeint
    val GLContext : nativeint

[<Struct>]
type DrawTriangle =
    val X : Vector3
    val Y : Vector3
    val Z : Vector3

    new (x, y, z) = { X = x; Y = y; Z = z }
 
type Server<'a> = Server of state: 'a * update: (int64 -> 'a -> 'a)

type Client<'a, 'b> = Client of map: ('a -> 'b) * state: 'b * render: (float32 -> 'b -> 'b -> unit)

type ClientServer<'a, 'b> =
    {
        server: Server<'a>
        client: Client<'a, 'b>
    }

type Triangle =
    {
        data: DrawTriangle
        color: float32 * float32 * float32
    }

type Octahedron =
    {
        vertices: single []
        indices: int []
        color: float32 * float32 * float32
    }

type Sphere = Sphere of unit

[<RequireQualifiedAccess>]
type Entity =
    | Triangle of Triangle
    | Octahedron of Octahedron
    | Sphere of Sphere

[<Ferop>]
[<ClangOsx (
    "-DGL_GLEXT_PROTOTYPES -I/Library/Frameworks/SDL2.framework/Headers",
    "-F/Library/Frameworks -framework Cocoa -framework OpenGL -framework IOKit -framework SDL2"
)>]
[<GccLinux ("-I../../include/SDL2", "-lSDL2")>]
#if __64BIT__
[<MsvcWin (""" /I ..\include\SDL2 /I ..\include ..\lib\win\x64\SDL2.lib ..\lib\win\x64\SDL2main.lib ..\lib\win\x64\glew32.lib opengl32.lib """)>]
#else
[<MsvcWin (""" /I  ..\include\SDL2 /I  ..\include  ..\lib\win\x86\SDL2.lib  ..\lib\win\x86\SDL2main.lib  ..\lib\win\x86\glew32.lib opengl32.lib """)>]
#endif
[<Header ("""
#include <stdio.h>
#if defined(__GNUC__)
#   include "SDL.h"
#   include "SDL_opengl.h"
#else
#   include "SDL.h"
#   include <GL/glew.h>
#   include <GL/wglew.h>
#endif
""")>]

module R = 

    [<Import; MI (MIO.NoInlining)>]
    let createWindow () : nativeint =
        C """
        SDL_Init (SDL_INIT_VIDEO);
        return
        SDL_CreateWindow(
            "Galileo",
            SDL_WINDOWPOS_UNDEFINED,
            SDL_WINDOWPOS_UNDEFINED,
            600, 600,
            SDL_WINDOW_OPENGL);
        """

    [<Import; MI (MIO.NoInlining)>]
    let init (window: nativeint) : Renderer =
        C """
        R_Renderer r;

        r.Window = window;

        SDL_GL_SetAttribute (SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL_GL_SetAttribute (SDL_GL_CONTEXT_MINOR_VERSION, 2);
        SDL_GL_SetAttribute (SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_CORE);

        r.GLContext = SDL_GL_CreateContext ((SDL_Window*)r.Window);
        SDL_GL_SetSwapInterval (0);

        #if defined(__GNUC__)
        #else
        glewExperimental = GL_TRUE;
        glewInit ();
        #endif

        return r;
        """

    [<Import; MI (MIO.NoInlining)>]
    let exit (r: Renderer) : int =
        C """
        SDL_GL_DeleteContext (r.GLContext);
        SDL_DestroyWindow ((SDL_Window*)r.Window);
        SDL_Quit ();
        return 0;
        """
    
    [<Import; MI (MIO.NoInlining)>]
    let clear () : unit = C """ glClear (GL_COLOR_BUFFER_BIT); """

    [<Import; MI (MIO.NoInlining)>]
    let draw (app: Renderer) : unit = C """ SDL_GL_SwapWindow ((SDL_Window*)app.Window); """

    [<Import; MI (MIO.NoInlining)>]
    let generateVbo (size: int) (data: single []) : int =
        C """
        GLuint vbo;
        glGenBuffers (1, &vbo);

        glBindBuffer (GL_ARRAY_BUFFER, vbo);

        glBufferData (GL_ARRAY_BUFFER, size, data, GL_DYNAMIC_DRAW);
        return vbo;
        """

    [<Import; MI (MIO.NoInlining)>]
    let drawVbo (offset: int) (size: int) (vbo: int) : unit =
        C """
        glBindBuffer (GL_ARRAY_BUFFER, vbo);
        glEnableVertexAttribArray(0);

        glVertexAttribPointer(
            0,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
            3,                  // size
            GL_FLOAT,           // type
            GL_FALSE,           // normalized?
            0,                  // stride
            (void*)0            // array buffer offset
        );

        glDrawArrays(GL_TRIANGLES, offset, size); // 3 indices starting at 0 -> 1 triangle
        glDisableVertexAttribArray(0);
        """

    [<Import; MI (MIO.NoInlining)>]
    let drawIndices (count: int) (indices: int []) (vbo: int) : unit =
        C """
        glBindBuffer (GL_ARRAY_BUFFER, vbo);
        glEnableVertexAttribArray(0);

        glVertexAttribPointer(
            0,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
            3,                  // size
            GL_FLOAT,           // type
            GL_FALSE,           // normalized?
            0,                  // stride
            (void*)0            // array buffer offset
        );

        glDrawElements(GL_TRIANGLES, count, GL_UNSIGNED_INT, indices);
        glDisableVertexAttribArray(0);
        """

    [<Import; MI (MIO.NoInlining)>]
    let drawColor (shaderProgram: int) (r: single) (g: single) (b: single) : unit = 
        C """
        GLint uni_color = glGetUniformLocation (shaderProgram, "uni_color");
        glUniform4f (uni_color, r, g, b, 0.0f);
        """

    [<Import; MI (MIO.NoInlining)>]
    let loadRawShaders (vertexSource: byte[]) (fragmentSource: byte[]) : int =
        C """
        GLuint vertexShader = glCreateShader (GL_VERTEX_SHADER);
        glShaderSource (vertexShader, 1, (const GLchar*const*)&vertexSource, NULL);    
        glCompileShader (vertexShader);

        GLuint fragmentShader = glCreateShader (GL_FRAGMENT_SHADER);
        glShaderSource (fragmentShader, 1, (const GLchar*const*)&fragmentSource, NULL);
        glCompileShader (fragmentShader);

        /******************************************************/

        GLuint shaderProgram = glCreateProgram ();
        glAttachShader (shaderProgram, vertexShader);
        glAttachShader (shaderProgram, fragmentShader);

        glLinkProgram (shaderProgram);

        glUseProgram (shaderProgram);

        /******************************************************/

        GLuint vao;
        glGenVertexArrays (1, &vao);

        glBindVertexArray (vao);

        return shaderProgram;
        """

    let loadShaders () =
        let mutable vertexFile = ([|0uy|]) |> Array.append (File.ReadAllBytes ("v.vertex"))
        let mutable fragmentFile = ([|0uy|]) |> Array.append (File.ReadAllBytes ("f.fragment"))

        loadRawShaders vertexFile fragmentFile

// http://gafferongames.com/game-physics/fix-your-timestep/
module GameLoop =
    type private GameLoop<'T> = { 
        State: 'T
        PreviousState: 'T
        LastTime: int64
        UpdateTime: int64
        UpdateAccumulator: int64
        RenderAccumulator: int64
        RenderFrameCount: int
        RenderFrameCountTime: int64
        RenderFrameLastCount: int }

    let start (state: 'T) (pre: unit -> unit) (update: int64 -> int64 -> 'T -> 'T) (render: float32 -> 'T -> 'T -> unit) =
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
                    let state = update gl.UpdateTime targetUpdateInterval gl.State

                    processUpdate
                        { gl with 
                            State = state
                            PreviousState = gl.State
                            UpdateTime = gl.UpdateTime + targetUpdateInterval
                            UpdateAccumulator = gl.UpdateAccumulator - targetUpdateInterval }
                else
                    gl

            let processRender gl =
                if gl.RenderAccumulator >= targetRenderInterval then
                    render (single gl.UpdateAccumulator / single targetUpdateInterval) gl.PreviousState gl.State

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
            { State = state
              PreviousState = state
              LastTime = 0L
              UpdateTime = 0L
              UpdateAccumulator = targetUpdateInterval
              RenderAccumulator = 0L
              RenderFrameCount = 0
              RenderFrameCountTime = 0L
              RenderFrameLastCount = 0 }

[<RequireQualifiedAccess>]
module Galileo =

    let octahedron_vtx = 
        [|
           0.0f; -1.0f;  0.0f;
           1.0f;  0.0f;  0.0f;
           0.0f;  0.0f;  1.0f;
          -1.0f;  0.0f;  0.0f;
           0.0f;  0.0f; -1.0f;
           0.0f;  1.0f;  0.0f
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

    let window = ref System.IntPtr.Zero

    let bufferInfo = System.Collections.Generic.Dictionary<int, int * (single * single * single)> ()

    let addDrawTriangle (datum: DrawTriangle) color =
        let size = sizeof<DrawTriangle>

        let data =
            [|datum.X;datum.Y;datum.Z|]
            |> Array.map (fun v -> [|v.X;v.Y;v.Z|])
            |> Array.reduce Array.append

        let vbo = R.generateVbo size data
        bufferInfo.Add (vbo, (size, color))

    let addOctahedron color =
        let size = sizeof<single> * octahedron_vtx.Length
        let vbo = R.generateVbo size octahedron_vtx
        bufferInfo.Add (vbo, (size, color))

    type Galileo =
        {
            clientEntities: Client<Entity, Entity> list
        }

    let proc = new MailboxProcessor<AsyncReplyChannel<unit> * (unit -> unit)>(fun inbox ->
        let rec loop () = async {
            let r = R.init (!window)
            let shaderProgram = R.loadShaders ()

            let initial =
                {
                    clientEntities = []
                }

            GameLoop.start initial id
                // server/client
                (fun time interval curr ->
                    GC.Collect (0, GCCollectionMode.Forced, true)

                    curr
                )
                // client/render
                (fun t _ curr ->
                    R.clear ()

                    curr.clientEntities
                    |> List.iter (fun x ->
                        match x with
                        | Client (_, ent, render) -> render t ent ent                       
                    )

                    R.draw r
                )
        }
        loop ())

    let init () =
        printfn "Begin Initializing Galileo"
        window := R.createWindow ()
        proc.Start ()
        ()

    let spawnDefaultRedTriangle () : Async<Triangle> = async {
        let ent : Triangle = 
            {
                data = DrawTriangle (Vector3 (0.f, 1.f, 0.f), Vector3 (-1.f, -1.f, 0.f), Vector3 (1.f, -1.f, 0.f))
                color = (1.f, 0.f, 0.f)
            }

        let sv = Server (ent, fun _ _ -> ent)
        let cl = Client (id, ent, fun _ _ ent ->
            ()
        )

        return ent
    }

    let spawnDefaultBlueTriangle () : Async<Triangle> = async {
        let ent : Triangle = 
            {
                data = DrawTriangle (Vector3 (0.f, -1.f, 0.f), Vector3 (1.f, 1.f, 0.f), Vector3 (-1.f, 1.f, 0.f))
                color = (0.f, 0.f, 1.f)
            }

        let sv = Server (ent, fun _ _ -> ent)
        let cl = Client (id, ent, fun _ _ _ -> ())

        return ent
    }

    let spawnDefaultOctahedron () : Async<Octahedron> = async {
        let ent : Octahedron =
            {
                vertices = octahedron_vtx
                indices = octahedron_idx
                color = (0.f, 1.f, 0.f)
            }

        let sv = Server (ent, fun _ _ -> ent)
        let cl = Client (id, ent, fun _ _ _ -> ())

        return ent
    }

    let lift<'a when 'a :> Entity> (f: 'a -> Entity) sv =
        match sv with
        | Server (state, update) ->
            Server (f (state), fun t x ->
                let x : 'a = x :> 'a
                update t x
            )

    let spawnDefaultSphere () : Async<Sphere> = async {
        let sv = Server (Sphere (), fun _ x -> x)
        let cl = Client (id, Sphere (), fun _ _ _ -> ())

        return Sphere ()
    }


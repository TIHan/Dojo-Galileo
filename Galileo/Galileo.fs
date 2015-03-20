namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic

open Ferop
open Game

[<Struct>]
type RendererContext =
    val Window : nativeint
    val GLContext : nativeint

type VBO = VBO of id: int * size: int

[<Ferop>]
[<ClangOsx (
    "-DGL_GLEXT_PROTOTYPES -I/Library/Frameworks/SDL2.framework/Headers",
    "-F/Library/Frameworks -framework Cocoa -framework OpenGL -framework IOKit -framework SDL2"
)>]
[<GccLinux ("-I../../include/SDL2", "-lSDL2")>]
#if __64BIT__
[<MsvcWin ("""/O2 /I ..\include\SDL2 /I ..\include ..\lib\win\x64\SDL2.lib ..\lib\win\x64\SDL2main.lib ..\lib\win\x64\glew32.lib opengl32.lib """)>]
#else
[<MsvcWin ("""/O2 /I  ..\include\SDL2 /I  ..\include  ..\lib\win\x86\SDL2.lib  ..\lib\win\x86\SDL2main.lib  ..\lib\win\x86\glew32.lib opengl32.lib """)>]
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
[<Source ("""
char VertexShaderErrorMessage[65536];
char FragmentShaderErrorMessage[65536];
char ProgramErrorMessage[65536];
""")>]
type R private () = 

    [<Export>]
    static member private Failwith (size: int, ptr: nativeptr<sbyte>) : unit =
        let str = String (ptr)
        failwith str

    [<Import; MI (MIO.NoInlining)>]
    static member private _CreateBuffer_float32 (size: int, data: float32 []) : int =
        C """
        GLuint buffer;

        glGenBuffers (1, &buffer);
        glBindBuffer (GL_ARRAY_BUFFER, buffer);
        glBufferData (GL_ARRAY_BUFFER, size, data, GL_DYNAMIC_DRAW);
        glBindBuffer (GL_ARRAY_BUFFER, 0);

        return buffer;
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _CreateBuffer_vector3 (size: int, data: Vector3 []) : int =
        C """
        GLuint buffer;

        glGenBuffers (1, &buffer);
        glBindBuffer (GL_ARRAY_BUFFER, buffer);
        glBufferData (GL_ARRAY_BUFFER, size, data, GL_DYNAMIC_DRAW);
        glBindBuffer (GL_ARRAY_BUFFER, 0);

        return buffer;
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _SetBuffer (size: int, data: Vector3 [], vbo: int) : unit =
        C """
        glBindBuffer (GL_ARRAY_BUFFER, vbo);
        glBufferSubData (GL_ARRAY_BUFFER, 0, size, data);
        glBindBuffer (GL_ARRAY_BUFFER, 0);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _DrawBufferAsTriangles (size: int, vbo: int) : unit =
        C """
        glBindBuffer (GL_ARRAY_BUFFER, vbo);

        glEnableVertexAttribArray (0);
        glVertexAttribPointer (
            0,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
            3,                  // size
            GL_FLOAT,           // type
            GL_FALSE,           // normalized?
            0,                  // stride
            (void*)0            // array buffer offset
        );

        glEnableVertexAttribArray (1);
        glVertexAttribPointer (
            1,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
            3,                  // size
            GL_FLOAT,           // type
            GL_FALSE,           // normalized?
            0,                  // stride
            (void*)0            // array buffer offset
        );

        glDrawArrays (GL_TRIANGLES, 0, size);

        glDisableVertexAttribArray (1);
        glDisableVertexAttribArray (0);
        glBindBuffer (GL_ARRAY_BUFFER, 0);
        """

    // FIXME:
    [<Import; MI (MIO.NoInlining)>]
    static member private _DrawBufferAsTrianglesWithNBO (size: int, vbo: int, nbo: int) : unit =
        C """
        glEnableVertexAttribArray (0);
        glBindBuffer (GL_ARRAY_BUFFER, vbo);
        glVertexAttribPointer (
            0,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
            3,                  // size
            GL_FLOAT,           // type
            GL_FALSE,           // normalized?
            0,                  // stride
            (void*)0            // array buffer offset
        );

        glEnableVertexAttribArray (1);
        glBindBuffer (GL_ARRAY_BUFFER, nbo);
        glVertexAttribPointer (
            1,                  // attribute 0. No particular reason for 0, but must match the layout in the shader.
            3,                  // size
            GL_FLOAT,           // type
            GL_FALSE,           // normalized?
            0,                  // stride
            (void*)0            // array buffer offset
        );

        glDrawArrays (GL_TRIANGLES, 0, size);

        glDisableVertexAttribArray (1);
        glDisableVertexAttribArray (0);
        glBindBuffer (GL_ARRAY_BUFFER, 0);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _LoadShaders (vertexSource: byte[]) (fragmentSource: byte[]) : int =
        C """
        // Create the shaders
        GLuint VertexShaderID = glCreateShader(GL_VERTEX_SHADER);
        GLuint FragmentShaderID = glCreateShader(GL_FRAGMENT_SHADER);

        GLint Result = GL_FALSE;
        int InfoLogLength;



        // Compile Vertex Shader
        glShaderSource(VertexShaderID, 1, &vertexSource, NULL);
        glCompileShader(VertexShaderID);

        // Check Vertex Shader
        glGetShaderiv(VertexShaderID, GL_COMPILE_STATUS, &Result);
        glGetShaderiv(VertexShaderID, GL_INFO_LOG_LENGTH, &InfoLogLength);
        if ( InfoLogLength > 0 ){
            glGetShaderInfoLog(VertexShaderID, InfoLogLength, &InfoLogLength, &VertexShaderErrorMessage[0]);
            if (InfoLogLength > 0)
            {
                R_Failwith(InfoLogLength, &VertexShaderErrorMessage[0]);
            }
            for (int i = 0; i < 65536; ++i) { VertexShaderErrorMessage[i] = '\0'; }
        }



        // Compile Fragment Shader
        glShaderSource(FragmentShaderID, 1, &fragmentSource, NULL);
        glCompileShader(FragmentShaderID);

        // Check Fragment Shader
        glGetShaderiv(FragmentShaderID, GL_COMPILE_STATUS, &Result);
        glGetShaderiv(FragmentShaderID, GL_INFO_LOG_LENGTH, &InfoLogLength);
        if ( InfoLogLength > 0 ){
            glGetShaderInfoLog(FragmentShaderID, InfoLogLength, &InfoLogLength, &FragmentShaderErrorMessage[0]);
            if (InfoLogLength > 0)
            {
                R_Failwith(InfoLogLength, &FragmentShaderErrorMessage[0]);
            }
            for (int i = 0; i < 65536; ++i) { FragmentShaderErrorMessage[i] = '\0'; }
        }



        // Link the program
        printf("Linking program\n");
        GLuint ProgramID = glCreateProgram();
        glAttachShader(ProgramID, VertexShaderID);
        glAttachShader(ProgramID, FragmentShaderID);


        glLinkProgram(ProgramID);

        // Check the program
        glGetProgramiv(ProgramID, GL_LINK_STATUS, &Result);
        glGetProgramiv(ProgramID, GL_INFO_LOG_LENGTH, &InfoLogLength);
        if ( InfoLogLength > 0 ){
            glGetProgramInfoLog(ProgramID, InfoLogLength, &InfoLogLength, &ProgramErrorMessage[0]);
            if (InfoLogLength > 0)
            {
                R_Failwith(InfoLogLength, &ProgramErrorMessage[0]);
            }
            for (int i = 0; i < 65536; ++i) { ProgramErrorMessage[i] = '\0'; }
        }

        glUseProgram (ProgramID);

        /******************************************************/

        GLuint vao;
        glGenVertexArrays (1, &vao);

        glBindVertexArray (vao);

        return ProgramID;
        """

    [<Import; MI (MIO.NoInlining)>]
    static member CreateWindow () : nativeint =
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
    static member Init (window: nativeint) : RendererContext =
        C """
        R_RendererContext r;

        r.Window = window;

        SDL_GL_SetAttribute (SDL_GL_CONTEXT_MAJOR_VERSION, 3);
        SDL_GL_SetAttribute (SDL_GL_CONTEXT_MINOR_VERSION, 3);
        SDL_GL_SetAttribute (SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_CORE);

        r.GLContext = SDL_GL_CreateContext ((SDL_Window*)r.Window);
        SDL_GL_SetSwapInterval (1);

        #if defined(__GNUC__)
        #else
        glewExperimental = GL_TRUE;
        glewInit ();
        #endif

        return r;
        """

    [<Import; MI (MIO.NoInlining)>]
    static member Exit (r: RendererContext) : int =
        C """
        SDL_GL_DeleteContext (r.GLContext);
        SDL_DestroyWindow ((SDL_Window*)r.Window);
        SDL_Quit ();
        return 0;
        """
    
    [<Import; MI (MIO.NoInlining)>]
    static member Clear () : unit = C """ 
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    
	// Enable depth test
	glEnable(GL_DEPTH_TEST);
	// Accept fragment if it closer to the camera than the former one
	glDepthFunc(GL_LESS); 

	// Cull triangles which normal is not towards the camera
	glEnable(GL_CULL_FACE);
    
    """

    [<Import; MI (MIO.NoInlining)>]
    static member Draw (r: RendererContext) : unit = C """ SDL_GL_SwapWindow ((SDL_Window*)r.Window); """

    static member CreateVBO (data: float32 []) : VBO =
        let size = data.Length * sizeof<float32>
        let id = R._CreateBuffer_float32 (size, data)
        VBO (id, size)

    static member CreateVBO (data: Vector3 []) : VBO =
        let size = data.Length * sizeof<Vector3>
        let id = R._CreateBuffer_vector3 (size, data)
        VBO (id, size)

    static member SetVBO (data: Vector3 [], (VBO (id, _))) =
       let size = data.Length * sizeof<Vector3>
       R._SetBuffer (size, data, id)
 
    static member DrawVBOAsTriangles (VBO (id, size)) : unit = 
        R._DrawBufferAsTriangles (size, id)

    // FIXME:
    static member DrawVBOAsTrianglesWithNBO (VBO (id, size)) nbo : unit = 
        R._DrawBufferAsTrianglesWithNBO (size, id, nbo)

    [<Import; MI (MIO.NoInlining)>]
    static member SetColor (shaderProgram: int) (r: single) (g: single) (b: single) : unit = 
        C """
        GLint uni_color = glGetUniformLocation (shaderProgram, "uni_color");
        glUniform4f (uni_color, r, g, b, 0.0f);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member SetProjection (shaderProgram: int) (projection: Matrix4x4) : unit =
        C """
        GLuint uni = glGetUniformLocation (shaderProgram, "uni_projection");
        glUniformMatrix4fv (uni, 1, GL_FALSE, &projection);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member SetView (shaderProgram: int) (view: Matrix4x4) : unit =
        C """
        GLuint uni = glGetUniformLocation (shaderProgram, "uni_view");
        glUniformMatrix4fv (uni, 1, GL_FALSE, &view);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member SetModel (shaderProgram: int) (model: Matrix4x4) : unit =
        C """
        GLuint uni = glGetUniformLocation (shaderProgram, "uni_model");
        glUniformMatrix4fv (uni, 1, GL_FALSE, &model);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member SetCameraPosition (shaderProgram: int) (cameraPosition: Vector3) : unit =
        C """
        GLuint uni = glGetUniformLocation (shaderProgram, "uni_cameraPosition");
        glUniform3f (uni, cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
        """

    static member LoadShaders () =
        let mutable vertexFile = ([|0uy|]) |> Array.append (File.ReadAllBytes ("v.vertex"))
        let mutable fragmentFile = ([|0uy|]) |> Array.append (File.ReadAllBytes ("f.fragment"))

        R._LoadShaders vertexFile fragmentFile


(******************************************************************)

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
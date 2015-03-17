namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic
open System.Runtime.InteropServices

open Microsoft.FSharp.NativeInterop

open Ferop

#nowarn "9"

[<Struct>]
type RendererContext =
    val Window : nativeint
    val GLContext : nativeint

type VBO = VBO of id: int * size: int
type EBO = EBO of id: int * count: int

type ShaderUniformValue =
    | UniformNone
    | UniformSingle of single
    | UniformVector3 of Vector3
    | UniformMatrix4x4 of Matrix4x4

type ShaderUniform =
    {
        id: int
        name: string
    }

type Shader =
    {
        id: int
        uniforms: ShaderUniform list
    }

    static member Empty =
        {
            id = 0
            uniforms = []
        }

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
[<Sealed>]
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
    static member private _CreateElementBuffer (size: int, data: int []) : int =
        C """
        GLuint buffer;

        glGenBuffers (1, &buffer);
        glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, buffer);
        glBufferData (GL_ELEMENT_ARRAY_BUFFER, size, data, GL_DYNAMIC_DRAW);
        glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, 0);

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
    static member private _DrawElementBufferAsTriangles (count: int, ebo: int, vbo: int) : unit =
        C """
        glEnableVertexAttribArray (0);

        glBindBuffer (GL_ARRAY_BUFFER, vbo);
        glVertexAttribPointer (0, 3, GL_FLOAT, GL_FALSE, 0, 0);
        glBindBuffer (GL_ARRAY_BUFFER, 0);

        glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, ebo);
        glDrawElements (GL_TRIANGLES, count, GL_UNSIGNED_INT, 0);
        glBindBuffer (GL_ELEMENT_ARRAY_BUFFER, 0);
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

        return ProgramID;
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _GetAttributeId (shaderProgram: int, str: nativeptr<sbyte>) : int =
        C """
        return glGetAttribLocation (shaderProgram, str);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _UseProgram (programId: int) : unit =
        C """
        glUseProgram (programId);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _GetUniformId (program: int) (str: nativeptr<sbyte>) : int =
        C """
        return glGetUniformLocation (program, str);
        """

    static member private GetUniformId (shaderId: int) (uniformName: string) =
        let handle = GCHandle.Alloc (uniformName, GCHandleType.Pinned)
        let ptr = handle.AddrOfPinnedObject () |> NativePtr.ofNativeInt<sbyte>
        R._GetUniformId shaderId ptr

    static member UseShader (shader: Shader) =
        R._UseProgram (shader.id)

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
        SDL_GL_SetSwapInterval (0);

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
    //glDepthFunc(GL_LESS); 

    // Cull triangles which normal is not towards the camera
    //glEnable(GL_CULL_FACE);
    
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

    static member CreateEBO (data: int []) : EBO =
        let size = data.Length * sizeof<int>
        let id = R._CreateElementBuffer (size, data)
        EBO (id, data.Length)

    static member SetVBO (data: Vector3 [], (VBO (id, _))) =
       let size = data.Length * sizeof<Vector3>
       R._SetBuffer (size, data, id)
 
    static member DrawVBOAsTriangles (VBO (id, size)) : unit = 
        R._DrawBufferAsTriangles (size, id)

    // FIXME:
    static member DrawVBOAsTrianglesWithNBO (VBO (id, size)) nbo : unit = 
        R._DrawBufferAsTrianglesWithNBO (size, id, nbo)

    static member DrawEBOAsTriangles (EBO (eboId, count)) (VBO (vboId, _)) : unit = 
        R._DrawElementBufferAsTriangles (count, eboId, vboId)

    [<Import; MI (MIO.NoInlining)>]
    static member CreateVAO () : unit =
        C """
        GLuint vao;
        glGenVertexArrays (1, &vao);

        glBindVertexArray (vao);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _SetUniformVector4 (uniformId: int) (v: Vector4) : unit =
        C """
        glUniform4f (uniformId, v.X, v.Y, v.Z, v.W);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _SetUniformVector3 (uniformId: int) (v: Vector3) : unit =
        C """
        glUniform3f (uniformId, v.X, v.Y, v.Z);
        """

    [<Import; MI (MIO.NoInlining)>]
    static member private _SetUniformMatrix4x4 (uniformId: int) (m: Matrix4x4) : unit =
        C """
        glUniformMatrix4fv (uniformId, 1, GL_FALSE, &m);
        """

    static member SetShaderUniformVector4 (uniform: ShaderUniform) (v: Vector4) : unit =
        R._SetUniformVector4 uniform.id v

    static member SetShaderUniformVector3 (uniform: ShaderUniform) (v: Vector3) : unit =
        R._SetUniformVector3 uniform.id v
     
    static member SetShaderUniformMatrix4x4 (uniform: ShaderUniform) (m: Matrix4x4) : unit =
        R._SetUniformMatrix4x4 uniform.id m

    static member CreateShader vertexFileName fragmentFileName (uniforms: string list) =
        let mutable vertexFile = ([|0uy|]) |> Array.append (File.ReadAllBytes (vertexFileName))
        let mutable fragmentFile = ([|0uy|]) |> Array.append (File.ReadAllBytes (fragmentFileName))

        let shaderId = R._LoadShaders vertexFile fragmentFile

        let uniforms =
            uniforms
            |> List.map (fun x -> R.GetUniformId shaderId x)
            |> List.mapi (fun i uniformId -> 
                {
                    id = uniformId
                    name = uniforms.[i]
                }
            )

        {
            id = shaderId
            uniforms = uniforms
        }

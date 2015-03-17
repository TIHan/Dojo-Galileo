namespace Galileo

open System
open System.IO
open System.Numerics
open System.Diagnostics
open System.Collections.Generic

[<Struct>]
type RendererContext =
    val Window : nativeint
    val GLContext : nativeint

[<Sealed>]
type VBO

[<Sealed>]
type EBO

[<Sealed>]
type ShaderUniform

[<Sealed>]
type Shader =
    static member Empty : Shader

[<Sealed>]
type R =

    static member UseShader : shader: Shader -> unit

    static member CreateWindow : unit -> nativeint

    static member Init : window: nativeint -> RendererContext

    static member Exit : r: RendererContext -> int

    static member Clear : unit -> unit

    static member Draw : r: RendererContext -> unit

    static member CreateVBO : data: float32 [] -> VBO

    static member CreateVBO : data: Vector3 [] -> VBO

    static member CreateEBO : data: int [] -> EBO

    static member SetVBO : data: Vector3 [] * VBO -> unit

    static member DrawVBOAsTriangles : VBO -> unit

    static member DrawVBOAsTrianglesWithNBO : VBO -> int -> unit

    static member DrawEBOAsTriangles : EBO -> VBO -> unit

    static member CreateVAO : unit -> unit

    static member SetShaderUniformVector4 : uniform: ShaderUniform -> v: Vector4 -> unit

    static member SetShaderUniformVector3 : uniform: ShaderUniform -> v: Vector3 -> unit

    static member SetShaderUniformMatrix4x4 : uniform: ShaderUniform -> m: Matrix4x4 -> unit

    static member CreateShader : vertexFileName: string -> fragmentFileName: string -> string list -> Shader
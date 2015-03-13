namespace Galileo

[<Sealed>]
type Triangle

[<Sealed>]
type Octahedron

[<Sealed>]
type Sphere

[<System.Runtime.InteropServices.UnmanagedFunctionPointer (System.Runtime.InteropServices.CallingConvention.Cdecl)>]
type VboDelegate = delegate of unit -> unit

[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnDefaultRedTriangle : unit -> Async<Triangle>

    val spawnDefaultBlueTriangle : unit -> Async<Triangle>

    val spawnDefaultOctahedron : unit -> Async<Octahedron>

    val spawnDefaultSphere : unit -> Async<Sphere>

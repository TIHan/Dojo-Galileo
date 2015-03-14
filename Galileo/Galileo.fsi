namespace Galileo

[<Sealed>]
type Triangle

[<Sealed>]
type Octahedron

[<Sealed>]
type Sphere

[<RequireQualifiedAccess>]
type Entity =
    | Triangle of Triangle
    | Octahedron of Octahedron
    | Sphere of Sphere

[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnDefaultRedTriangle : unit -> Async<Triangle>

    val spawnDefaultBlueTriangle : unit -> Async<Triangle>

    val spawnDefaultOctahedron : unit -> Async<Octahedron>

    val spawnDefaultSphere : unit -> Async<Sphere>

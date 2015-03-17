namespace Galileo

[<Sealed>]
type Triangle

[<Sealed>]
type Octahedron

[<Sealed>]
type Sphere

[<RequireQualifiedAccess>]
module Galileo =

    val init : unit -> unit

    val spawnRedTriangle : unit -> unit

    val spawnBlueTriangle : unit -> unit

    val spawnOctahedron : unit -> unit
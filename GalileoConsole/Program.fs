open System
open System.Numerics
open Galileo
open Game

[<EntryPoint>]
let main argv = 
    Galileo.init ()

    // ------------------------------------------------------------------------- //
    (*
    1.
    *)

    Galileo.setUpdateCameraPosition (fun () ->
        Vector3 (0.f, 0.f, 2.f)
    )

    let earth = Galileo.spawnPlanet "earth.jpg"

    // ------------------------------------------------------------------------- //
    (*
    2.
    *)

    for i in - 10.f .. 1.f .. 10.f do
        Async.Sleep 1000 |> Async.RunSynchronously
        Galileo.setUpdateCameraPosition (fun () ->
            Vector3 (0.f, i, 2.f)
        )

    // ------------------------------------------------------------------------- //
    (*
    3.
    *)

    Galileo.setUpdateCameraPosition (fun () ->
        Vector3 (0.f, 0.f, 4.f)
    )

    earth.SetUpdate (fun time interval planet ->
        printfn "Time: %A" time
        printfn "Interval: %A" interval
        { planet with Translation = Matrix4x4.CreateTranslation (Vector3.Zero) }
    )

    // ------------------------------------------------------------------------- //

    earth.SetUpdate (fun time interval planet ->
        { planet with Translation = Matrix4x4.CreateTranslation (Vector3.UnitX) }
    )

    // ------------------------------------------------------------------------- //

    earth.SetUpdate (fun time interval planet ->
        { planet with Translation = Matrix4x4.CreateTranslation (Vector3.Zero) }
    )

    // Try out Vector3.UnitY and Vector3.UnitZ.

    // ------------------------------------------------------------------------- //
    (*
    4.
    *)

    let moon = Galileo.spawnPlanet "moon.jpg"

    earth.SetUpdate (fun time interval planet ->
        { planet with Translation = Matrix4x4.CreateTranslation (Vector3.UnitX * 3.f) }
    )

    // ------------------------------------------------------------------------- //

    moon.SetUpdate (fun time interval planet ->
        { planet with Scale = Matrix4x4.CreateScale (0.5f) }
    )

    // Try out changing scale of the moon.

    // ------------------------------------------------------------------------- //

    let PI = Math.PI |> single

    moon.SetUpdate (fun time interval planet ->
        let center = earth.LastKnownState.Translation.Translation
        let angle = PI
        { planet with Translation = Matrix4x4.CreateRotationY (angle, center) }
    )

    // ------------------------------------------------------------------------- //
    (*
    5.
    *)

    let acc = ref 0.f
    moon.SetUpdate (fun time interval planet ->
        let center = earth.LastKnownState.Translation.Translation
        let angle = !acc
        acc := !acc + 0.05f
        { planet with 
            Translation = Matrix4x4.CreateRotationY (angle, center)
            Scale = Matrix4x4.CreateScale (0.5f) }
    )


    // ------------------------------------------------------------------------- //
    (*
    Free form
    *)

    let satellite = Galileo.spawnPlanet "io.jpg"
    let satAngle = ref 0.f

    satellite.SetUpdate (fun time interval planet ->
        let isAround = moon.LastKnownState.Translation.Translation
        let position = Vector3.Add(isAround, Vector3(0.f,1.f,0.f))
        let a = !satAngle
        satAngle := !satAngle + 0.05f

        { planet with 
            Translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateRotationX (a, isAround)
            Scale = Matrix4x4.CreateScale (0.2f)
        }
    )

    Galileo.setUpdateLookAtPosition (fun () ->
        moon.LastKnownState.Translation.Translation
    )

    let rng = System.Random()

    for x in 1 .. 1000 do

        let rock = Galileo.spawnPlanet "io.jpg"
        let satAngle = ref (2. * 3.14 * rng.NextDouble() |> single)
        let size = rng.NextDouble () * 0.02 |> single
        let dist = 1. + 0.3 * rng.NextDouble () |> single
        let n1 = 0.001 * rng.NextDouble() - 0.0005 |> single
        let n2 = 0.001 * rng.NextDouble() - 0.0005 |> single

        rock.SetUpdate (fun time interval planet ->
            let isAround = moon.LastKnownState.Translation.Translation
            let position = Vector3.Add(isAround, Vector3(n1,dist,n2))
            let a = !satAngle
            satAngle := !satAngle + 0.05f

            { planet with 
                Translation = Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateRotationX (a, isAround)
                Scale = Matrix4x4.CreateScale (size)
            }
        )

    Console.ReadLine() |> ignore
    0

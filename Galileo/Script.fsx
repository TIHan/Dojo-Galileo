﻿#I @"../build/"

#r @"Galileo.dll"

open System
open Galileo

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
Galileo.init ()

// ------------------------------------------------------------------------- //

Galileo.spawnDefaultRedTriangle () |> Async.RunSynchronously
Galileo.spawnDefaultBlueTriangle () |> Async.RunSynchronously
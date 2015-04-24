﻿namespace Input

open Ferop

open System.Collections.Generic
open System.Runtime.InteropServices

type MouseButtonType =
    | Left = 1
    | Middle = 2
    | Right = 3
    | X1 = 4
    | X2 = 5

[<Struct>]
type MouseState =
    val X : int
    val Y : int

type InputEvent =
    | KeyPressed of char
    | KeyReleased of char
    | MouseButtonPressed of MouseButtonType
    | MouseButtonReleased of MouseButtonType
    | MouseWheelScrolled of x: int * y: int

type InputState = 
    { Events: InputEvent list 
      Mouse: MouseState }

[<Struct>]
type KeyboardEvent =
    val IsPressed : int
    val KeyCode : int

[<Struct>]
type MouseButtonEvent =
    val IsPressed : int
    val Clicks : int
    val Button : MouseButtonType
    val X : int
    val Y : int

[<Struct>]
type MouseWheelEvent =
    val X : int
    val Y : int


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
#include "SDL.h"
""")>]
module Input =

    let inputEvents = ResizeArray<InputEvent> ()

    let keyPressedSet = HashSet<char> ()

    let mouseButtonPressedSet = HashSet<MouseButtonType> ()

    let mutable state =
        {
            Events = []
            Mouse = Unchecked.defaultof<MouseState>
        }

    [<Export>]
    let dispatchKeyboardEvent (kbEvt: KeyboardEvent) : unit =
        inputEvents.Add (
            let key = char kbEvt.KeyCode
            if kbEvt.IsPressed = 0 then 
                keyPressedSet.Remove key |> ignore
                InputEvent.KeyReleased key
            else 
                keyPressedSet.Add key |> ignore
                InputEvent.KeyPressed key
        )

    [<Export>]
    let dispatchMouseButtonEvent (mbEvt: MouseButtonEvent) : unit =
        inputEvents.Add (
            let btn = mbEvt.Button
            if mbEvt.IsPressed = 0 then
                mouseButtonPressedSet.Remove btn |> ignore
                InputEvent.MouseButtonReleased btn
            else
                mouseButtonPressedSet.Add btn |> ignore
                InputEvent.MouseButtonPressed btn
        )

    [<Export>]
    let dispatchMouseWheelEvent (evt: MouseWheelEvent) : unit =
        inputEvents.Add (InputEvent.MouseWheelScrolled (evt.X, evt.Y))

    [<Import; MI (MIO.NoInlining)>]
    let getMouseState () : MouseState =
        C """
        int32_t x;
        int32_t y;
        Input_MouseState state;
        SDL_GetMouseState (&x, &y);
        state.X = x;
        state.Y = y;
        return state;
        """

    [<Export>]
    let setState () =
        let events = inputEvents |> List.ofSeq
        state <-
            {
                Events = events
                Mouse = getMouseState ()
            }

    let clearEvents () =
        inputEvents.Clear ()

    [<Import; MI (MIO.NoInlining)>]
    let poll () : unit =
        C """
        SDL_Event e;
        while (SDL_PollEvent (&e))
        {
            if (e.type == SDL_KEYDOWN)
            {
                SDL_KeyboardEvent* event = (SDL_KeyboardEvent*)&e;
                if (event->repeat != 0) continue;

                Input_KeyboardEvent evt;
                evt.IsPressed = 1;
                evt.KeyCode = event->keysym.sym;
                Input_dispatchKeyboardEvent (evt);
            }
            else if (e.type == SDL_KEYUP)
            {
                SDL_KeyboardEvent* event = (SDL_KeyboardEvent*)&e;
                if (event->repeat != 0) continue;

                Input_KeyboardEvent evt;
                evt.IsPressed = 0;
                evt.KeyCode = event->keysym.sym;

                Input_dispatchKeyboardEvent (evt);
            }
            else if (e.type == SDL_MOUSEBUTTONDOWN)
            {
                SDL_MouseButtonEvent* event = (SDL_MouseButtonEvent*)&e;
        
                Input_MouseButtonEvent evt;
                evt.IsPressed = 1;
                evt.Clicks = event->clicks;
                evt.Button = event->button;
                evt.X = event->x;
                evt.Y = event->y;

                Input_dispatchMouseButtonEvent (evt);
            }
            else if (e.type == SDL_MOUSEBUTTONUP)
            {
                SDL_MouseButtonEvent* event = (SDL_MouseButtonEvent*)&e;
        
                Input_MouseButtonEvent evt;
                evt.IsPressed = 0;
                evt.Clicks = event->clicks;
                evt.Button = event->button;
                evt.X = event->x;
                evt.Y = event->y;

                Input_dispatchMouseButtonEvent (evt);
            }
            else if (e.type == SDL_MOUSEWHEEL)
            {
                SDL_MouseWheelEvent* event = (SDL_MouseWheelEvent*)&e;
        
                Input_MouseWheelEvent evt;
                evt.X = event->x;
                evt.Y = event->y;

                Input_dispatchMouseWheelEvent (evt);
            }
        }
        
        Input_setState (); 
        """

    let getState () : InputState = state

    let isKeyPressed key = keyPressedSet.Contains key

    let isMouseButtonPressed btn = mouseButtonPressedSet.Contains btn
        
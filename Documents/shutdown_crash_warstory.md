<p align="center"><img src="https://github.com/chromelyapps/Chromely/blob/master/nugets/chromely.ico?raw=true" /></p>
<h1 align="center">Chromely.XamMac</h1>

# Goal

After trawling the internet for material on boostrapping CEF and Xamarin Mac, I came up with dead-ends on every turn. 

Some problems i encoutered were: 
* Nothing worked with the multi-threaded message loop (CEF v75+) which is recommended approach to get Chrome-like performance.
* Nothing gave developers a susccint understanding of what exactly *was required* to acheive the integration

Everything seemed possible, so i started down the rabbit hole and have come out the other side wanting to share my experience with the commuinity and make this task straight forward for those who come after me.

My goal:
* Integrating CEF with Xamarin MacoS in the lightest way possible
* Use C# across the stack
* Allow the application to customise the look and feel of the CEF window as if it was its own
* Provide a nuget package that can be added to any Xamarin MacS Application, that bootstraps it for plug & play operation with CEF.

## The CEF basics

* To run CEF, you need to integrate with its processing model on a platform it supports.
* This means, you need to implement a CefClient that understands how to implement the CEF protocal
* This client can be in any lanuage, but it must support the platform protocal (*swizzel events etc*)
* By implementing this client, at runtime CEF will provide your App with a OpenGlView window/view which is the browser and u can paint into various surfaces

## The Journey

My experience coming into this project was using [CEFSharp]() which uses a `C++ layer` to smooth out the CEF abstraction and make it *user friendly* with a *drop-and-drag canvas*. You dont need to worry about anything threading related, and they provide a great interface for quickly boostrapping your project. but.... they were never going to be [cross-platform](https://github.com/cefsharp/CefSharp/issues/1450).

### *Cross platform* CEF

Enter [CefGlue](https://gitlab.com/xiliumhq/chromiumembedded/cefglue). It ran on .NET Standard and i found [demo projects on integrating it with CEF ~v30-50 & MonoMac](https://www.magpcss.org/ceforum/viewtopic.php?f=14&t=14003). 
* *These **all** relied on a single threaded messages loop* to work correctly. This option is now *deprecated* CEF.
* They fell over on startup constantly. Something had changed between CEF versions, and i didnt know what..
* This was the closest i got with to a running [CEF app on XamMac](https://github.com/VitalElement/CefGlue.Core/tree/master/CefGlue.Avalonia)

A picture was starting to form in my head about what it will take to integrate CEF cross-platform... I had future goals of pushing the limits of code sharing, my ultimate goal being running the same code on *Windows/Mac/Andriod/Linux/MacOs*...?

<img src="graph1.png">

### CEF on *Xamarin Mac*

By this stage I knew i needed to strap myself in and hold on for the ride. I needed to somehow mash all the peices of the puzzle into something that worked. I was experienced with Xamarin/Andriod/iOS but *new to* Xamarin Mac... MacOS... and also new to the inner workings of CEF... and suddenly I was faced with the challenge of having to write a CefClient layer that talked the CEF protocal... My project deadline was nearing... Could i do it with XamMac? hmmmm!

## Saved by *Chromely*

So i started digging through all the resources i had found for a *working sample *of a C#/Mono CefClient & CEFGlue... but again, *nothing worked* with the latest CEF API where the protocal had evolved... and then i found [Chromely]() & its [chromely_mac.mm]().

* I didnt know `Obj-C`, but i knew this worked with the *latest* CEF version so it implemented the protocal correctly
* It took the approach of creating a native window with `obj-c`, and then handing off to `CefGlue` with the bare essentials boostrapped.


<img src="graph2.png">
The challenges Chromely had solved were:
* Working integration with *latest* CEF API
* Implementation of CefClient on MacOS to boostrap CEF's process model
* Renaming *"Chromenimium Embedded Framework"* to *"libcef"* to solve the dreaded *"libcef not found"*

### Why rename CEF?

This topic deserves a special callout. Over and over and over and over and over and.... over you will read doco on CEF / Xamarin / p/inokves and other topics of interop'ing. It will tell you `Place CEF next to your dll`. I saw many lost souls along the way aganonising over this fact. "*Why doesnt it detect CEF!? I followed the DOCS!*" they would say in desperate rants.

* Your not going crazy. Apparently this is common knowledge to those old veterans of mono but if CEF is your first dip in the water, then may get lost too

### I lied, *Chromely* was still crashing on startup

Chromely got the furthest of any platform but was built for `.Net Core`, not `Xam.Mac`/`mono`. Whats the differences? They both implement .Net standard 2.0+? Why did it crash on startup? Time to deep dive on CEF

* `Xam.Mac` runs on `mono` and is more mature then `.net core` which will morph into `.NET 5`. Fun fact [Eto]() maintains a fork of `Xam.Mac` to run on `.net core`.
* I used `sudo opensnoop` to detect what `xam.mac` was trying todo during compile and what paths it was probing
* I saw it was probing every dir BUT `frameworks` for a libary called `libcef` not `Chrominium Embedded Framework`
* CEF is hardcoded to probe for `assets` on certain paths. Some assets it *needs* next to `libcef` some needed inside of `frameworks`

The lessons i learnt were:

* CEF requires [assets](https://bitbucket.org/chromiumembedded/cef/issues/2737/macos-76-requires-multiple-helper-app) to be in certain folders.
* CEF hardcodes these expectations like *any other* Mac app, renaming or [DllImports] dont effect *that*.
* If you follow the required dir layout, placing CEF inside the `my.app/contents/frameworks` it will fail to detect!  
* `libcef` needs to be placed inside of `my.app/contents/monobundle` for Xam.mac to detect it *There are other folders also but its safest here as thats what most others do*
* Placing a copy of `libcef` in both `monobundle` & `frameworks` will result in a **STARTUP CRASH**! *why? no idea!* *It just does!*
* You need to keep all the *libcef/assets/resources* **ONLY** in `frameworks` and then another copy of everything PLUS libcef inside of `monobundle`
  * *I personally think this is a bug in Xamarin mac.* Xam.Mac should use `frameworks` as a probing path!?!

### but.... it still crashed on startup!

This time though, I was getting different error codes. These were related to the GPU [sub-processes]() that CEF attempts to launch after it has `Initailised` (...and *beleive me*, after a week of watching CEF crash on startup over and over, that first time you initalise its *pure bliss*)

Why did it crash?
* Xamarin mac creates a `MacOS.app` that is natively executed but *lies at a different execution path* then the calling assembly
* `CEFGlue` needs to be told about this so it sets CEF's `sub process path`. We set this `to the .app.`. By default, it will call into the executing process... Which in Xam mac world, is not what we want (mono etc in the between)

## The result

I had to make a few quick hacks *here and there *to get through this spike and to make this process streamlined for others. Ive collated all these `fixes` into this repository so [checkout the history]() to get more context.

I hope this helps anyone who may follow in attempting to make CEF do something others havnt before. Hopefully my methodology to debugging will help you also.

I plan on maintaining this fork going foward and/or integrating these changes back into Chromely and providing an nuget package to consume to faciliate  that fabled - plug and play `CEF` integration - in *any* cross platform .net app.

This [nuget package]() ***will*** provide

* All the hacks i just decribed to copy CEF and its assets to the *right dir*s, allows you to clean and rebuild with no delay in your flow.
* Distribute the [supported CEF runtime](http://opensource.spotify.com/cefbuilds/index.html) with the libary instead of having it as a seperate download. This has a MAJOR pain when ramping up on CEF, its hard to understand all the version numbers and what depends on what! Clean and rebuild and** download** and **unzip** *is not fun.*
* Supplies the *window handle* to your App so you can style it *using platform functions* if you wish
* Implements the Chromely NativeHost `.MessageBox` dialogs to allow cross-platform message boxes via Chromelys APIs (using Xamarin / .Net MUAIs APIs)

## But i have a problem!

This repo *successfully(!!)* boostraps CEF with Chromely. It will show you a browser window and you can use Chromelys API as usual. You can also use Xamarin to style the Window that Chromely mediates with CEF.... 

But *it will crash* when you try and *shut it down*. 

I am actively working on this and another issue: 

### Shutdown crash
* [CEF has a very particular API for shutdown](https://magpcss.org/ceforum/apidocs3/projects/(default)/CefLifeSpanHandler.html), like it does for init.
  * You must close the browser before exiting your App
  * You must wait for the browser to go through its shutdown process and cleanup its resources
    * This could take 100ms~ of its MessagePump do its thing
    * You need then, only after all browser handles/resources have been released by your app, call `CefRunetime.Shutdown()`
    * Then it is safe for you to Exit your app
  
My implementation so far does not comply and CEF crashes the App because it is holding a browser reference at shutdown

<img src="cef_shutdown_error.png">

Cloning and running via [VS Community]() + [Xamarin MacoS]() workload installed will demo the crashing issue as soon after you *click the cross to exit the app*.

## Release mode crash

CEF will fail to init when you switch compliation options to *Release mode*.
* I think this may be related to Notorisation the app

If anyone is able to help out diagnosing or debugging these issues, I would really appreciate the help. PRs welcome. 

## Parting words

I hope the information I have presented helps you launch your CEF project. Cross-platform rocks, and I am actively working on updating another project of mine, [Reactions](https://github.com/captainjono/rxns) to be *plug / play* & dependency free with .NET5!

# About me

In the world of tomorrow, the expectation is that your code will run on any device. I love open source. I love the possibilities of cross-platform. Ive been devv'ing since JDK 1.1 and moved to .NET @ 2.0. After building Java, Silverlight, Xamarin, Angular/React/TS/Progressive Apps/Services I found myself doing the same things over and over again.

I want to use that experience to help your code run on any device using the patterns that are portable to produce apps which are reliable, scalable, and maintainable over the *well after the hype cycle fades*
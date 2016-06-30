Mono Tools
============

Mono Tools enables local Mono debugging, Linux remote debugging, XBuild, pdb2mdb & MoMA using Visual Studio 2015.

Debugging
---
Download MonoDebuggerServer on the linux machine.
> wget https://github.com/simonegli8/MonoTools/releases/download/v2.0/MonoDebuggerServer.zip

Extract MonoDebuggerServer
> unzip -d MonoDebuggerServer MonoDebuggerServer.zip

Run MonoDebuggerServer on the linux machine.
> cd MonoDebuggerServer

> mono MonoDebuggerServer.exe

If you debug often on this machine, you might want to include this command in your session startup file.

<br/>

Install Mono Tools Visual Studio extension. You can find it in the Visual Studio Gallery.

Run Visual Studio 2015.

You can debug with a local Windows Mono installation, or if you want to debug on the remote Linux machine, go to your startup project properties and set the
*Use remote machine* field with either the IP or hostname of your Linux machine running the MonoDebugger server. If you specify an asterisk "*" as hostname,
you can interactively choose from detected servers on the local network.

Toolbar -> Mono -> Debug

Then the program will run and hit the breakpoint which you set on Visual Studio.

XBuild
---
Usually you won't build with XBuild, but with the normal Build and the pdb2mdb option, this way your build will run with MSBuild and support also custom build tasks, and will be more compatible with MSBuild.
It will perfectly run on Mono and you still will have the mdb files for debugging. You only use XBuild for compile time compatibility checking.

MoMA
---
With this menu entry you can create a MoMA report for your solution or project. Note that the current MoMA profile is still 2.8,
so you won't get an exact view, but you still get an idea what might not be compatible in your application.

Mono Tools
--
Mono debugging, xbuild, pdb2mdb & MoMA integration for Visual Studio.

![MonoTools](https://github.com/simonegli8/MonoTools/releases/download/2.0/Screenshot.png)

XBuild
---
With the xbuild entries you can run XBuild on your solution or project. The error reporting is not perfect, but should work. Usually you won't use xbuild, but normal build with the pdb2mdb option, because this is of course 100% compatible to MSBuild, for example when you use custom build tasks in your projects. You only build with XBuild to check for compatibility issues at compile time, but a normal build with pdb2mdb will run perfectly on Mono.

Debugging
---
For remote debugging, download and unzip the [server](https://github.com/simonegli8/MonoTools/releases/download/2.0/MonoDebuggerServer.zip), and run it with mono on a Linux machine. If you debug on Linux often, you might want to start the server with your session in the background, so you can always connect immediately.
To remote debug, you have to configure your project in the project properties and set the ip or hostname in the *Use remote server* field, the same as you would for Windows remote debugging. If you specify an asterisk "\*" or a question mark "?" as the remote server, you can interactively choose the server from servers on the local network. For web applications, you just specify the ip or hostname of the remote server in the application's url.
Apart from this, you should be able to configure all the other debugging options in the project properties and Mono Tools should understand them. If the ports used by the debugging server are occupied or not available on a server, you can set the ports used for debugging in the Visual Studio Mono Tools options. You'll have to specify three comma separated ports. You then must specify these ports also to the server program with the command line argument -ports=*port1*,*port2*,*port3*.

Pdb2mdb
---
With the Menu entry Add pdb2mdb to project you can add an import to your project file, that generates Mono debugging mdb files for your project after the build, so you can debug the project in Mono.

MoMA
---
Here you can generate a MoMA report of your solution or project. Unfortunately current MoMA profiles are from Mono 2.8, but at least you can get an idea what might not be compatible.
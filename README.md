windowsphonedriver
==================
This repository hosts the code for the Windows Phone driver. Its first iteration is intended
for testing the mobile web on Windows Phone 8. It should be able to support testing via an
emulator, or via actual devices, though actual device testing has yet to occur.

The driver is implemented as a command-line executable, which is compatible with the
[WebDriver JSON Wire Protocol](http://code.google.com/p/selenium/wiki/JsonWireProtocol). This
means that any language with a WebDriver language binding (including Java) can use that binding's
RemoteWebDriver implementation to speak directly to the command-line executable server application.

The command-line executable, WindowsPhoneDriverServer.exe, communicates to an application
that gets installed on the Windows Phone 8 device or emulator at runtime. This application
contains a WebBrowser control, through which websites are browsed and examined by the WebDriver
methods.

Requirements to Run the Windows Phone Driver
--------------------------------------------
* Windows 8 or higher
* Microsoft .NET Framework 4.5
* For use of the Windows Phone emulator, the [Windows Phone SDK](http://go.microsoft.com/fwlink/?LinkId=265772)
is required.

Using the Windows Phone Driver
------------------------------
Browser-specific driver classes for various langauges are planned, but until they are created,
the following steps should be followed to use the Windows Phone Driver:

1. Launch the WindowsPhoneDriverServer.exe, optionally indicating which port on which the server should listen on the commmand line (the default port is 7332).
2. Use the RemoteWebDriver class in your language binding to communicate with the server using http://localhost:<port>.

When the server is running, and you are executing WebDriver code against it, the WindowsPhoneDriverBrowser
mobile application should be installed onto the device or emulator automatically, and begin browsing to
the sites you specify. If you are using the emulator, you should see the emulator launch automatically as
well.

Requirements to Develop the Windows Phone Driver
------------------------------------------------
For development of the Windows Phone Driver, the following prerequisites are required, in addition to
those required above for running the driver:
* [Windows Phone SDK](http://go.microsoft.com/fwlink/?LinkId=265772)
* A clone of the [WebDriver project source tree](http://code.google.com/p/selenium/source/checkout)
* Java Development Kit (for compilation of the WebDriver project)
* Python (optional, for enhancing performance of compiling the WebDriver project)
* Visual Studio 2012 or higher (Visual Studio 2013 recommended)
* [StyleCop](https://stylecop.codeplex.com/), for enforcing consistent C# coding style

Developing the Windows Phone Driver
-----------------------------------
The Windows Phone Driver is written in C#, as it requires a Windows Phone 8 application. It is
not intended to run on the Mono framework.

Code correctness is maintained by a combination of static code analysis and integration tests.
Visual Studio 2012 includes static analysis tools that should be executed before committing
code. A clone of the WebDriver project's .NET integration tests has been added into this project's 
repository. They can be executed using the NUnit project found in the `lib` directory.

Consistent code style is maintained by use of StyleCop, which can be run as an add-in to Visual
Studio, or can be run stand-alone. The correct StyleCop settings for each project in the Visual
Studio solution are checked into the project, and should not need modification.

The Windows Phone Driver is dependent on the WebDriver project's JavaScript [Automation Atoms]
(http://code.google.com/p/selenium/wiki/AutomationAtoms). From time to time, these will need to
be refreshed. Refreshing these has been made pretty simple. From a Visual Studio 2012 Command
Prompt, in the root of the Windows Phone Driver project, you can execute the following command:

    support\updateatoms.cmd <full path to your clone of the WebDriver source code>
    
This will execute the build script in the WebDriver project required to build the atoms for the
Windows Phone Driver and copy the updated file into the correct location in the project tree.

Testing Your Changes to the Windows Phone Driver
------------------------------------------------
The Windows Phone Driver can be tested using the RemoteWebDriver tests in the WebDriver project's
.NET bindings integration tests. To accomplish this, you will need to perform the following steps:

1. Build the Windows Phone Driver project.
2. From the bin\\[Debug|Release] directory, start `WindowsPhoneDriverServer.exe` with the following command:
`bin\[Debug|Release]\WindowsPhoneDriverServer.exe /port:6000 /urlpath:wd/hub`
3. In your WebDriver project clone, make the following changes to the WebDriver.Remote.Tests.config file:
    * For the `add` element with the `key` attribute named `DriverName`, change the `value` attribute to `WindowsPhone`
    * For the `add` element with the `key` attribute named `AutoStartRemoteServer`, change the `value` attribute to `false`
    * For the `add` element with the `key` attribute named `HostName`, change the `value` attribute to the IP address of your local machine
4. Run the .NET RemoteWebDriver tests using NUnit or using the WebDriver project's 'go' build command with the target `//dotnet/test/remote:remote:run`
 



*********************************************************************************************
* 1) License information
*********************************************************************************************

    EasyHook - The reinvention of Windows API hooking
 
    Copyright (C) 2009 Christoph Husse & (C) 2013 EasyHook Development Team

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

    Please visit http://www.codeplex.com/easyhook for more information
    about the project and latest updates.

UDIS86:
    udis86 is Copyright (c) 2002-2012, Vivek Thampi <vivek.mt@gmail.com>
    See DirectShared\Disassembler\udis86-LICENSE.txt for license details.
    Minor modifications have been made for it to compile correctly in VC++.
    More information can be found at http://udis86.sourceforge.net/

BUG REPORTS:
    Reporting bugs is the only chance to get them fixed! Don't consider your
    report useless... The EasyHook team will fix any serious bug within a short time! 
    Bugs with lower priority will try be fixed in the next release...
    
EVENT LOGS:
    Please notice that you should always look into the application event log
    if something goes wrong, because EasyHook will often output extended error
    information there...

*********************************************************************************************
* 2) How to compile
*********************************************************************************************

EasyHook 2.7 includes a MSBuild script to build all versions of the binaries as well as
packaging for deployment, just run build.bat and then build-package.bat.
(requires the MSBuild Community Tasks http://msbuildtasks.tigris.org/)

After running the build-package.bat you will have all files you need for deployment with an
application located in the ".\Deploy\NetFX3.5" and ".\Deploy\NetFX4.0" directories. This is 
what you get with the "Binaries Only" package.

Please note that there are now different configurations for .NET 3.5 and .NET 4.0:

"netfx3.5-Debug\x64"
"netfx3.5-Debug\x86"
"netfx3.5-Release\x64"
"netfx3.5-Release\x86"
"netfx4-Debug\x64"
"netfx4-Debug\x86"
"netfx4-Release\x64"
"netfx4-Release\x86"

When compiling for distribution without using the build script you must build once for "x64" 
and then once for "x86" for the applicable framework version.

The project files include MSBuild tasks to copy and rename files as appropriate. The 
TargetFrameworkVersion is also overridden in each configuration - this is not visible 
within the VS2010 project settings GUI. To change the framework version or AfterBuild tasks
you must edit the project files directly:
 * in VS2010, right click project file: "Unload"
 * right click project file: "Edit ..csproj"

For testing purposes, as long as you keep all test applications in either the "x86" or "x64" 
directory, they will run properly. The "Deploy" directory is meant to contain all files 
necessary to ship an application based on EasyHook.


*********************************************************************************************
* 3) Prerequisites
*********************************************************************************************

Since the CRT is now statically compiled, you won't need the Visual Studio Redistributable 
package anymore. For testing purposes I would always recommend to use the DEBUG version...

.NET 3.5 or .NET 4.0 is required for Managed hooks.
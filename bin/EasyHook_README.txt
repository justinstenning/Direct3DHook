*********************************************************************************************
******************************************************* 1) License information
*********************************************************************************************

    EasyHook - The reinvention of Windows API hooking
 
    Copyright (C) 2009 Christoph Husse

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
    

BUG REPORTS:

    Reporting bugs is the only chance to get them fixed! Don't consider your
    report useless... I will fix any serious bug within a short time! Bugs with
    lower priority will always be fixed in the next release...
    
EVENT LOGS:
    Please notice that you should always look into the application event log
    if something goes wrong, because EasyHook will often output extended error
    information there...

*********************************************************************************************
***************************************************** 2) How to compile
*********************************************************************************************

Since EasyHook 2.5, compilation is dramatically simplified. Just compile once
for "x64" and once for "x86". The native EasyHook DLLs will automatically be
copied into their counterpart arch directory. 

The you will have to copy the following files into any desired "Deploy" directory:

"Debug\x64\EasyHook64.dll" -> "Deploy\EasyHook64.dll"
"Debug\x64\EasyHook64Svc.dll" -> "Deploy\EasyHook64Svc.dll"
"Debug\x86\EasyHook32.dll" -> "Deploy\EasyHook32.dll"
"Debug\x86\EasyHook32Svc.dll" -> "Deploy\EasyHook32Svc.dll"
"Debug\x64\EasyHook.dll" -> "Deploy\EasyHook.dll"
"Debug\x64\EasyHook.dll.xml" -> "Deploy\EasyHook.dll.xml"

(optional; only required for kernel hooking)
"Debug\x86\EasyHook32Drv.sys" -> "Deploy\EasyHook32Drv.sys"
"Debug\x64\EasyHook64Drv.sys" -> "Deploy\EasyHook64Drv.sys"


Of course this is not necessary for testing purposes. As long as you keep
all test applications in either the "x86" or "x64" directory, they will
run properly. The "Deploy" directory is meant to contain all files necessary
to ship an application based on EasyHook. This is what you get with the
"Binaries Only" package.


*********************************************************************************************
***************************************************** 3) Prerequisites
*********************************************************************************************

Since the CRT is now statically compiled, you won't need the Visual Studio Redistributable 
package anymore. For testing purposes I would always recommend to use the DEBUG version...


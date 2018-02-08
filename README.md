# A .Net [jet peer](http://jetbus.io/) written in C&#35;

[![Build status](https://ci.appveyor.com/api/projects/status/ftoeig69t9x9c4lh?svg=true)](https://ci.appveyor.com/project/gatzka/SharpJet)
[![Coverity](https://scan.coverity.com/projects/9877/badge.svg)](https://scan.coverity.com/projects/9877)
[![Coverage Status](https://coveralls.io/repos/github/gatzka/SharpJet/badge.svg?branch=master)](https://coveralls.io/github/gatzka/SharpJet?branch=master)

#### Some Powershell issues
This project uses a Powershell script executed as Pre-bulid event in order to set the AssemblyInformationalVersion to GUID.
Running Powershell scripts seems a little tricky, some information to make it easier:
By default powershell ExecutionPolicy is set to ``` Restriced```, then the Pre-build event throws an error message. 
On german system:
>"Die Datei "<Projectpath>\SharpJet\scripts\version.ps1" kann nicht geladen werden, da die Ausführung von Skripts auf diesem System deaktiviert ist.")

Solution: Lower the restrictions in your Powershell.
Unfortunately there are two Powershells on a x64-System:

1. 64-Bit Version (default) in <WinDir>\System32\WindowsPowerShell\v1.0\PowerShell.exe
2. 32-Bit-Version in <WinDir>\SysWOW64\WindowsPowerShell\v1.0

Yes, that's absolutely weird!!! 

Inside the relevant  Powershell just execute this command once:
```Set-ExecutionPolicy RemoteSigned```  or ```Set-ExecutionPolicy Unrestricted```
Notice that this enables executing powershell scripts for your system until you reset it to ```Set-ExecutionPolicy Unrestricted```. Read the current setting via ```Get-ExecutionPolicy```.

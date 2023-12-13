ExtIO plugin for SDR# 17xx versions (those with a frontend.xml).

Only works on SDR# 17xx x86 versions for now, exploring the possibility for an x64 version plugin as well...

-Compile solution with Visual Studio 2017 or above with C# .NET support.
-Repair references to SDRSharp.Common.dll and SDRSharp.Radio.dll. Use the DLLs from your SDR# 17xx installation.
-Install SSDRSharp.ExtIOSDR.dll in the same folder where you got your other SDRSharp.*.dll's.
-Add the line in registration.txt to your frontend.xml.
-Start SDR#
-Select ExtIOSDR as device
-Open ExtIOSDR config
-Select ExtIO DLL to use (install some if there are none in the combobox and hit Refresh)
-Configure the ExtIO DLL of your choice
-Start listening.

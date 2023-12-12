ExtIO plugin for SDR# 17xx versions (those with a frontend.xml)

-Compile solution with Visual Studio 2017 or above with C# .NET support.
-Repair references to SDRSharp.Common.dll and SDRSharp.Radio.dll. Use the DLLs from your SDR# 17xx installation.
-Add the line in registration.txt to your frontend.xml.
-Start SDR#
-Select ExtIOSDR as device
-Open ExtIOSDR config
-Select ExtIO DLL to use (install some if there are none in the combobox and hit Refresh)
-Configure the ExtIO DLL of your choice
-Start listening.
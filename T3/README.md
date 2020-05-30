Download and install 

  Microsoft.Common.CurrentVersion.targets(1177, 5): [MSB3644] The reference assemblies for .NETFramework,Version=v4.7.1 were not found. To resolve this, install the Developer Pack (SDK/Targeting Pack) for this framework version or retarget your application. You can download .NET Framework Developer Packs at https://aka.ms/msbuild/developerpacks
https://www.microsoft.com/en-us/download/confirmation.aspx?id=56119

[.net SDK Overview](https://dotnet.microsoft.com/download/visual-studio-sdks)



## for Rider...

1. In toolbar click on T3-Dropdown and change the working directory from...
```C:/Users/YOUR_USER_NAME/coding/t3/T3/bin/Debug``` to 
```C:/Users/YOUR_USER_NAME/coding/t3```





Pitfalls

Bass.dll missing

```
System.TypeInitializationException: The type initializer for 'T3.Gui.T3Ui' threw an exception. ---> System.DllNotFoundException: Unable to load DLL 'bass': The specified module could not be found. (Exception from HRESULT: 0x8007007E)
  at at ManagedBass.Bass.Init(Int32 Device, Int32 Frequency, DeviceInitFlags Flags, IntPtr Win, IntPtr ClsID)
```


```
SharpDX.SharpDXException: HRESULT: [0x887A002D], Module: [SharpDX.DXGI], ApiCode: [DXGI_ERROR_SDK_COMPONENT_MISSING/SdkComponentMissing], 
Message: The application requested an operation that depends on an SDK component that is missing or mismatched.

  at at SharpDX.Result.CheckError()
  at at SharpDX.Direct3D11.Device.CreateDevice(Adapter adapter, DriverType driverType, DeviceCreationFlags flags, FeatureLevel[] featureLevels)
```
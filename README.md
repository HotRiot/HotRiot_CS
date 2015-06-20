We provide an API that makes writing C# applications with HotRiot a snap. The API source code is maintained on GitHub, you can use the link below to download it directly. 

https://github.com/HotRiot/HotRiot_CS/archive/master.zip

We keep it simple, there is just one file to download, HotRiot_CS.cs. Integrating this source file into your project should be a simple matter of including HotRiot_CS.cs directly into your project. You will also need to include the System.Web assembly in your project. HotRiot is dependent on Json.NET, which is freely available through NuGet.

Json.NET is a high performance JSON processor for .NET. It include assemblies that support Windows, Windows Phone, Mono, MonoTouch, MonoDroid, and Silverlight. It can be installed directly from within the Visual Studio IDE using NuGet. Select Project->Manage Nuget Packages, then search for Json.NET. It can also be downloaded from the following location and installed manually:

https://json.codeplex.com/releases/view/117958

The HotRiot API includes platform specific code for creating thumbnail images. This code targets the following four platforms:

Windows
Android
Mac
iOS

At the top of the HotRiot_CS.cs file are three conditional compilation directives, they are:

&#35;define  WINDOWS_OR_MAC_BUILD
&#35;define  ANDROID_BUILD
&#35;define  IOS_BUILD

Uncomment the appropriate directive depending on the platform for which you are targeting.

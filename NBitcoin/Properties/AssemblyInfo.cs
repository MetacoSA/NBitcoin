﻿using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("NBitcoin")]
[assembly: AssemblyDescription("Implementation of bitcoin protocol")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("AO-IS")]
[assembly: AssemblyProduct("NBitcoin")]
[assembly: AssemblyCopyright("Copyright © AO-IS 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
#if !PORTABLE
[assembly:InternalsVisibleTo("NBitcoin.Tests")]
#else
[assembly: InternalsVisibleTo("NBitcoin.Portable.Tests")]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:

[assembly: AssemblyVersion("3.0.0.25")]
[assembly: AssemblyFileVersion("3.0.0.25")]
[assembly: AssemblyInformationalVersion("3.0.0.25")]

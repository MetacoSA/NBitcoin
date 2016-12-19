using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

[assembly: CLSCompliant(false)]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//[assembly: AssemblyTitle("HashLib")]
//[assembly: AssemblyDescription("")]
//[assembly: AssemblyConfiguration("")]
//[assembly: AssemblyCompany("")]
//[assembly: AssemblyProduct("HashLib")]
//[assembly: AssemblyCopyright("")]
//[assembly: AssemblyTrademark("")]
//[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
//[assembly: ComVisible(false)]

#if !NETCORE
// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9419e00e-d63b-4d92-954a-937ac7ec005d")]

#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
//[assembly: AssemblyVersion("2.1.0.0")]
//[assembly: AssemblyFileVersion("2.1.0.0")]

[assembly: InternalsVisibleTo("HashLibTest")]


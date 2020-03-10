# NBitcoin.Secp256k1

## Why this project?

This library is a C# implementation of the [secp256k1](https://github.com/bitcoin-core/secp256k1) project.
NBitcoin does not depends on this package, because this library is statically built into NBitcoin to prevent any conflicts.

This project IS NOT C# PInvoke bindings to the native secp256k1 library.

This project has been created for NBitcoin to replace cryptographic part previously implemented on top of BouncyCastle.
We decided to move out from BouncyCastle as Bitcoin's cryptographers are generally building their work on top of secp256k1. This will us to easily reimplement their algorithms in C#.

DO NOT USE THIS PROJECT IF:
* You don't know what it is about (this mean you don't need it)
* Only need to use standard Bitcoin feature (use NBitcoin directly instead of this library)

## Why are you not using C# bindings to the C library?

We decided to not use native bindings to the C library to keep NBitcoin fully managed.
As NBitcoin is used by multiple projects, moving out of a fully managed environment is not without pitfalls.

If we chose to use C# bindings then as soon as our users update NBitcoin's version, they would start running an unauditable dynamic C library.
The solution to this, is to have a deterministic build of Secp256k1 C codebase for each platforms NBitcoin was previously supporting.
This is a huge amount of work, and nobody in the community has been willing to do it, even for a single platform as popular as linux x64.

Another solution to this problem is to delegate the build to the user of this library.
But this is not a proper solution for a library, as we are targetting the C# community who very often does not have any skill building C applications by themselves.
This would be a huge adoption problem for NBitcoin.

Finally, some environment may not even support pInvoke bindings such as WASM or Unity.

In the future NBitcoin will probably try to use native bindings if available, else fallback on the managed implementation.

## How to use ?

In .NET Core:
```bash
dotnet add package NBitcoin.Secp256k1
```
If using legacy .NET Framework in Visual Studio
```bash
Install-Package NBitcoin.Secp256k1
```
You can also just use the `Manage NuGet Package` window on your project in Visual Studio.

Go on the [NuGet website](https://www.nuget.org/packages/NBitcoin.Secp256k1/) for more information.

## Where is the code?

[In a folder under NBitcoin's project](../NBitcoin/Secp256k1).

## Where is the documentation?

If you have to ask the question, please do not use this library.
This library is implementing the low level mathematical abstractions of [secp256k1](https://github.com/bitcoin-core/secp256k1).
Unless you know exactly what it means, you should not use this library.

## Contributions

We only accept straight port from [secp256k1](github.com/bitcoin-core/secp256k1) or [secp256k1-zkp](https://github.com/ElementsProject/secp256k1-zkp).
The reason is that NBitcoin's contributors are not cryptographers and can only review if the port is faithful to the original C code, but we are unable to review the underlying mathematical soundness.

Any contributions need to also port the unit tests written from C to C# in the `NBitcoin.Tests` project.

## License

MIT

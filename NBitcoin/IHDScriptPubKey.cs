using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	/// <summary>
	/// A IHDScriptPubKey represent an object which represent a tree of scriptPubKeys
	/// </summary>
	public interface IHDScriptPubKey
	{
		IHDScriptPubKey Derive(KeyPath keyPath);
		bool CanDeriveHardenedPath();
		Script ScriptPubKey { get; }
	}
}

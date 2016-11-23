using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace HashLib
{
    public static class Hashes
    {
        public readonly static ReadOnlyCollection<Type> All;
        public readonly static ReadOnlyCollection<Type> AllUnique;
        public readonly static ReadOnlyCollection<Type> Hash32;
        public readonly static ReadOnlyCollection<Type> Hash64;
        public readonly static ReadOnlyCollection<Type> Hash128;
        public readonly static ReadOnlyCollection<Type> CryptoAll;
        public readonly static ReadOnlyCollection<Type> CryptoNotBuildIn;
        public readonly static ReadOnlyCollection<Type> CryptoBuildIn;
        public readonly static ReadOnlyCollection<Type> HasHMACBuildIn;

        public readonly static ReadOnlyCollection<Type> NonBlock;
        public readonly static ReadOnlyCollection<Type> FastComputes;
        public readonly static ReadOnlyCollection<Type> Checksums;
        public readonly static ReadOnlyCollection<Type> WithKey;

        static Hashes()
        {
#if !NETCORE
			All = (from hf in Assembly.GetAssembly(typeof(IHash)).GetTypes()
                   where hf.IsClass
                   where !hf.IsAbstract
                   where hf != typeof(HMACNotBuildInAdapter)
                   where hf != typeof(HashCryptoBuildIn)
                   where hf != typeof(HMACBuildInAdapter)
                   where hf.IsImplementInterface(typeof(IHash))
                   where !hf.IsNested
                   select hf).ToList().AsReadOnly();

            All = (from hf in All
                   orderby hf.Name
                   select hf).ToList().AsReadOnly();

            var x2 = new Type[] 
            {

                typeof(HashLib.Crypto.BuildIn.SHA1Cng), 
                typeof(HashLib.Crypto.BuildIn.SHA1Managed), 
                typeof(HashLib.Crypto.BuildIn.SHA256Cng), 
                typeof(HashLib.Crypto.BuildIn.SHA256Managed), 
                typeof(HashLib.Crypto.BuildIn.SHA384Cng), 
                typeof(HashLib.Crypto.BuildIn.SHA384Managed), 
                typeof(HashLib.Crypto.BuildIn.SHA512Cng), 
                typeof(HashLib.Crypto.BuildIn.SHA512Managed), 
			typeof(HashLib.Crypto.MD5),
                typeof(HashLib.Crypto.RIPEMD160),
                typeof(HashLib.Crypto.SHA1),
                typeof(HashLib.Crypto.SHA256),
                typeof(HashLib.Crypto.SHA384),
                typeof(HashLib.Crypto.SHA512),
            };

            AllUnique = (from hf in All
                         where !(hf.IsDerivedFrom(typeof(HashLib.Hash32.DotNet)))
                         where !x2.Contains(hf)
                         where !hf.IsNested
                         select hf).ToList().AsReadOnly();

            Hash32 = (from hf in All
                      where hf.IsImplementInterface(typeof(IHash32))
                      where !hf.IsImplementInterface(typeof(IChecksum))
                      select hf).ToList().AsReadOnly();

            Hash64 = (from hf in All
                      where hf.IsImplementInterface(typeof(IHash64))
                      where !hf.IsImplementInterface(typeof(IChecksum))
                      select hf).ToList().AsReadOnly();

            Hash128 = (from hf in All
                       where hf.IsImplementInterface(typeof(IHash128))
                       where !hf.IsImplementInterface(typeof(IChecksum))
                       select hf).ToList().AsReadOnly();

            Checksums = (from hf in All
                         where hf.IsImplementInterface(typeof(IChecksum))
                         select hf).ToList().AsReadOnly();

            FastComputes = (from hf in All
                            where hf.IsImplementInterface(typeof(IFastHash32))
                            select hf).ToList().AsReadOnly();

            NonBlock = (from hf in All
                        where hf.IsImplementInterface(typeof(INonBlockHash))
                        select hf).ToList().AsReadOnly();

            WithKey = (from hf in All
                       where hf.IsImplementInterface(typeof(IWithKey))
                       select hf).ToList().AsReadOnly();

            CryptoAll = (from hf in All
                         where hf.IsImplementInterface(typeof(ICrypto))
                         select hf).ToList().AsReadOnly();

            CryptoNotBuildIn = (from hf in CryptoAll
                                where hf.IsImplementInterface(typeof(ICryptoNotBuildIn))
                                select hf).ToList().AsReadOnly();

            CryptoBuildIn = (from hf in CryptoAll
                             where hf.IsImplementInterface(typeof(ICryptoBuildIn))
                             select hf).ToList().AsReadOnly();

            HasHMACBuildIn = (from hf in CryptoBuildIn
                              where hf.IsImplementInterface(typeof(IHasHMACBuildIn))
                              select hf).ToList().AsReadOnly();
#endif

		}
	}
}

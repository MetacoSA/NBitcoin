using System;
using System.Collections;

using NBitcoin.BouncyCastle.Asn1.Nist;
using NBitcoin.BouncyCastle.Asn1.Sec;
using NBitcoin.BouncyCastle.Asn1.TeleTrust;
using NBitcoin.BouncyCastle.Utilities;
using NBitcoin.BouncyCastle.Utilities.Collections;

namespace NBitcoin.BouncyCastle.Asn1.X9
{
    /**
     * A general class that reads all X9.62 style EC curve tables.
     */
    public class ECNamedCurveTable
    {
        /**
         * return a X9ECParameters object representing the passed in named
         * curve. The routine returns null if the curve is not present.
         *
         * @param name the name of the curve requested
         * @return an X9ECParameters object or null if the curve is not available.
         */
        public static X9ECParameters GetByName(string name)
        {
            X9ECParameters ecP = X962NamedCurves.GetByName(name);

            if (ecP == null)
            {
                ecP = SecNamedCurves.GetByName(name);
            }

            if (ecP == null)
            {
                ecP = TeleTrusTNamedCurves.GetByName(name);
            }

            if (ecP == null)
            {
                ecP = NistNamedCurves.GetByName(name);
            }

            return ecP;
        }

        /**
         * return the object identifier signified by the passed in name. Null
         * if there is no object identifier associated with name.
         *
         * @return the object identifier associated with name, if present.
         */
        public static DerObjectIdentifier GetOid(string name)
        {
            DerObjectIdentifier oid = X962NamedCurves.GetOid(name);

            if (oid == null)
            {
                oid = SecNamedCurves.GetOid(name);
            }

            if (oid == null)
            {
                oid = TeleTrusTNamedCurves.GetOid(name);
            }

            if (oid == null)
            {
                oid = NistNamedCurves.GetOid(name);
            }

            return oid;
        }

        /**
         * return a X9ECParameters object representing the passed in named
         * curve.
         *
         * @param oid the object id of the curve requested
         * @return an X9ECParameters object or null if the curve is not available.
         */
        public static X9ECParameters GetByOid(DerObjectIdentifier oid)
        {
            X9ECParameters ecP = X962NamedCurves.GetByOid(oid);

            if (ecP == null)
            {
                ecP = SecNamedCurves.GetByOid(oid);
            }

            if (ecP == null)
            {
                ecP = TeleTrusTNamedCurves.GetByOid(oid);
            }

            // NOTE: All the NIST curves are currently from SEC, so no point in redundant OID lookup

            return ecP;
        }

        /**
         * return an enumeration of the names of the available curves.
         *
         * @return an enumeration of the names of the available curves.
         */
        public static IEnumerable Names
        {
            get
            {
                IList v = Platform.CreateArrayList();
                CollectionUtilities.AddRange(v, X962NamedCurves.Names);
                CollectionUtilities.AddRange(v, SecNamedCurves.Names);
                CollectionUtilities.AddRange(v, NistNamedCurves.Names);
                CollectionUtilities.AddRange(v, TeleTrusTNamedCurves.Names);
                return v;
            }
        }
    }
}

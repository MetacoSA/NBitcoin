using System;

using NBitcoin.BouncyCastle.Math.EC.Abc;

namespace NBitcoin.BouncyCastle.Math.EC.Multiplier
{
    /**
    * Class implementing the WTNAF (Window
    * <code>&#964;</code>-adic Non-Adjacent Form) algorithm.
    */
    public class WTauNafMultiplier
        : AbstractECMultiplier
    {
        // TODO Create WTauNafUtilities class and move various functionality into it
        internal static readonly string PRECOMP_NAME = "bc_wtnaf";

        /**
        * Multiplies a {@link NBitcoin.BouncyCastle.math.ec.F2mPoint F2mPoint}
        * by <code>k</code> using the reduced <code>&#964;</code>-adic NAF (RTNAF)
        * method.
        * @param p The F2mPoint to multiply.
        * @param k The integer by which to multiply <code>k</code>.
        * @return <code>p</code> multiplied by <code>k</code>.
        */
        protected override ECPoint MultiplyPositive(ECPoint point, BigInteger k)
        {
            if (!(point is F2mPoint))
                throw new ArgumentException("Only F2mPoint can be used in WTauNafMultiplier");

            F2mPoint p = (F2mPoint)point;
            F2mCurve curve = (F2mCurve)p.Curve;
            int m = curve.M;
            sbyte a = (sbyte) curve.A.ToBigInteger().IntValue;
            sbyte mu = curve.GetMu();
            BigInteger[] s = curve.GetSi();

            ZTauElement rho = Tnaf.PartModReduction(k, m, a, s, mu, (sbyte)10);

            return MultiplyWTnaf(p, rho, curve.GetPreCompInfo(p, PRECOMP_NAME), a, mu);
        }

        /**
        * Multiplies a {@link NBitcoin.BouncyCastle.math.ec.F2mPoint F2mPoint}
        * by an element <code>&#955;</code> of <code><b>Z</b>[&#964;]</code> using
        * the <code>&#964;</code>-adic NAF (TNAF) method.
        * @param p The F2mPoint to multiply.
        * @param lambda The element <code>&#955;</code> of
        * <code><b>Z</b>[&#964;]</code> of which to compute the
        * <code>[&#964;]</code>-adic NAF.
        * @return <code>p</code> multiplied by <code>&#955;</code>.
        */
        private F2mPoint MultiplyWTnaf(F2mPoint p, ZTauElement lambda,
            PreCompInfo preCompInfo, sbyte a, sbyte mu)
        {
            ZTauElement[] alpha = (a == 0) ? Tnaf.Alpha0 : Tnaf.Alpha1;

            BigInteger tw = Tnaf.GetTw(mu, Tnaf.Width);

            sbyte[]u = Tnaf.TauAdicWNaf(mu, lambda, Tnaf.Width,
                BigInteger.ValueOf(Tnaf.Pow2Width), tw, alpha);

            return MultiplyFromWTnaf(p, u, preCompInfo);
        }
        
        /**
        * Multiplies a {@link NBitcoin.BouncyCastle.math.ec.F2mPoint F2mPoint}
        * by an element <code>&#955;</code> of <code><b>Z</b>[&#964;]</code>
        * using the window <code>&#964;</code>-adic NAF (TNAF) method, given the
        * WTNAF of <code>&#955;</code>.
        * @param p The F2mPoint to multiply.
        * @param u The the WTNAF of <code>&#955;</code>..
        * @return <code>&#955; * p</code>
        */
        private static F2mPoint MultiplyFromWTnaf(F2mPoint p, sbyte[] u, PreCompInfo preCompInfo)
        {
            F2mCurve curve = (F2mCurve)p.Curve;
            sbyte a = (sbyte)curve.A.ToBigInteger().IntValue;

            F2mPoint[] pu;
            if ((preCompInfo == null) || !(preCompInfo is WTauNafPreCompInfo))
            {
                pu = Tnaf.GetPreComp(p, a);

                WTauNafPreCompInfo pre = new WTauNafPreCompInfo();
                pre.PreComp = pu;
                curve.SetPreCompInfo(p, PRECOMP_NAME, pre);
            }
            else
            {
                pu = ((WTauNafPreCompInfo)preCompInfo).PreComp;
            }

            // q = infinity
            F2mPoint q = (F2mPoint)curve.Infinity;
            for (int i = u.Length - 1; i >= 0; i--)
            {
                q = Tnaf.Tau(q);
                sbyte ui = u[i];
                if (ui != 0)
                {
                    if (ui > 0)
                    {
                        q = q.AddSimple(pu[ui]);
                    }
                    else
                    {
                        // u[i] < 0
                        q = q.SubtractSimple(pu[-ui]);
                    }
                }
            }

            return q;
        }
    }
}

namespace Org.BouncyCastle.Math.EC.Multiplier
{
    /**
     * Class holding precomputation data for the WTNAF (Window
     * <code>&#964;</code>-adic Non-Adjacent Form) algorithm.
     */
    public class WTauNafPreCompInfo
        : PreCompInfo
    {
        /**
         * Array holding the precomputed <code>F2mPoint</code>s used for the
         * WTNAF multiplication in <code>
         * {@link org.bouncycastle.math.ec.multiplier.WTauNafMultiplier.multiply()
         * WTauNafMultiplier.multiply()}</code>.
         */
        protected F2mPoint[] m_preComp;

        public virtual F2mPoint[] PreComp
        {
            get { return m_preComp; }
            set { this.m_preComp = value; }
        }
    }
}

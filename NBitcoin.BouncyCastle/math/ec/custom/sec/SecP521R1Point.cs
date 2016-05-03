﻿using System;

using NBitcoin.BouncyCastle.Math.Raw;

namespace NBitcoin.BouncyCastle.Math.EC.Custom.Sec
{
    internal class SecP521R1Point
        : AbstractFpPoint
    {
        /**
         * Create a point which encodes with point compression.
         * 
         * @param curve
         *            the curve to use
         * @param x
         *            affine x co-ordinate
         * @param y
         *            affine y co-ordinate
         * 
         * @deprecated Use ECCurve.createPoint to construct points
         */
        public SecP521R1Point(ECCurve curve, ECFieldElement x, ECFieldElement y)
            : this(curve, x, y, false)
        {
        }

        /**
         * Create a point that encodes with or without point compresion.
         * 
         * @param curve
         *            the curve to use
         * @param x
         *            affine x co-ordinate
         * @param y
         *            affine y co-ordinate
         * @param withCompression
         *            if true encode with point compression
         * 
         * @deprecated per-point compression property will be removed, refer
         *             {@link #getEncoded(bool)}
         */
        public SecP521R1Point(ECCurve curve, ECFieldElement x, ECFieldElement y, bool withCompression)
            : base(curve, x, y, withCompression)
        {
            if ((x == null) != (y == null))
                throw new ArgumentException("Exactly one of the field elements is null");
        }

        internal SecP521R1Point(ECCurve curve, ECFieldElement x, ECFieldElement y, ECFieldElement[] zs, bool withCompression)
            : base(curve, x, y, zs, withCompression)
        {
        }

        protected override ECPoint Detach()
        {
            return new SecP521R1Point(null, AffineXCoord, AffineYCoord);
        }

        public override ECPoint Add(ECPoint b)
        {
            if (this.IsInfinity)
                return b;
            if (b.IsInfinity)
                return this;
            if (this == b)
                return Twice();

            ECCurve curve = this.Curve;

            SecP521R1FieldElement X1 = (SecP521R1FieldElement)this.RawXCoord, Y1 = (SecP521R1FieldElement)this.RawYCoord;
            SecP521R1FieldElement X2 = (SecP521R1FieldElement)b.RawXCoord, Y2 = (SecP521R1FieldElement)b.RawYCoord;

            SecP521R1FieldElement Z1 = (SecP521R1FieldElement)this.RawZCoords[0];
            SecP521R1FieldElement Z2 = (SecP521R1FieldElement)b.RawZCoords[0];

            uint[] t1 = Nat.Create(17);
            uint[] t2 = Nat.Create(17);
            uint[] t3 = Nat.Create(17);
            uint[] t4 = Nat.Create(17);

            bool Z1IsOne = Z1.IsOne;
            uint[] U2, S2;
            if (Z1IsOne)
            {
                U2 = X2.x;
                S2 = Y2.x;
            }
            else
            {
                S2 = t3;
                SecP521R1Field.Square(Z1.x, S2);

                U2 = t2;
                SecP521R1Field.Multiply(S2, X2.x, U2);

                SecP521R1Field.Multiply(S2, Z1.x, S2);
                SecP521R1Field.Multiply(S2, Y2.x, S2);
            }

            bool Z2IsOne = Z2.IsOne;
            uint[] U1, S1;
            if (Z2IsOne)
            {
                U1 = X1.x;
                S1 = Y1.x;
            }
            else
            {
                S1 = t4;
                SecP521R1Field.Square(Z2.x, S1);

                U1 = t1;
                SecP521R1Field.Multiply(S1, X1.x, U1);

                SecP521R1Field.Multiply(S1, Z2.x, S1);
                SecP521R1Field.Multiply(S1, Y1.x, S1);
            }

            uint[] H = Nat.Create(17);
            SecP521R1Field.Subtract(U1, U2, H);

            uint[] R = t2;
            SecP521R1Field.Subtract(S1, S2, R);

            // Check if b == this or b == -this
            if (Nat.IsZero(17, H))
            {
                if (Nat.IsZero(17, R))
                {
                    // this == b, i.e. this must be doubled
                    return this.Twice();
                }

                // this == -b, i.e. the result is the point at infinity
                return curve.Infinity;
            }

            uint[] HSquared = t3;
            SecP521R1Field.Square(H, HSquared);

            uint[] G = Nat.Create(17);
            SecP521R1Field.Multiply(HSquared, H, G);

            uint[] V = t3;
            SecP521R1Field.Multiply(HSquared, U1, V);

            SecP521R1Field.Multiply(S1, G, t1);

            SecP521R1FieldElement X3 = new SecP521R1FieldElement(t4);
            SecP521R1Field.Square(R, X3.x);
            SecP521R1Field.Add(X3.x, G, X3.x);
            SecP521R1Field.Subtract(X3.x, V, X3.x);
            SecP521R1Field.Subtract(X3.x, V, X3.x);

            SecP521R1FieldElement Y3 = new SecP521R1FieldElement(G);
            SecP521R1Field.Subtract(V, X3.x, Y3.x);
            SecP521R1Field.Multiply(Y3.x, R, t2);
            SecP521R1Field.Subtract(t2, t1, Y3.x);

            SecP521R1FieldElement Z3 = new SecP521R1FieldElement(H);
            if (!Z1IsOne)
            {
                SecP521R1Field.Multiply(Z3.x, Z1.x, Z3.x);
            }
            if (!Z2IsOne)
            {
                SecP521R1Field.Multiply(Z3.x, Z2.x, Z3.x);
            }

            ECFieldElement[] zs = new ECFieldElement[] { Z3 };

            return new SecP521R1Point(curve, X3, Y3, zs, IsCompressed);
        }

        public override ECPoint Twice()
        {
            if (this.IsInfinity)
                return this;

            ECCurve curve = this.Curve;

            SecP521R1FieldElement Y1 = (SecP521R1FieldElement)this.RawYCoord;
            if (Y1.IsZero)
                return curve.Infinity;

            SecP521R1FieldElement X1 = (SecP521R1FieldElement)this.RawXCoord, Z1 = (SecP521R1FieldElement)this.RawZCoords[0];

            uint[] t1 = Nat.Create(17);
            uint[] t2 = Nat.Create(17);

            uint[] Y1Squared = Nat.Create(17);
            SecP521R1Field.Square(Y1.x, Y1Squared);

            uint[] T = Nat.Create(17);
            SecP521R1Field.Square(Y1Squared, T);

            bool Z1IsOne = Z1.IsOne;

            uint[] Z1Squared = Z1.x;
            if (!Z1IsOne)
            {
                Z1Squared = t2;
                SecP521R1Field.Square(Z1.x, Z1Squared);
            }

            SecP521R1Field.Subtract(X1.x, Z1Squared, t1);

            uint[] M = t2;
            SecP521R1Field.Add(X1.x, Z1Squared, M);
            SecP521R1Field.Multiply(M, t1, M);
            Nat.AddBothTo(17, M, M, M);
            SecP521R1Field.Reduce23(M);

            uint[] S = Y1Squared;
            SecP521R1Field.Multiply(Y1Squared, X1.x, S);
            Nat.ShiftUpBits(17, S, 2, 0);
            SecP521R1Field.Reduce23(S);

            Nat.ShiftUpBits(17, T, 3, 0, t1);
            SecP521R1Field.Reduce23(t1);

            SecP521R1FieldElement X3 = new SecP521R1FieldElement(T);
            SecP521R1Field.Square(M, X3.x);
            SecP521R1Field.Subtract(X3.x, S, X3.x);
            SecP521R1Field.Subtract(X3.x, S, X3.x);

            SecP521R1FieldElement Y3 = new SecP521R1FieldElement(S);
            SecP521R1Field.Subtract(S, X3.x, Y3.x);
            SecP521R1Field.Multiply(Y3.x, M, Y3.x);
            SecP521R1Field.Subtract(Y3.x, t1, Y3.x);

            SecP521R1FieldElement Z3 = new SecP521R1FieldElement(M);
            SecP521R1Field.Twice(Y1.x, Z3.x);
            if (!Z1IsOne)
            {
                SecP521R1Field.Multiply(Z3.x, Z1.x, Z3.x);
            }

            return new SecP521R1Point(curve, X3, Y3, new ECFieldElement[] { Z3 }, IsCompressed);
        }

        public override ECPoint TwicePlus(ECPoint b)
        {
            if (this == b)
                return ThreeTimes();
            if (this.IsInfinity)
                return b;
            if (b.IsInfinity)
                return Twice();

            ECFieldElement Y1 = this.RawYCoord;
            if (Y1.IsZero)
                return b;

            return Twice().Add(b);
        }

        public override ECPoint ThreeTimes()
        {
            if (this.IsInfinity || this.RawYCoord.IsZero)
                return this;

            // NOTE: Be careful about recursions between TwicePlus and ThreeTimes
            return Twice().Add(this);
        }

        public override ECPoint Negate()
        {
            if (IsInfinity)
                return this;

            return new SecP521R1Point(Curve, RawXCoord, RawYCoord.Negate(), RawZCoords, IsCompressed);
        }
    }
}

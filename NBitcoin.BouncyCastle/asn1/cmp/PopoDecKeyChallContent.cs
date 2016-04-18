using System;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Asn1.Cmp
{
	public class PopoDecKeyChallContent
	    : Asn1Encodable
	{
	    private readonly Asn1Sequence content;

	    private PopoDecKeyChallContent(Asn1Sequence seq)
	    {
	        content = seq;
	    }

	    public static PopoDecKeyChallContent GetInstance(object obj)
	    {
	        if (obj is PopoDecKeyChallContent)
	            return (PopoDecKeyChallContent)obj;

			if (obj is Asn1Sequence)
	            return new PopoDecKeyChallContent((Asn1Sequence)obj);

            throw new ArgumentException("Invalid object: " + Platform.GetTypeName(obj), "obj");
	    }

	    public virtual Challenge[] ToChallengeArray()
	    {
	        Challenge[] result = new Challenge[content.Count];
	        for (int i = 0; i != result.Length; ++i)
	        {
	            result[i] = Challenge.GetInstance(content[i]);
	        }
	        return result;
	    }

	    /**
	     * <pre>
	     * PopoDecKeyChallContent ::= SEQUENCE OF Challenge
	     * </pre>
	     * @return a basic ASN.1 object representation.
	     */
	    public override Asn1Object ToAsn1Object()
	    {
	        return content;
	    }
	}
}

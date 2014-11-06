namespace Org.BouncyCastle.Crypto.Tls
{
    public abstract class ExtensionType
    {
        /*
         * RFC 2546 2.3.
         */
        public const int server_name = 0;
        public const int max_fragment_length = 1;
        public const int client_certificate_url = 2;
        public const int trusted_ca_keys = 3;
        public const int truncated_hmac = 4;
        public const int status_request = 5;

        /*
         * RFC 4681
         */
        public const int user_mapping = 6;

        /*
         * RFC 4492 5.1.
         */
        public const int elliptic_curves = 10;
        public const int ec_point_formats = 11;

        /*
         * RFC 5054 2.8.1.
         */
        public const int srp = 12;

        /*
         * RFC 5246 7.4.1.4.
         */
        public const int signature_algorithms = 13;

        /*
         * RFC 5764 9.
         */
        public const int use_srtp = 14;

        /*
         * RFC 6520 6.
         */
        public const int heartbeat = 15;

        /*
         * RFC 7366
         */
        public const int encrypt_then_mac = 22;

        /*
         * draft-ietf-tls-session-hash-01
         * 
         * NOTE: Early code-point assignment
         */
        public const int extended_master_secret = 23;

        /*
         * RFC 5077 7.
         */
        public const int session_ticket = 35;

        /*
         * draft-ietf-tls-negotiated-ff-dhe-01
         * 
         * WARNING: Placeholder value; the real value is TBA
         */
        public static readonly int negotiated_ff_dhe_groups = 101;

        /*
         * RFC 5746 3.2.
         */
        public const int renegotiation_info = 0xff01;
    }
}

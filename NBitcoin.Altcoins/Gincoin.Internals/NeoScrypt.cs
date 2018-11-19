/*
 * Copyright (c) 2009 Colin Percival, 2011 ArtForz
 * Copyright (c) 2012 Andrew Moon (floodyberry)
 * Copyright (c) 2012 Samuel Neves <sneves@dei.uc.pt>
 * Copyright (c) 2014-2018 John Doering <ghostlander@phoenixcoin.org>
 * Copyright (c) 2018 C# Conversion by Mogwaicoin Team <mogwai@mogwaicoin.org>
 * 
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NeoScrypt
{
    public unsafe class NeoScrypt
    {
        #region "Interface"

        public static void neoscrypt(byte[] password, ref byte[] output, uint profile)
        {
            fixed (byte* password_p = password) {
                fixed (byte* output_p = output) {
                    neoscrypt(password_p, output_p, profile);
                }
            }
        }

        public static void neoscrypt_fastkdf(byte[] password, uint password_len, byte[] salt, uint salt_len, uint N, ref byte[] output, uint output_len)
        {
            fixed (byte* password_p = password) {
                fixed (byte* salt_p = salt) {
                    fixed (byte* output_p = output) {
                        neoscrypt_fastkdf(password_p, password_len, salt_p, salt_len, N, output_p, output_len);
                    }
                }
            }
        }

        public static void neoscrypt_blake2s(byte[] input, uint input_size, byte[] key, byte key_size, ref byte[] output, byte output_size)
        {
            fixed (byte* input_p = input) {
                fixed (byte* key_p = key) {
                    fixed (byte* output_p = output) {
                        neoscrypt_blake2s(input_p, input_size, key_p, key_size, output_p, output_size);
                    }
                }
            }
        }

        #endregion

        #region "Core Module"

        public const int BLOCK_SIZE = 64;
        public const int BUFFER_SIZE = BLOCK_SIZE * 8;
        public const int DIGEST_SIZE = 32;

        #region "NEOSCRYPT_SHA256"

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint MIN(uint a, uint b) { return ((a) < (b) ? a : b); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint MAX(uint a, uint b) { return ((a) > (b) ? a : b); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint ROTL32(uint a, int b) { return (((a) << (b)) | ((a) >> (32 - b))); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint ROTR32(uint a, int b) { return (((a) >> (b)) | ((a) << (32 - b))); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint U8TO32_BE(byte* p)
        {
            return
            (((uint)((p)[0]) << 24) | ((uint)((p)[1]) << 16) |
            ((uint)((p)[2]) << 8) | ((uint)((p)[3])));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void U32TO8_BE(byte* p, uint v)
        {
            (p)[0] = (byte)((v) >> 24); (p)[1] = (byte)((v) >> 16);
            (p)[2] = (byte)((v) >> 8); (p)[3] = (byte)((v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void U64TO8_BE(byte* p, ulong v)
        {
            (p)[0] = (byte)((v) >> 56); (p)[1] = (byte)((v) >> 48);
            (p)[2] = (byte)((v) >> 40); (p)[3] = (byte)((v) >> 32);
            (p)[4] = (byte)((v) >> 24); (p)[5] = (byte)((v) >> 16);
            (p)[6] = (byte)((v) >> 8); (p)[7] = (byte)((v));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Ch(uint x, uint y, uint z) { return (z ^ (x & (y ^ z))); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Maj(uint x, uint y, uint z) { return (((x | y) & z) | (x & y)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint S0(uint x) { return (ROTR32(x, 2) ^ ROTR32(x, 13) ^ ROTR32(x, 22)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint S1(uint x) { return (ROTR32(x, 6) ^ ROTR32(x, 11) ^ ROTR32(x, 25)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint G0(uint x) { return (ROTR32(x, 7) ^ ROTR32(x, 18) ^ (x >> 3)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint G1(uint x) { return (ROTR32(x, 17) ^ ROTR32(x, 19) ^ (x >> 10)); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint W0(byte* inp, int i) { return (U8TO32_BE(&inp[i * 4])); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint W1(uint[] w, int i) { return (G1(w[i - 2]) + w[i - 7] + G0(w[i - 15]) + w[i - 16]); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void STEP(uint[] r, uint[] w, int i)
        {
            uint t1 = S0(r[0]) + Maj(r[0], r[1], r[2]);
            uint t0 = r[7] + S1(r[4]) + Ch(r[4], r[5], r[6]) + sha256_constants[i] + w[i];
            r[7] = r[6];
            r[6] = r[5];
            r[5] = r[4];
            r[4] = r[3] + t0;
            r[3] = r[2];
            r[2] = r[1];
            r[1] = r[0];
            r[0] = t0 + t1;
        }

        static uint[] sha256_constants = new uint[64] {
            0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
            0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
            0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
            0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
            0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
            0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
            0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
            0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct sha256_hash_state
        {
            public fixed uint H[8];
            public ulong T;
            public uint leftover;
            public fixed byte buffer[BLOCK_SIZE * 8];
        }

        static void sha256_blocks(sha256_hash_state* S, byte* inp, uint blocks)
        {
            uint[] r = new uint[8];
            uint[] w = new uint[64];
            int i;

            for (i = 0; i < 8; i++)
                r[i] = S->H[i];

            while (blocks-- > 0) {
                for (i = 0; i < 16; i++) {
                    w[i] = W0(inp, i);
                }
                for (i = 16; i < 64; i++) {
                    w[i] = W1(w, i);
                }
                for (i = 0; i < 64; i++) {
                    STEP(r, w, i);
                }
                for (i = 0; i < 8; i++) {
                    r[i] += S->H[i];
                    S->H[i] = r[i];
                }
                S->T += (uint)(BLOCK_SIZE * 8);
                inp += BLOCK_SIZE;
            }
        }

        static void neoscrypt_hash_init_sha256(sha256_hash_state* S)
        {
            S->H[0] = 0x6A09E667;
            S->H[1] = 0xBB67AE85;
            S->H[2] = 0x3C6EF372;
            S->H[3] = 0xA54FF53A;
            S->H[4] = 0x510E527F;
            S->H[5] = 0x9B05688C;
            S->H[6] = 0x1F83D9AB;
            S->H[7] = 0x5BE0CD19;
            S->T = 0;
            S->leftover = 0;
        }

        static void neoscrypt_hash_update_sha256(sha256_hash_state* S, byte* inp, uint inlen)
        {
            uint blocks, want;

            /* handle the previous data */
            if (S->leftover > 0) {
                want = (uint)(BLOCK_SIZE - S->leftover);
                want = (want < inlen) ? want : inlen;
                neoscrypt_copy(S->buffer + S->leftover, inp, want);
                S->leftover += (uint)want;
                if (S->leftover < BLOCK_SIZE)
                    return;
                inp += want;
                inlen -= want;
                sha256_blocks(S, S->buffer, 1);
            }

            /* handle the current data */
            blocks = (uint)(inlen & ~(BLOCK_SIZE - 1));
            S->leftover = (uint)(inlen - blocks);
            if (blocks > 0) {
                sha256_blocks(S, inp, (uint)(blocks / BLOCK_SIZE));
                inp += blocks;
            }

            /* handle leftover data */
            if (S->leftover > 0)
                neoscrypt_copy(S->buffer, inp, S->leftover);
        }

        static void neoscrypt_hash_finish_sha256(sha256_hash_state* S, byte* hash)
        {
            UInt64 t = S->T + (S->leftover * 8);

            S->buffer[S->leftover] = 0x80;
            if (S->leftover <= 55) {
                neoscrypt_erase(S->buffer + S->leftover + 1, 55 - S->leftover);
            }
            else {
                neoscrypt_erase(S->buffer + S->leftover + 1, 63 - S->leftover);
                sha256_blocks(S, S->buffer, 1);
                neoscrypt_erase(S->buffer, 56);
            }

            U64TO8_BE(S->buffer + 56, t);
            sha256_blocks(S, S->buffer, 1);

            U32TO8_BE(&hash[0], S->H[0]);
            U32TO8_BE(&hash[4], S->H[1]);
            U32TO8_BE(&hash[8], S->H[2]);
            U32TO8_BE(&hash[12], S->H[3]);
            U32TO8_BE(&hash[16], S->H[4]);
            U32TO8_BE(&hash[20], S->H[5]);
            U32TO8_BE(&hash[24], S->H[6]);
            U32TO8_BE(&hash[28], S->H[7]);
        }

        /* HMAC for SHA-256 */

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct sha256_hmac_state
        {
            [MarshalAs(UnmanagedType.Struct)]
            public sha256_hash_state inner;
            [MarshalAs(UnmanagedType.Struct)]
            public sha256_hash_state outer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void neoscrypt_hmac_init_sha256(sha256_hmac_state* st, byte* key, uint keylen)
        {
            fixed (byte* pad = new byte[BLOCK_SIZE + DIGEST_SIZE]) {
                uint* P = (uint*)pad;
                uint i;

                /* The pad initialisation for the inner loop */
                for (i = 0; i < (BLOCK_SIZE >> 2); i++)
                    P[i] = 0x36363636;

                if (keylen <= BLOCK_SIZE) {
                    /* XOR the key into the pad */
                    neoscrypt_xor(pad, key, keylen);
                }
                else {
                    /* Hash the key and XOR into the pad */
                    sha256_hash_state st0;
                    neoscrypt_hash_init_sha256(&st0);
                    neoscrypt_hash_update_sha256(&st0, key, keylen);
                    neoscrypt_hash_finish_sha256(&st0, &pad[BLOCK_SIZE]);
                    neoscrypt_xor(&pad[0], &pad[BLOCK_SIZE], (uint)DIGEST_SIZE);
                }

                neoscrypt_hash_init_sha256(&st->inner);
                /* h(inner || pad) */
                neoscrypt_hash_update_sha256(&st->inner, pad, (uint)BLOCK_SIZE);

                /* The pad re-initialisation for the outer loop */
                for (i = 0; i < (BLOCK_SIZE >> 2); i++)
                    P[i] ^= (0x36363636 ^ 0x5C5C5C5C);

                neoscrypt_hash_init_sha256(&st->outer);
                /* h(outer || pad) */
                neoscrypt_hash_update_sha256(&st->outer, pad, (uint)BLOCK_SIZE);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void neoscrypt_hmac_update_sha256(sha256_hmac_state* st, byte* m, uint mlen)
        {
            /* h(inner || m...) */
            neoscrypt_hash_update_sha256(&st->inner, m, mlen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void neoscrypt_hmac_finish_sha256(sha256_hmac_state* st, byte* mac)
        {
            /* h(inner || m) */
            fixed (byte* innerhash = new byte[DIGEST_SIZE]) {
                neoscrypt_hash_finish_sha256(&st->inner, innerhash);

                /* h(outer || h(inner || m)) */
                neoscrypt_hash_update_sha256(&st->outer, innerhash, (uint)DIGEST_SIZE);
                neoscrypt_hash_finish_sha256(&st->outer, mac);
            }
        }

        /* PBKDF2 for SHA-256 */

        static void neoscrypt_pbkdf2_sha256(byte* password, uint password_len, byte* salt, uint salt_len, uint N, byte* output, uint output_len)
        {
            sha256_hmac_state hmac_pw = new sha256_hmac_state();
            sha256_hmac_state hmac_pw_salt = new sha256_hmac_state();
            sha256_hmac_state work = new sha256_hmac_state();

            fixed (byte* ti = new byte[DIGEST_SIZE]) {
                fixed (byte* u = new byte[DIGEST_SIZE]) {
                    fixed (byte* be = new byte[4]) {
                        uint i, j, k, blocks;

                        /* bytes must be <= (0xffffffff - (DIGEST_SIZE - 1)), which they will always be under scrypt */

                        /* hmac(password, ...) */
                        neoscrypt_hmac_init_sha256(&hmac_pw, password, password_len);

                        /* hmac(password, salt...) */
                        hmac_pw_salt = hmac_pw;
                        neoscrypt_hmac_update_sha256(&hmac_pw_salt, salt, salt_len);

                        blocks = (uint)((output_len + (DIGEST_SIZE - 1)) / DIGEST_SIZE);
                        for (i = 1; i <= blocks; i++) {
                            /* U1 = hmac(password, salt || be(i)) */
                            U32TO8_BE(be, i);
                            work = hmac_pw_salt;
                            neoscrypt_hmac_update_sha256(&work, be, 4);
                            neoscrypt_hmac_finish_sha256(&work, ti);
                            neoscrypt_copy(u, ti, (uint)DIGEST_SIZE);

                            /* T[i] = U1 ^ U2 ^ U3... */
                            for (j = 0; j < N - 1; j++) {
                                /* UX = hmac(password, U{X-1}) */
                                work = hmac_pw;
                                neoscrypt_hmac_update_sha256(&work, u, (uint)DIGEST_SIZE);
                                neoscrypt_hmac_finish_sha256(&work, u);

                                /* T[i] ^= UX */
                                for (k = 0; k < DIGEST_SIZE; k++)
                                    ti[k] ^= u[k];
                            }

                            neoscrypt_copy(output, ti, (output_len > (uint)DIGEST_SIZE) ? (uint)DIGEST_SIZE : output_len);
                            output += DIGEST_SIZE;
                            output_len -= (uint)DIGEST_SIZE;
                        }
                    }
                }
            }
        }

        #endregion

        #region "NEOSCRYPT_BLAKE256"

        static byte[] blake256_sigma = new byte[] {
             0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15,
            14, 10,  4,  8,  9, 15, 13,  6,  1, 12,  0,  2, 11,  7,  5,  3,
            11,  8, 12,  0,  5,  2, 15, 13, 10, 14,  3,  6,  7,  1,  9,  4,
             7,  9,  3,  1, 13, 12, 11, 14,  2,  6,  5, 10,  4,  0, 15,  8,
             9,  0,  5,  7,  2,  4, 10, 15, 14,  1, 11, 12,  6,  8,  3, 13,
             2, 12,  6, 10,  0, 11,  8,  3,  4, 13,  7,  5, 15, 14,  1,  9,
            12,  5,  1, 15, 14, 13,  4, 10,  0,  7,  6,  3,  9,  2,  8, 11,
            13, 11,  7, 14, 12,  1,  3,  9,  5,  0, 15,  4,  8,  6,  2, 10,
             6, 15, 14,  9, 11,  3,  0,  8, 12,  2, 13,  7,  1,  4, 10,  5,
            10,  2,  8,  4,  7,  6,  1,  5, 15, 11,  9, 14,  3, 12, 13,  0
        };

        static uint[] blake256_constants = new uint[] {
            0x243F6A88, 0x85A308D3, 0x13198A2E, 0x03707344,
            0xA4093822, 0x299F31D0, 0x082EFA98, 0xEC4E6C89,
            0x452821E6, 0x38D01377, 0xBE5466CF, 0x34E90C6C,
            0xC0AC29B7, 0xC97C50DD, 0x3F84D5B5, 0xB5470917
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct blake256_hash_state
        {
            public fixed uint H[8];
            public fixed uint T[2];
            public uint leftover;
            public fixed byte buffer[BLOCK_SIZE];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void G(uint a, uint b, uint c, uint d, uint e, byte* sigma, uint* m, uint* v) {
            v[a] += (m[sigma[e + 0]] ^ blake256_constants[sigma[e + 1]]) + v[b];
            v[d] = ROTR32(v[d] ^ v[a], 16);
            v[c] += v[d];
            v[b] = ROTR32(v[b] ^ v[c], 12);
            v[a] += (m[sigma[e + 1]] ^ blake256_constants[sigma[e + 0]]) + v[b];
            v[d] = ROTR32(v[d] ^ v[a], 8);
            v[c] += v[d];
            v[b] = ROTR32(v[b] ^ v[c], 7);
        }

        static void blake256_blocks(blake256_hash_state* S, byte* inp, uint blocks) {
            fixed (byte* blake256_sigma_p = blake256_sigma) {
                byte* sigma;
                byte* sigma_end = blake256_sigma_p + (10 * 16);
                fixed (uint* m = new uint[16 * sizeof(uint)]) {
                    fixed (uint* v = new uint[16 * sizeof(uint)]) {
                        fixed (uint* h = new uint[8 * sizeof(uint)]) {
                            fixed (uint* t = new uint[2 * sizeof(uint)]) {
                                uint i;

                                for (i = 0; i < 8; i++)
                                    h[i] = S->H[i];
                                for (i = 0; i < 2; i++)
                                    t[i] = S->T[i];

                                while (blocks-- > 0) {
                                    t[0] += 512;
                                    t[1] += (uint)((t[0] < 512) ? 1 : 0);

                                    for (i = 0; i < 8; i++)
                                        v[i] = h[i];
                                    for (i = 0; i < 4; i++)
                                        v[i + 8] = blake256_constants[i];
                                    for (i = 0; i < 2; i++)
                                        v[i + 12] = blake256_constants[i + 4] ^ t[0];
                                    for (i = 0; i < 2; i++)
                                        v[i + 14] = blake256_constants[i + 6] ^ t[1];

                                    for (i = 0; i < 16; i++)
                                        m[i] = U8TO32_BE(&inp[i * 4]);

                                    inp += 64;

                                    for (i = 0, sigma = blake256_sigma_p; i < 14; i++) {
                                        G(0, 4, 8, 12, 0, sigma, m, v);
                                        G(1, 5, 9, 13, 2, sigma, m, v);
                                        G(2, 6, 10, 14, 4, sigma, m, v);
                                        G(3, 7, 11, 15, 6, sigma, m, v);

                                        G(0, 5, 10, 15, 8, sigma, m, v);
                                        G(1, 6, 11, 12, 10, sigma, m, v);
                                        G(2, 7, 8, 13, 12, sigma, m, v);
                                        G(3, 4, 9, 14, 14, sigma, m, v);

                                        sigma += 16;
                                        if (sigma == sigma_end)
                                            sigma = blake256_sigma_p;
                                    }

                                    for (i = 0; i < 8; i++)
                                        h[i] ^= (v[i] ^ v[i + 8]);
                                }

                                for (i = 0; i < 8; i++)
                                    S->H[i] = h[i];
                                for (i = 0; i < 2; i++)
                                    S->T[i] = t[i];
                            }
                        }
                    }
                }
            }
        }

        static void neoscrypt_hash_init_blake256(blake256_hash_state* S)
        {
            S->H[0] = 0x6a09e667;
            S->H[1] = 0xbb67ae85;
            S->H[2] = 0x3c6ef372;
            S->H[3] = 0xa54ff53a;
            S->H[4] = 0x510e527f;
            S->H[5] = 0x9b05688c;
            S->H[6] = 0x1f83d9ab;
            S->H[7] = 0x5be0cd19;
            S->T[0] = 0;
            S->T[1] = 0;
            S->leftover = 0;
        }

        static void neoscrypt_hash_update_blake256(blake256_hash_state* S, byte* inp, uint inlen)
        {
            uint blocks, want;

            /* handle the previous data */
            if (S->leftover > 0) {
                want = (uint)(BLOCK_SIZE - S->leftover);
                want = (want < inlen) ? want : inlen;
                neoscrypt_copy(S->buffer + S->leftover, inp, want);
                S->leftover += (uint)want;
                if (S->leftover < BLOCK_SIZE)
                    return;
                inp += want;
                inlen -= want;
                blake256_blocks(S, S->buffer, 1);
            }

            /* handle the current data */
            blocks = (uint)(inlen & ~(BLOCK_SIZE - 1));
            S->leftover = (uint)(inlen - blocks);
            if (blocks > 0) {
                blake256_blocks(S, inp, (uint)(blocks / BLOCK_SIZE));
                inp += blocks;
            }

            /* handle leftover data */
            if (S->leftover > 0)
                neoscrypt_copy(S->buffer, inp, S->leftover);
        }

        static void neoscrypt_hash_finish_blake256(blake256_hash_state* S, byte* hash)
        {
            uint th, tl, bits;

            bits = (S->leftover << 3);
            tl = S->T[0] + bits;
            th = S->T[1];
            if (S->leftover == 0) {
                S->T[0] = (uint)(0x100000000 - 512);
                S->T[1] = (uint)(0x100000000 - 1);
            }
            else if (S->T[0] == 0) {
                S->T[0] = (uint)(0x100000000 - 512) + bits;
                S->T[1] = S->T[1] - 1;
            }
            else {
                S->T[0] -= (512 - bits);
            }

            S->buffer[S->leftover] = 0x80;
            if (S->leftover <= 55) {
                neoscrypt_erase(S->buffer + S->leftover + 1, 55 - S->leftover);
            }
            else {
                neoscrypt_erase(S->buffer + S->leftover + 1, 63 - S->leftover);
                blake256_blocks(S, S->buffer, 1);
                S->T[0] = (uint)(0x100000000 - 512);
                S->T[1] = (uint)(0x100000000 - 1);
                neoscrypt_erase(S->buffer, 56);
            }
            S->buffer[55] |= 1;
            U32TO8_BE(S->buffer + 56, th);
            U32TO8_BE(S->buffer + 60, tl);
            blake256_blocks(S, S->buffer, 1);

            U32TO8_BE(&hash[0], S->H[0]);
            U32TO8_BE(&hash[4], S->H[1]);
            U32TO8_BE(&hash[8], S->H[2]);
            U32TO8_BE(&hash[12], S->H[3]);
            U32TO8_BE(&hash[16], S->H[4]);
            U32TO8_BE(&hash[20], S->H[5]);
            U32TO8_BE(&hash[24], S->H[6]);
            U32TO8_BE(&hash[28], S->H[7]);
        }

        /* HMAC for BLAKE-256 */
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct blake256_hmac_state
        {
            [MarshalAs(UnmanagedType.Struct)]
            public blake256_hash_state inner;
            [MarshalAs(UnmanagedType.Struct)]
            public blake256_hash_state outer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void neoscrypt_hmac_init_blake256(blake256_hmac_state* st, byte* key, uint keylen)
        {
            fixed (byte* pad = new byte[BLOCK_SIZE + DIGEST_SIZE]) {
                uint* P = (uint*)pad;
                uint i;

                /* The pad initialisation for the inner loop */
                for (i = 0; i < (BLOCK_SIZE >> 2); i++)
                    P[i] = 0x36363636;

                if (keylen <= BLOCK_SIZE) {
                    /* XOR the key into the pad */
                    neoscrypt_xor(pad, key, keylen);
                }
                else {
                    /* Hash the key and XOR into the pad */
                    blake256_hash_state st0;
                    neoscrypt_hash_init_blake256(&st0);
                    neoscrypt_hash_update_blake256(&st0, key, keylen);
                    neoscrypt_hash_finish_blake256(&st0, &pad[BLOCK_SIZE]);
                    neoscrypt_xor(&pad[0], &pad[BLOCK_SIZE], (uint)DIGEST_SIZE);
                }

                neoscrypt_hash_init_blake256(&st->inner);
                /* h(inner || pad) */
                neoscrypt_hash_update_blake256(&st->inner, pad, (uint)BLOCK_SIZE);

                /* The pad re-initialisation for the outer loop */
                for (i = 0; i < (BLOCK_SIZE >> 2); i++)
                    P[i] ^= (0x36363636 ^ 0x5C5C5C5C);

                neoscrypt_hash_init_blake256(&st->outer);
                /* h(outer || pad) */
                neoscrypt_hash_update_blake256(&st->outer, pad, (uint)BLOCK_SIZE);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void neoscrypt_hmac_update_blake256(blake256_hmac_state* st, byte* m, uint mlen)
        {
            /* h(inner || m...) */
            neoscrypt_hash_update_blake256(&st->inner, m, mlen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void neoscrypt_hmac_finish_blake256(blake256_hmac_state* st, byte* mac)
        {
            /* h(inner || m) */
            fixed (byte* innerhash = new byte[DIGEST_SIZE]) {
                neoscrypt_hash_finish_blake256(&st->inner, innerhash);

                /* h(outer || h(inner || m)) */
                neoscrypt_hash_update_blake256(&st->outer, innerhash, (uint)DIGEST_SIZE);
                neoscrypt_hash_finish_blake256(&st->outer, mac);
            }
        }

        static void neoscrypt_pbkdf2_blake256(byte* password, uint password_len, byte* salt, uint salt_len, uint N, byte* output, uint output_len)
        {
            blake256_hmac_state hmac_pw, hmac_pw_salt, work;
            fixed (byte* ti = new byte[DIGEST_SIZE]) {
                fixed (byte* u = new byte[DIGEST_SIZE]) {
                    fixed (byte* be = new byte[4]) {
                        uint i, j, k, blocks;

                        /* bytes must be <= (0xffffffff - (DIGEST_SIZE - 1)), which they will always be under scrypt */

                        /* hmac(password, ...) */
                        neoscrypt_hmac_init_blake256(&hmac_pw, password, password_len);

                        /* hmac(password, salt...) */
                        hmac_pw_salt = hmac_pw;
                        neoscrypt_hmac_update_blake256(&hmac_pw_salt, salt, salt_len);

                        blocks = (uint)((output_len + (DIGEST_SIZE - 1)) / DIGEST_SIZE);
                        for (i = 1; i <= blocks; i++) {
                            /* U1 = hmac(password, salt || be(i)) */
                            U32TO8_BE(be, i);
                            work = hmac_pw_salt;
                            neoscrypt_hmac_update_blake256(&work, be, 4);
                            neoscrypt_hmac_finish_blake256(&work, ti);
                            neoscrypt_copy(u, ti, (uint)DIGEST_SIZE);

                            /* T[i] = U1 ^ U2 ^ U3... */
                            for (j = 0; j < N - 1; j++) {
                                /* UX = hmac(password, U{X-1}) */
                                work = hmac_pw;
                                neoscrypt_hmac_update_blake256(&work, u, (uint)DIGEST_SIZE);
                                neoscrypt_hmac_finish_blake256(&work, u);

                                /* T[i] ^= UX */
                                for (k = 0; k < (uint)DIGEST_SIZE; k++)
                                    ti[k] ^= u[k];
                            }

                            neoscrypt_copy(output, ti, (output_len > (uint)DIGEST_SIZE) ? (uint)DIGEST_SIZE : output_len);
                            output += DIGEST_SIZE;
                            output_len -= (uint)DIGEST_SIZE;
                        }
                    }
                }
            }
        }

        #endregion

        #region "NeoScrypt"

        /* Salsa20, rounds must be a multiple of 2 */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void quarter1(ref uint a, ref uint b, ref uint c, ref uint d) {
            uint t = a + d; t = ROTL32(t, 7); b ^= t;
            t = b + a; t = ROTL32(t, 9); c ^= t;
            t = c + b; t = ROTL32(t, 13); d ^= t;
            t = d + c; t = ROTL32(t, 18); a ^= t;
        }

        static void neoscrypt_salsa(uint* X, uint rounds)
        {
            uint x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15; //, t;

            x0 = X[0]; x1 = X[1]; x2 = X[2]; x3 = X[3];
            x4 = X[4]; x5 = X[5]; x6 = X[6]; x7 = X[7];
            x8 = X[8]; x9 = X[9]; x10 = X[10]; x11 = X[11];
            x12 = X[12]; x13 = X[13]; x14 = X[14]; x15 = X[15];
            
            for (; rounds > 0; rounds -= 2) {
                quarter1(ref x0, ref x4, ref x8, ref x12);
                quarter1(ref x5, ref x9, ref x13, ref x1);
                quarter1(ref x10, ref x14, ref x2, ref x6);
                quarter1(ref x15, ref x3, ref x7, ref x11);
                quarter1(ref x0, ref x1, ref x2, ref x3);
                quarter1(ref x5, ref x6, ref x7, ref x4);
                quarter1(ref x10, ref x11, ref x8, ref x9);
                quarter1(ref x15, ref x12, ref x13, ref x14);
            }

            X[0] += x0; X[1] += x1; X[2] += x2; X[3] += x3;
            X[4] += x4; X[5] += x5; X[6] += x6; X[7] += x7;
            X[8] += x8; X[9] += x9; X[10] += x10; X[11] += x11;
            X[12] += x12; X[13] += x13; X[14] += x14; X[15] += x15;
        }

        /* ChaCha20, rounds must be a multiple of 2 */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void quarter2(ref uint a, ref uint b, ref uint c, ref uint d) {
            uint t;
            a += b; t = d ^ a; d = ROTL32(t, 16);
            c += d; t = b ^ c; b = ROTL32(t, 12);
            a += b; t = d ^ a; d = ROTL32(t, 8);
            c += d; t = b ^ c; b = ROTL32(t, 7);
        }

        static void neoscrypt_chacha(uint* X, uint rounds)
        {
            uint x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15; //, t;

            x0 = X[0]; x1 = X[1]; x2 = X[2]; x3 = X[3];
            x4 = X[4]; x5 = X[5]; x6 = X[6]; x7 = X[7];
            x8 = X[8]; x9 = X[9]; x10 = X[10]; x11 = X[11];
            x12 = X[12]; x13 = X[13]; x14 = X[14]; x15 = X[15];

            for (; rounds > 0; rounds -= 2) {
                quarter2(ref x0, ref x4, ref x8, ref x12);
                quarter2(ref x1, ref x5, ref x9, ref x13);
                quarter2(ref x2, ref x6, ref x10, ref x14);
                quarter2(ref x3, ref x7, ref x11, ref x15);
                quarter2(ref x0, ref x5, ref x10, ref x15);
                quarter2(ref x1, ref x6, ref x11, ref x12);
                quarter2(ref x2, ref x7, ref x8, ref x13);
                quarter2(ref x3, ref x4, ref x9, ref x14);
            }

            X[0] += x0; X[1] += x1; X[2] += x2; X[3] += x3;
            X[4] += x4; X[5] += x5; X[6] += x6; X[7] += x7;
            X[8] += x8; X[9] += x9; X[10] += x10; X[11] += x11;
            X[12] += x12; X[13] += x13; X[14] += x14; X[15] += x15;
        }

        /* Fast 32-bit / 64-bit memcpy();
         * len must be a multiple of 32 bytes */
        static void neoscrypt_blkcpy(void* dstp, void* srcp, uint len)
        {
            uint* dst = (uint*)dstp;
            uint* src = (uint*)srcp;
            uint i;

            for (i = 0; i < (len / sizeof(uint)); i += 4) {
                dst[i] = src[i];
                dst[i + 1] = src[i + 1];
                dst[i + 2] = src[i + 2];
                dst[i + 3] = src[i + 3];
            }
        }

        /* Fast 32-bit / 64-bit block swapper;
         * len must be a multiple of 32 bytes */
        static void neoscrypt_blkswp(void* blkAp, void* blkBp, uint len)
        {
            uint* blkA = (uint*)blkAp;
            uint* blkB = (uint*)blkBp;
            uint t0, t1, t2, t3;
            uint i;

            for (i = 0; i < (len / sizeof(uint)); i += 4) {
                t0 = blkA[i];
                t1 = blkA[i + 1];
                t2 = blkA[i + 2];
                t3 = blkA[i + 3];
                blkA[i] = blkB[i];
                blkA[i + 1] = blkB[i + 1];
                blkA[i + 2] = blkB[i + 2];
                blkA[i + 3] = blkB[i + 3];
                blkB[i] = t0;
                blkB[i + 1] = t1;
                blkB[i + 2] = t2;
                blkB[i + 3] = t3;
            }
        }

        /* Fast 32-bit / 64-bit block XOR engine;
         * len must be a multiple of 32 bytes */
        static void neoscrypt_blkxor(void* dstp, void* srcp, uint len)
        {
            uint* dst = (uint*)dstp;
            uint* src = (uint*)srcp;
            uint i;

            for (i = 0; i < (len / sizeof(uint)); i += 4) {
                dst[i] ^= src[i];
                dst[i + 1] ^= src[i + 1];
                dst[i + 2] ^= src[i + 2];
                dst[i + 3] ^= src[i + 3];
            }
        }

        /* 32-bit / 64-bit optimised memcpy() */
        static void neoscrypt_copy(void* dstp, void* srcp, uint len)
        {
            uint* dst = (uint*)dstp;
            uint* src = (uint*)srcp;
            uint i, tail;

            for (i = 0; i < (len / sizeof(uint)); i++)
                dst[i] = src[i];

            tail = len & (sizeof(uint) - 1);
            if (tail > 0) {
                byte* dstb = (byte*)dstp;
                byte* srcb = (byte*)srcp;

                for (i = len - tail; i < len; i++)
                    dstb[i] = srcb[i];
            }
        }

        /* 32-bit / 64-bit optimised memory erase aka memset() to zero */
        static void neoscrypt_erase(void* dstp, uint len)
        {
            //const size_t null = 0;
            uint* dst = (uint*)dstp;
            uint i, tail;

            for (i = 0; i < (len / sizeof(uint)); i++)
                dst[i] = 0;

            tail = len & (sizeof(uint) - 1);
            if (tail > 0) {
                byte* dstb = (byte*)dstp;

                for (i = len - tail; i < len; i++)
                    dstb[i] = 0;
            }
        }

        /* Fail safe bytewise XOR engine */
        static void neoscrypt_xor(void* dstp, void* srcp, uint len)
        {
            byte* dst = (byte*)dstp;
            byte* src = (byte*)srcp;
            uint i;

            for (i = 0; i < len; i++)
                dst[i] ^= src[i];
        }

        #endregion

        #region "BLAKE2s"

        /* Parameter block of 32 bytes */
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct blake2s_param
        {
            public byte digest_length;
            public byte key_length;
            public byte fanout;
            public byte depth;
            public uint leaf_length;
            public fixed byte node_offset[6];
            public byte node_depth;
            public byte inner_length;
            public fixed byte salt[8];
            public fixed byte personal[8];
        }

        /* State block of 256 bytes */
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct blake2s_state
        {
            public fixed uint h[8];
            public fixed uint t[2];
            public fixed uint f[2];
            public fixed byte buf[2 * BLOCK_SIZE];
            public uint buflen;
            public fixed uint padding[3];
            public fixed byte tempbuf[BLOCK_SIZE];
        }

        static uint[] blake2s_IV = new uint[8] {
            0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A,
            0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19
        };

        /* Buffer mixer (compressor) */
        static void blake2s_compress(blake2s_state* S)
        {
            uint* v = (uint*)S->tempbuf;
            uint* m = (uint*)S->buf;
            uint t0, t1, t2, t3;

            v[0] = S->h[0];
            v[1] = S->h[1];
            v[2] = S->h[2];
            v[3] = S->h[3];
            v[4] = S->h[4];
            v[5] = S->h[5];
            v[6] = S->h[6];
            v[7] = S->h[7];
            v[8] = blake2s_IV[0];
            v[9] = blake2s_IV[1];
            v[10] = blake2s_IV[2];
            v[11] = blake2s_IV[3];
            v[12] = S->t[0] ^ blake2s_IV[4];
            v[13] = S->t[1] ^ blake2s_IV[5];
            v[14] = S->f[0] ^ blake2s_IV[6];
            v[15] = S->f[1] ^ blake2s_IV[7];

            /* Round 0 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[0];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[1];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[2];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[3];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[4];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[5];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[6];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[7];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[8];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[9];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[10];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[11];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[12];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[13];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[14];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[15];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 1 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[14];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[10];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[4];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[8];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[9];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[15];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[13];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[6];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[1];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[12];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[0];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[2];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[11];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[7];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[5];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[3];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 2 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[11];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[8];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[12];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[0];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[5];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[2];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[15];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[13];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[10];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[14];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[3];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[6];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[7];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[1];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[9];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[4];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 3 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[7];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[9];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[3];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[1];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[13];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[12];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[11];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[14];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[2];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[6];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[5];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[10];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[4];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[0];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[15];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[8];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 4 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[9];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[0];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[5];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[7];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[2];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[4];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[10];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[15];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[14];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[1];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[11];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[12];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[6];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[8];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[3];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[13];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 5 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[2];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[12];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[6];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[10];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[0];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[11];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[8];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[3];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[4];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[13];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[7];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[5];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[15];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[14];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[1];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[9];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 6 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[12];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[5];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[1];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[15];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[14];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[13];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[4];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[10];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[0];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[7];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[6];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[3];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[9];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[2];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[8];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[11];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 7 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[13];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[11];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[7];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[14];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[12];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[1];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[3];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[9];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[5];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[0];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[15];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[4];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[8];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[6];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[2];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[10];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 8 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[6];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[15];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[14];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[9];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[11];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[3];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[0];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[8];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[12];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[2];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[13];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[7];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[1];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[4];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[10];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[5];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            /* Round 9 */
            t0 = v[0];
            t1 = v[4];
            t0 = t0 + t1 + m[10];
            t3 = v[12];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[2];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            t0 = v[1];
            t1 = v[5];
            t0 = t0 + t1 + m[8];
            t3 = v[13];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[4];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[2];
            t1 = v[6];
            t0 = t0 + t1 + m[7];
            t3 = v[14];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[6];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[3];
            t1 = v[7];
            t0 = t0 + t1 + m[1];
            t3 = v[15];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[5];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[0];
            t1 = v[5];
            t0 = t0 + t1 + m[15];
            t3 = v[15];
            t2 = v[10];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[11];
            v[0] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[15] = t3;
            t2 = t2 + t3;
            v[10] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[5] = t1;

            t0 = v[1];
            t1 = v[6];
            t0 = t0 + t1 + m[9];
            t3 = v[12];
            t2 = v[11];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[14];
            v[1] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[12] = t3;
            t2 = t2 + t3;
            v[11] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[6] = t1;

            t0 = v[2];
            t1 = v[7];
            t0 = t0 + t1 + m[3];
            t3 = v[13];
            t2 = v[8];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[12];
            v[2] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[13] = t3;
            t2 = t2 + t3;
            v[8] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[7] = t1;

            t0 = v[3];
            t1 = v[4];
            t0 = t0 + t1 + m[13];
            t3 = v[14];
            t2 = v[9];
            t3 = ROTR32(t3 ^ t0, 16);
            t2 = t2 + t3;
            t1 = ROTR32(t1 ^ t2, 12);
            t0 = t0 + t1 + m[0];
            v[3] = t0;
            t3 = ROTR32(t3 ^ t0, 8);
            v[14] = t3;
            t2 = t2 + t3;
            v[9] = t2;
            t1 = ROTR32(t1 ^ t2, 7);
            v[4] = t1;

            S->h[0] ^= v[0] ^ v[8];
            S->h[1] ^= v[1] ^ v[9];
            S->h[2] ^= v[2] ^ v[10];
            S->h[3] ^= v[3] ^ v[11];
            S->h[4] ^= v[4] ^ v[12];
            S->h[5] ^= v[5] ^ v[13];
            S->h[6] ^= v[6] ^ v[14];
            S->h[7] ^= v[7] ^ v[15];
        }

        static void blake2s_update(blake2s_state* S, byte* input, uint input_size)
        {
            uint left, fill;

            while (input_size > 0) {
                left = S->buflen;
                fill = (uint)(2 * BLOCK_SIZE - left);
                if (input_size > fill) {
                    /* Buffer fill */
                    neoscrypt_copy(S->buf + left, input, fill);
                    S->buflen += fill;
                    /* Counter increment */
                    S->t[0] += (uint)BLOCK_SIZE;
                    /* Compress */
                    blake2s_compress(S);
                    /* Shift buffer left */
                    neoscrypt_copy(S->buf, S->buf + BLOCK_SIZE, (uint)BLOCK_SIZE);
                    S->buflen -= (uint)BLOCK_SIZE;
                    input += fill;
                    input_size -= fill;
                }
                else {
                    neoscrypt_copy(S->buf + left, input, input_size);
                    S->buflen += input_size;
                    /* Do not compress */
                    input += input_size;
                    input_size = 0;
                }
            }
        }

        static void neoscrypt_blake2s(void* input, uint input_size, void* key, byte key_size, void* output, byte output_size) {
            fixed (byte* block = new byte[BLOCK_SIZE]) {
                blake2s_param P; //[1];
                blake2s_state S; //[1];

                /* Initialise */
                neoscrypt_erase(&P, 32);
                P.digest_length = output_size;
                P.key_length = key_size;
                P.fanout = 1;
                P.depth = 1;

                fixed (uint* blake2s_IV_p = blake2s_IV) {
                    neoscrypt_erase(&S, 256);
                    neoscrypt_copy(&S, blake2s_IV_p, 32);
                    neoscrypt_xor(&S, &P, 32);

                    neoscrypt_erase(block, (uint)BLOCK_SIZE);
                    neoscrypt_copy(block, key, key_size);
                    blake2s_update(&S, block, (uint)BLOCK_SIZE);

                    /* Update */
                    blake2s_update(&S, (byte*)input, input_size);

                    /* Finish */
                    if (S.buflen > BLOCK_SIZE) {
                        S.t[0] += (uint)BLOCK_SIZE;
                        blake2s_compress(&S);
                        S.buflen -= (uint)BLOCK_SIZE;
                        neoscrypt_copy(S.buf, S.buf + BLOCK_SIZE, S.buflen);
                    }
                    S.t[0] += S.buflen;
                    S.f[0] = ~0U;
                    neoscrypt_erase(S.buf + S.buflen, (uint)(2 * BLOCK_SIZE - S.buflen));
                    blake2s_compress(&S);

                    /* Write back */
                    neoscrypt_copy(output, &S, output_size);
                }
            }
        }

        static uint FASTKDF_BUFFER_SIZE = 256;

        /* FastKDF, a fast buffered key derivation function:
         * FASTKDF_BUFFER_SIZE must be a power of 2;
         * password_len, salt_len and output_len should not exceed FASTKDF_BUFFER_SIZE;
         * prf_output_size must be <= prf_key_size; */
        static void neoscrypt_fastkdf(byte* password, uint password_len, byte* salt, uint salt_len, uint N, byte* output, uint output_len)
        {
            uint stack_align = 0x40;
            uint kdf_buf_size = FASTKDF_BUFFER_SIZE;
            uint prf_input_size = 64;
            uint prf_key_size = 32;
            uint prf_output_size = 32;
            uint bufptr, a, b, i, j;
            byte* A;
            byte* B;
            byte* prf_input;
            byte* prf_key;
            byte* prf_output;

            /* Align and set up the buffers in stack */
            fixed (byte* stack = new byte[(int)(2 * kdf_buf_size + prf_input_size + prf_key_size + prf_output_size + stack_align)]) {
                A = (byte*)stack;//A = (byte*)(((uint)stack & ~(stack_align - 1)) + stack_align);
                B = &A[kdf_buf_size + prf_input_size];
                prf_output = &A[2 * kdf_buf_size + prf_input_size + prf_key_size];

                /* Initialise the password buffer */
                if (password_len > kdf_buf_size)
                    password_len = kdf_buf_size;

                a = kdf_buf_size / password_len;
                for (i = 0; i < a; i++)
                    neoscrypt_copy(&A[i * password_len], &password[0], password_len);
                b = kdf_buf_size - a * password_len;
                if (b > 0)
                    neoscrypt_copy(&A[a * password_len], &password[0], b);
                neoscrypt_copy(&A[kdf_buf_size], &password[0], prf_input_size);

                /* Initialise the salt buffer */
                if (salt_len > kdf_buf_size)
                    salt_len = kdf_buf_size;

                a = kdf_buf_size / salt_len;
                for (i = 0; i < a; i++)
                    neoscrypt_copy(&B[i * salt_len], &salt[0], salt_len);
                b = kdf_buf_size - a * salt_len;
                if (b > 0)
                    neoscrypt_copy(&B[a * salt_len], &salt[0], b);
                neoscrypt_copy(&B[kdf_buf_size], &salt[0], prf_key_size);

                /* The primary iteration */
                for (i = 0, bufptr = 0; i < N; i++) {

                    /* Map the PRF input buffer */
                    prf_input = &A[bufptr];

                    /* Map the PRF key buffer */
                    prf_key = &B[bufptr];

                    /* PRF */
                    neoscrypt_blake2s(prf_input, prf_input_size, prf_key, (byte)prf_key_size, prf_output, (byte)prf_output_size);

                    /* Calculate the next buffer pointer */
                    for (j = 0, bufptr = 0; j < prf_output_size; j++)
                        bufptr += prf_output[j];
                    bufptr &= (kdf_buf_size - 1);

                    /* Modify the salt buffer */
                    neoscrypt_xor(&B[bufptr], &prf_output[0], prf_output_size);

                    /* Head modified, tail updated */
                    if (bufptr < prf_key_size)
                        neoscrypt_copy(&B[kdf_buf_size + bufptr], &B[bufptr],
                          MIN(prf_output_size, prf_key_size - bufptr));
                    /* Tail modified, head updated */
                    else if ((kdf_buf_size - bufptr) < prf_output_size)
                        neoscrypt_copy(&B[0], &B[kdf_buf_size],
                          prf_output_size - (kdf_buf_size - bufptr));

                }

                /* Modify and copy into the output buffer */
                if (output_len > kdf_buf_size)
                    output_len = kdf_buf_size;

                a = kdf_buf_size - bufptr;
                if (a >= output_len) {
                    neoscrypt_xor(&B[bufptr], &A[0], output_len);
                    neoscrypt_copy(&output[0], &B[bufptr], output_len);
                }
                else {
                    neoscrypt_xor(&B[bufptr], &A[0], a);
                    neoscrypt_xor(&B[0], &A[a], output_len - a);
                    neoscrypt_copy(&output[0], &B[bufptr], a);
                    neoscrypt_copy(&output[a], &B[0], output_len - a);
                }
            }
        }

        /* Initialisation vector with a parameter block XOR'ed in */
        static uint[] blake2s_IV_P_XOR = new uint[8] {
            0x6B08C647, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A,
            0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19
        };

        /* Performance optimised FastKDF with BLAKE2s integrated */
        static void neoscrypt_fastkdf_opt(byte* password, byte* salt, byte* output, uint mode)
        {
            uint stack_align = 0x40;
            uint bufptr, output_len, i, j;
            byte* A;
            byte* B;
            uint* S;

            /* Align and set up the buffers in stack */
            fixed (byte* stack = new byte[(int)(864 + stack_align)]) {
                A = (byte*)stack; // A = (byte*)(((uint)stack & ~(stack_align - 1)) + stack_align);
                B = &A[320];
                S = (uint*)&A[608];

                neoscrypt_copy(&A[0], &password[0], 80);
                neoscrypt_copy(&A[80], &password[0], 80);
                neoscrypt_copy(&A[160], &password[0], 80);
                neoscrypt_copy(&A[240], &password[0], 16);
                neoscrypt_copy(&A[256], &password[0], 64);

                if (mode == 0) {
                    output_len = 256;
                    neoscrypt_copy(&B[0], &salt[0], 80);
                    neoscrypt_copy(&B[80], &salt[0], 80);
                    neoscrypt_copy(&B[160], &salt[0], 80);
                    neoscrypt_copy(&B[240], &salt[0], 16);
                    neoscrypt_copy(&B[256], &salt[0], 32);
                }
                else {
                    output_len = 32;
                    neoscrypt_copy(&B[0], &salt[0], 256);
                    neoscrypt_copy(&B[256], &salt[0], 32);
                }

                for (i = 0, bufptr = 0; i < 32; i++) {

                    fixed (uint* blake2s_IV_P_XOR_p = blake2s_IV_P_XOR) {
                        /* BLAKE2s: initialise */
                        neoscrypt_copy(&S[0], blake2s_IV_P_XOR_p, 32);
                        neoscrypt_erase(&S[8], 16);

                        /* BLAKE2s: update key */
                        neoscrypt_copy(&S[12], &B[bufptr], 32);
                        neoscrypt_erase(&S[20], 32);

                        /* BLAKE2s: compress IV using key */
                        S[8] = 64;
                        blake2s_compress((blake2s_state*)S);

                        /* BLAKE2s: update input */
                        neoscrypt_copy(&S[12], &A[bufptr], 64);

                        /* BLAKE2s: compress again using input */
                        S[8] = 128;
                        S[10] = ~0U;
                        blake2s_compress((blake2s_state*)S);

                        for (j = 0, bufptr = 0; j < 8; j++) {
                            bufptr += S[j];
                            bufptr += (S[j] >> 8);
                            bufptr += (S[j] >> 16);
                            bufptr += (S[j] >> 24);
                        }
                        bufptr &= 0xFF;

                        neoscrypt_xor(&B[bufptr], &S[0], 32);

                        if (bufptr < 32)
                            neoscrypt_copy(&B[256 + bufptr], &B[bufptr], 32 - bufptr);
                        else if (bufptr > 224)
                            neoscrypt_copy(&B[0], &B[256], bufptr - 224);
                    }
                }

                i = 256 - bufptr;
                if (i >= output_len) {
                    neoscrypt_xor(&B[bufptr], &A[0], output_len);
                    neoscrypt_copy(&output[0], &B[bufptr], output_len);
                }
                else {
                    neoscrypt_xor(&B[bufptr], &A[0], i);
                    neoscrypt_xor(&B[0], &A[i], output_len - i);
                    neoscrypt_copy(&output[0], &B[bufptr], i);
                    neoscrypt_copy(&output[i], &B[0], output_len - i);
                }
            }
        }

        /* Configurable optimised block mixer */
        static void neoscrypt_blkmix(uint* X, uint* Y, uint r, uint mixmode)
        {
            uint i, mixer, rounds;

            mixer = mixmode >> 8;
            rounds = mixmode & 0xFF;

            /* NeoScrypt flow:                   Scrypt flow:
                 Xa ^= Xd;  M(Xa'); Ya = Xa";      Xa ^= Xb;  M(Xa'); Ya = Xa";
                 Xb ^= Xa"; M(Xb'); Yb = Xb";      Xb ^= Xa"; M(Xb'); Yb = Xb";
                 Xc ^= Xb"; M(Xc'); Yc = Xc";      Xa" = Ya;
                 Xd ^= Xc"; M(Xd'); Yd = Xd";      Xb" = Yb;
                 Xa" = Ya; Xb" = Yc;
                 Xc" = Yb; Xd" = Yd; */

            if (r == 1) {
                if (mixer > 0) {
                    neoscrypt_blkxor(&X[0], &X[16], (uint)BLOCK_SIZE);
                    neoscrypt_chacha(&X[0], rounds);
                    neoscrypt_blkxor(&X[16], &X[0], (uint)BLOCK_SIZE);
                    neoscrypt_chacha(&X[16], rounds);
                }
                else {
                    neoscrypt_blkxor(&X[0], &X[16], (uint)BLOCK_SIZE);
                    neoscrypt_salsa(&X[0], rounds);
                    neoscrypt_blkxor(&X[16], &X[0], (uint)BLOCK_SIZE);
                    neoscrypt_salsa(&X[16], rounds);
                }
                return;
            }

            if (r == 2) {
                if (mixer > 0) {
                    neoscrypt_blkxor(&X[0], &X[48], (uint)BLOCK_SIZE);
                    neoscrypt_chacha(&X[0], rounds);
                    neoscrypt_blkxor(&X[16], &X[0], (uint)BLOCK_SIZE);
                    neoscrypt_chacha(&X[16], rounds);
                    neoscrypt_blkxor(&X[32], &X[16], (uint)BLOCK_SIZE);
                    neoscrypt_chacha(&X[32], rounds);
                    neoscrypt_blkxor(&X[48], &X[32], (uint)BLOCK_SIZE);
                    neoscrypt_chacha(&X[48], rounds);
                    neoscrypt_blkswp(&X[16], &X[32], (uint)BLOCK_SIZE);
                }
                else {
                    neoscrypt_blkxor(&X[0], &X[48], (uint)BLOCK_SIZE);
                    neoscrypt_salsa(&X[0], rounds);
                    neoscrypt_blkxor(&X[16], &X[0], (uint)BLOCK_SIZE);
                    neoscrypt_salsa(&X[16], rounds);
                    neoscrypt_blkxor(&X[32], &X[16], (uint)BLOCK_SIZE);
                    neoscrypt_salsa(&X[32], rounds);
                    neoscrypt_blkxor(&X[48], &X[32], (uint)BLOCK_SIZE);
                    neoscrypt_salsa(&X[48], rounds);
                    neoscrypt_blkswp(&X[16], &X[32], (uint)BLOCK_SIZE);
                }
                return;
            }

            /* Reference code for any reasonable r */
            for (i = 0; i < 2 * r; i++) {
                if (i > 0) neoscrypt_blkxor(&X[16 * i], &X[16 * (i - 1)], (uint)BLOCK_SIZE);
                else neoscrypt_blkxor(&X[0], &X[16 * (2 * r - 1)], (uint)BLOCK_SIZE);
                if (mixer > 0)
                    neoscrypt_chacha(&X[16 * i], rounds);
                else
                    neoscrypt_salsa(&X[16 * i], rounds);
                neoscrypt_blkcpy(&Y[16 * i], &X[16 * i], (uint)BLOCK_SIZE);
            }
            for (i = 0; i < r; i++)
                neoscrypt_blkcpy(&X[16 * i], &Y[16 * 2 * i], (uint)BLOCK_SIZE);
            for (i = 0; i < r; i++)
                neoscrypt_blkcpy(&X[16 * (i + r)], &Y[16 * (2 * i + 1)], (uint)BLOCK_SIZE);
        }

        /* NeoScrypt core engine:
         * p = 1, salt = password;
         * Basic customisation (required):
         *   profile bit 0:
         *     0 = NeoScrypt(128, 2, 1) with Salsa20/20 and ChaCha20/20;
         *     1 = Scrypt(1024, 1, 1) with Salsa20/8;
         *   profile bits 4 to 1:
         *     0000 = FastKDF-BLAKE2s;
         *     0001 = PBKDF2-HMAC-SHA256;
         *     0010 = PBKDF2-HMAC-BLAKE256;
         * Extended customisation (optional):
         *   profile bit 31:
         *     0 = extended customisation absent;
         *     1 = extended customisation present;
         *   profile bits 7 to 5 (rfactor):
         *     000 = r of 1;
         *     001 = r of 2;
         *     010 = r of 4;
         *     ...
         *     111 = r of 128;
         *   profile bits 12 to 8 (Nfactor):
         *     00000 = N of 2;
         *     00001 = N of 4;
         *     00010 = N of 8;
         *     .....
         *     00110 = N of 128;
         *     .....
         *     01001 = N of 1024;
         *     .....
         *     11110 = N of 2147483648;
         *   profile bits 30 to 13 are reserved */
        static void neoscrypt(byte* password, byte* output, uint profile)
        {
            uint stack_align = 0x40;
            uint N = 128, r = 2, dblmix = 1, mixmode = 0x14;
            uint kdf, i, j;
            uint* X;
            uint* Y;
            uint* Z;
            uint* V;

            if ((profile & 0x1) == 1) {
                N = 1024;        /* N = (1 << (Nfactor + 1)); */
                r = 1;           /* r = (1 << rfactor); */
                dblmix = 0;      /* Salsa only */
                mixmode = 0x08;  /* 8 rounds */
            }

            if (profile >> 31 != 0) {
                N = (uint)(1 << ((((int)profile >> 8) & 0x1F) + 1));
                r = (uint)(1 << (((int)profile >> 5) & 0x7));
            }

            fixed (byte* stack = new byte[(int)((N + 3) * r * 2 * BLOCK_SIZE + stack_align)]) {
                /* X = r * 2 * BLOCK_SIZE */
                X = (uint*)stack; // X = (uint*)(((uint)stack & ~(stack_align - 1)) + stack_align);
                /* Z is a copy of X for ChaCha */
                Z = &X[32 * r];
                /* Y is an X sized temporal space */
                Y = &X[64 * r];
                /* V = N * r * 2 * BLOCK_SIZE */
                V = &X[96 * r];

                /* X = KDF(password, salt) */
                kdf = (profile >> 1) & 0xF;

                switch (kdf) {
                    default:
                    case (0x0):
                        //# ifdef NEOSCRYPT_OPT
                        neoscrypt_fastkdf_opt(password, password, (byte*)X, 0);
                        //#else
                        //                    neoscrypt_fastkdf(password, 80, password, 80, 32,
                        //                      (uchar*)X, r * 2 * BLOCK_SIZE);
                        //#endif
                        break;

                    //# ifdef NEOSCRYPT_SHA256
                    case (0x1):
                        neoscrypt_pbkdf2_sha256(password, 80, password, 80, 1, (byte*)X, (uint)(r * 2 * BLOCK_SIZE));
                        break;
                    //#endif

                    //# ifdef NEOSCRYPT_BLAKE256
                    case (0x2):
                        neoscrypt_pbkdf2_blake256(password, 80, password, 80, 1, (byte*)X, (uint)(r * 2 * BLOCK_SIZE));
                        break;
                        //#endif
                }
                /* Process ChaCha 1st, Salsa 2nd and XOR them into FastKDF; otherwise Salsa only */

                if (dblmix > 0) {
                    /* blkcpy(Z, X) */
                    neoscrypt_blkcpy(&Z[0], &X[0], (uint)(r * 2 * BLOCK_SIZE));

                    /* Z = SMix(Z) */
                    for (i = 0; i < N; i++) {
                        /* blkcpy(V, Z) */
                        neoscrypt_blkcpy(&V[i * (32 * r)], &Z[0], (uint)(r * 2 * BLOCK_SIZE));
                        /* blkmix(Z, Y) */
                        neoscrypt_blkmix(&Z[0], &Y[0], r, (mixmode | 0x0100));
                    }

                    for (i = 0; i < N; i++) {
                        /* integerify(Z) mod N */
                        j = (32 * r) * (Z[16 * (2 * r - 1)] & (N - 1));
                        /* blkxor(Z, V) */
                        neoscrypt_blkxor(&Z[0], &V[j], (uint)(r * 2 * BLOCK_SIZE));
                        /* blkmix(Z, Y) */
                        neoscrypt_blkmix(&Z[0], &Y[0], r, (mixmode | 0x0100));
                    }
                }

                /* X = SMix(X) */
                for (i = 0; i < N; i++) {
                    /* blkcpy(V, X) */
                    neoscrypt_blkcpy(&V[i * (32 * r)], &X[0], (uint)(r * 2 * BLOCK_SIZE));
                    /* blkmix(X, Y) */
                    neoscrypt_blkmix(&X[0], &Y[0], r, mixmode);
                }
                for (i = 0; i < N; i++) {
                    /* integerify(X) mod N */
                    j = (32 * r) * (X[16 * (2 * r - 1)] & (N - 1));
                    /* blkxor(X, V) */
                    neoscrypt_blkxor(&X[0], &V[j], (uint)(r * 2 * BLOCK_SIZE));
                    /* blkmix(X, Y) */
                    neoscrypt_blkmix(&X[0], &Y[0], r, mixmode);
                }

                if (dblmix > 0)
                    /* blkxor(X, Z) */
                    neoscrypt_blkxor(&X[0], &Z[0], (uint)(r * 2 * BLOCK_SIZE));

                /* output = KDF(password, X) */
                switch (kdf) {

                    default:
                    case (0x0):
                        //# ifdef NEOSCRYPT_OPT
                        neoscrypt_fastkdf_opt(password, (byte*)X, output, 1);
                        //#else
                        //                    neoscrypt_fastkdf(password, 80, (uchar*)X,
                        //                      r * 2 * BLOCK_SIZE, 32, output, 32);
                        //#endif
                        break;

                    //# ifdef NEOSCRYPT_SHA256
                    case (0x1):
                        neoscrypt_pbkdf2_sha256(password, 80, (byte*)X, (uint)(r * 2 * BLOCK_SIZE), 1, output, 32);
                        break;
                    //#endif

                    //# ifdef NEOSCRYPT_BLAKE256
                    case (0x2):
                        neoscrypt_pbkdf2_blake256(password, 80, (byte*)X, (uint)(r * 2 * BLOCK_SIZE), 1, output, 32);
                        break;
                        //#endif
                }
            }
        }

        /* 4-way NeoScrypt(128, 2, 1) with Salsa20/20 and ChaCha20/20 */
        //static void neoscrypt_4way(byte* password, byte* output, byte* scratchpad) {
        //    const uint N = 128, r = 2, double_rounds = 10;
        //    uint* X, Z, V, Y, P;
        //    uint i, j0, j1, j2, j3, k;

        //    /* 2 * BLOCK_SIZE compacted to 128 below */
        //    ;

        //    /* Scratchpad size is 4 * ((N + 3) * r * 128 + 80) bytes */

        //    X = (uint*)&scratchpad[0];
        //    Z = &X[4 * 32 * r];
        //    V = &X[4 * 64 * r];
        //    /* Y is a temporary work space */
        //    Y = &X[4 * (N + 2) * 32 * r];
        //    /* P is a set of passwords 80 bytes each */
        //    P = &X[4 * (N + 3) * 32 * r];

        //    /* Load the password and increment nonces */
        //    for (k = 0; k < 4; k++) {
        //        neoscrypt_copy(&P[k * 20], password, 80);
        //        P[(k + 1) * 20 - 1] += k;
        //    }

        //    neoscrypt_fastkdf_4way((byte*)&P[0], (byte*)&P[0], (byte*)&Y[0], (byte*)&scratchpad[0], 0);

        //    neoscrypt_pack_4way(&X[0], &Y[0], 4 * r * 128);

        //    neoscrypt_blkcpy(&Z[0], &X[0], 4 * r * 128);

        //    for (i = 0; i < N; i++) {
        //        neoscrypt_blkcpy(&V[i * r * 128], &Z[0], 4 * r * 128);
        //        neoscrypt_xor_chacha_4way(&Z[0], &Z[192], &Y[0], double_rounds);
        //        neoscrypt_xor_chacha_4way(&Z[64], &Z[0], &Y[0], double_rounds);
        //        neoscrypt_xor_chacha_4way(&Z[128], &Z[64], &Y[0], double_rounds);
        //        neoscrypt_xor_chacha_4way(&Z[192], &Z[128], &Y[0], double_rounds);
        //        neoscrypt_blkswp(&Z[64], &Z[128], r * 128);
        //    }

        //    for (i = 0; i < N; i++) {
        //        j0 = (4 * r * 32) * (Z[64 * (2 * r - 1)] & (N - 1));
        //        j1 = (4 * r * 32) * (Z[1 + (64 * (2 * r - 1))] & (N - 1));
        //        j2 = (4 * r * 32) * (Z[2 + (64 * (2 * r - 1))] & (N - 1));
        //        j3 = (4 * r * 32) * (Z[3 + (64 * (2 * r - 1))] & (N - 1));
        //        neoscrypt_xor_4way(&Z[0], &V[j0], &V[j1], &V[j2], &V[j3], 4 * r * 128);
        //        neoscrypt_xor_chacha_4way(&Z[0], &Z[192], &Y[0], double_rounds);
        //        neoscrypt_xor_chacha_4way(&Z[64], &Z[0], &Y[0], double_rounds);
        //        neoscrypt_xor_chacha_4way(&Z[128], &Z[64], &Y[0], double_rounds);
        //        neoscrypt_xor_chacha_4way(&Z[192], &Z[128], &Y[0], double_rounds);
        //        neoscrypt_blkswp(&Z[64], &Z[128], 256);
        //    }

        //    for (i = 0; i < N; i++) {
        //        neoscrypt_blkcpy(&V[i * r * 128], &X[0], 4 * r * 128);
        //        neoscrypt_xor_salsa_4way(&X[0], &X[192], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[64], &X[0], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[128], &X[64], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[192], &X[128], &Y[0], double_rounds);
        //        neoscrypt_blkswp(&X[64], &X[128], r * 128);
        //    }

        //    for (i = 0; i < N; i++) {
        //        j0 = (4 * r * 32) * (X[64 * (2 * r - 1)] & (N - 1));
        //        j1 = (4 * r * 32) * (X[1 + (64 * (2 * r - 1))] & (N - 1));
        //        j2 = (4 * r * 32) * (X[2 + (64 * (2 * r - 1))] & (N - 1));
        //        j3 = (4 * r * 32) * (X[3 + (64 * (2 * r - 1))] & (N - 1));
        //        neoscrypt_xor_4way(&X[0], &V[j0], &V[j1], &V[j2], &V[j3], 4 * r * 128);
        //        neoscrypt_xor_salsa_4way(&X[0], &X[192], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[64], &X[0], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[128], &X[64], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[192], &X[128], &Y[0], double_rounds);
        //        neoscrypt_blkswp(&X[64], &X[128], r * 128);
        //    }

        //    neoscrypt_blkxor(&X[0], &Z[0], 4 * r * 128);
        //    neoscrypt_unpack_4way(&Y[0], &X[0], 4 * r * 128);
        //    neoscrypt_fastkdf_4way((byte*)&P[0], (byte*)&Y[0], (byte*)&output[0], (byte*)&scratchpad[0], 1);
        //}

        /* 4-way Scrypt(1024, 1, 1) with Salsa20/8 */
        //static void scrypt_4way(byte* password, byte* output, byte* scratchpad)
        //{
        //    const uint N = 1024, r = 1, double_rounds = 4;
        //    uint* X, V, Y, P;
        //    uint i, j0, j1, j2, j3, k;

        //    /* Scratchpad size is 4 * ((N + 2) * r * 128 + 80) bytes */

        //    X = (uint*)&scratchpad[0];
        //    V = &X[4 * 32 * r];
        //    Y = &X[4 * (N + 1) * 32 * r];
        //    P = &X[4 * (N + 2) * 32 * r];

        //    for (k = 0; k < 4; k++) {
        //        neoscrypt_copy(&P[k * 20], password, 80);
        //        P[(k + 1) * 20 - 1] += k;
        //    }

        //    for (k = 0; k < 4; k++)
        //        neoscrypt_pbkdf2_sha256((byte*)&P[k * 20], 80,
        //          (byte*)&P[k * 20], 80, 1,
        //          (byte*)&Y[k * r * 32], r * 128);

        //    neoscrypt_pack_4way(&X[0], &Y[0], 4 * r * 128);

        //    for (i = 0; i < N; i++) {
        //        neoscrypt_blkcpy(&V[i * r * 128], &X[0], 4 * r * 128);
        //        neoscrypt_xor_salsa_4way(&X[0], &X[64], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[64], &X[0], &Y[0], double_rounds);
        //    }

        //    for (i = 0; i < N; i++) {
        //        j0 = (4 * r * 32) * (X[64 * (2 * r - 1)] & (N - 1));
        //        j1 = (4 * r * 32) * (X[1 + (64 * (2 * r - 1))] & (N - 1));
        //        j2 = (4 * r * 32) * (X[2 + (64 * (2 * r - 1))] & (N - 1));
        //        j3 = (4 * r * 32) * (X[3 + (64 * (2 * r - 1))] & (N - 1));
        //        neoscrypt_xor_4way(&X[0], &V[j0], &V[j1], &V[j2], &V[j3], 4 * r * 128);
        //        neoscrypt_xor_salsa_4way(&X[0], &X[64], &Y[0], double_rounds);
        //        neoscrypt_xor_salsa_4way(&X[64], &X[0], &Y[0], double_rounds);
        //    }

        //    neoscrypt_unpack_4way(&Y[0], &X[0], 4 * r * 128);

        //    for (k = 0; k < 4; k++)
        //        neoscrypt_pbkdf2_sha256((byte*)&P[k * 20], 80,
        //          (byte*)&Y[k * r * 32], r * 128, 1,
        //          (byte*)&output[k * 32], 32);
        //}

        /* 4-way initialisation vector with a parameter block XOR'ed in */
        static uint[] blake2s_IV_P_XOR_4way = new uint[32] {
            0x6B08C647, 0x6B08C647, 0x6B08C647, 0x6B08C647,
            0xBB67AE85, 0xBB67AE85, 0xBB67AE85, 0xBB67AE85,
            0x3C6EF372, 0x3C6EF372, 0x3C6EF372, 0x3C6EF372,
            0xA54FF53A, 0xA54FF53A, 0xA54FF53A, 0xA54FF53A,
            0x510E527F, 0x510E527F, 0x510E527F, 0x510E527F,
            0x9B05688C, 0x9B05688C, 0x9B05688C, 0x9B05688C,
            0x1F83D9AB, 0x1F83D9AB, 0x1F83D9AB, 0x1F83D9AB,
            0x5BE0CD19, 0x5BE0CD19, 0x5BE0CD19, 0x5BE0CD19
        };

        /* 4-way BLAKE2s implementation */
        //static void neoscrypt_blake2s_4way(byte* input, byte* key, byte* output)
        //{
        //    uint stack_align = 0x40;
        //    uint* T;

        //    /* Align and set up the buffer in stack */
        //    byte* stack = (byte*)Marshal.AllocHGlobal((int)(704 + stack_align));
        //    T = (uint*)(((uint)stack & ~(stack_align - 1)) + stack_align);

        //    fixed (uint* blake2s_IV_P_XOR_4way_p = blake2s_IV_P_XOR_4way) {
        //        /* Initialise */
        //        neoscrypt_copy(&T[0], blake2s_IV_P_XOR_4way_p, 128);
        //        neoscrypt_erase(&T[32], 64);

        //        /* Update keys */
        //        neoscrypt_pack_4way(&T[48], &key[0], 128);
        //        neoscrypt_erase(&T[80], 128);

        //        /* Compress IVs using keys */
        //        T[32] = 64;
        //        T[33] = 64;
        //        T[34] = 64;
        //        T[35] = 64;
        //        blake2s_compress_4way(&T[0]);

        //        /* Update inputs */
        //        neoscrypt_pack_4way(&T[48], &input[0], 256);

        //        /* Compress using inputs */
        //        T[32] = 128;
        //        T[33] = 128;
        //        T[34] = 128;
        //        T[35] = 128;
        //        T[40] = ~0U;
        //        T[41] = ~0U;
        //        T[42] = ~0U;
        //        T[43] = ~0U;
        //        blake2s_compress_4way(&T[0]);

        //        neoscrypt_unpack_4way(&output[0], &T[0], 128);
        //    }
        //}

        /* 4-way FastKDF with BLAKE2s integrated */
        //static void neoscrypt_fastkdf_4way(byte* password, byte* salt, byte* output, byte* scratchpad, uint mode)
        //{
        //    uint bufptr_a = 0, bufptr_b = 0, bufptr_c = 0, bufptr_d = 0;
        //    uint output_len, i, j;
        //    uint* T;
        //    byte* Aa, Ab, Ac, Ad;
        //    byte* Ba, Bb, Bc, Bd;

        //    T = (uint*)&scratchpad[0];
        //    Aa = (byte*)&T[176];
        //    Ab = (byte*)&T[256];
        //    Ac = (byte*)&T[336];
        //    Ad = (byte*)&T[416];
        //    Ba = (byte*)&T[496];
        //    Bb = (byte*)&T[568];
        //    Bc = (byte*)&T[640];
        //    Bd = (byte*)&T[712];

        //    neoscrypt_copy(&Aa[0], &password[0], 80);
        //    neoscrypt_copy(&Aa[80], &password[0], 80);
        //    neoscrypt_copy(&Aa[160], &password[0], 80);
        //    neoscrypt_copy(&Aa[240], &password[0], 16);
        //    neoscrypt_copy(&Aa[256], &password[0], 64);
        //    neoscrypt_copy(&Ab[0], &password[80], 80);
        //    neoscrypt_copy(&Ab[80], &password[80], 80);
        //    neoscrypt_copy(&Ab[160], &password[80], 80);
        //    neoscrypt_copy(&Ab[240], &password[80], 16);
        //    neoscrypt_copy(&Ab[256], &password[80], 64);
        //    neoscrypt_copy(&Ac[0], &password[160], 80);
        //    neoscrypt_copy(&Ac[80], &password[160], 80);
        //    neoscrypt_copy(&Ac[160], &password[160], 80);
        //    neoscrypt_copy(&Ac[240], &password[160], 16);
        //    neoscrypt_copy(&Ac[256], &password[160], 64);
        //    neoscrypt_copy(&Ad[0], &password[240], 80);
        //    neoscrypt_copy(&Ad[80], &password[240], 80);
        //    neoscrypt_copy(&Ad[160], &password[240], 80);
        //    neoscrypt_copy(&Ad[240], &password[240], 16);
        //    neoscrypt_copy(&Ad[256], &password[240], 64);

        //    if (mode != 0) {
        //        output_len = 256;
        //        neoscrypt_copy(&Ba[0], &salt[0], 80);
        //        neoscrypt_copy(&Ba[80], &salt[0], 80);
        //        neoscrypt_copy(&Ba[160], &salt[0], 80);
        //        neoscrypt_copy(&Ba[240], &salt[0], 16);
        //        neoscrypt_copy(&Ba[256], &salt[0], 32);
        //        neoscrypt_copy(&Bb[0], &salt[80], 80);
        //        neoscrypt_copy(&Bb[80], &salt[80], 80);
        //        neoscrypt_copy(&Bb[160], &salt[80], 80);
        //        neoscrypt_copy(&Bb[240], &salt[80], 16);
        //        neoscrypt_copy(&Bb[256], &salt[80], 32);
        //        neoscrypt_copy(&Bc[0], &salt[160], 80);
        //        neoscrypt_copy(&Bc[80], &salt[160], 80);
        //        neoscrypt_copy(&Bc[160], &salt[160], 80);
        //        neoscrypt_copy(&Bc[240], &salt[160], 16);
        //        neoscrypt_copy(&Bc[256], &salt[160], 32);
        //        neoscrypt_copy(&Bd[0], &salt[240], 80);
        //        neoscrypt_copy(&Bd[80], &salt[240], 80);
        //        neoscrypt_copy(&Bd[160], &salt[240], 80);
        //        neoscrypt_copy(&Bd[240], &salt[240], 16);
        //        neoscrypt_copy(&Bd[256], &salt[240], 32);
        //    } else {
        //        output_len = 32;
        //        neoscrypt_copy(&Ba[0], &salt[0], 256);
        //        neoscrypt_copy(&Ba[256], &salt[0], 32);
        //        neoscrypt_copy(&Bb[0], &salt[256], 256);
        //        neoscrypt_copy(&Bb[256], &salt[256], 32);
        //        neoscrypt_copy(&Bc[0], &salt[512], 256);
        //        neoscrypt_copy(&Bc[256], &salt[512], 32);
        //        neoscrypt_copy(&Bd[0], &salt[768], 256);
        //        neoscrypt_copy(&Bd[256], &salt[768], 32);
        //    }

        //    for (i = 0; i < 32; i++) {

        //        /* BLAKE2s: initialise */
        //        fixed (uint* blake2s_IV_P_XOR_4way_p = blake2s_IV_P_XOR_4way) {
        //            neoscrypt_copy(&T[0], blake2s_IV_P_XOR_4way_p, 128);
        //            neoscrypt_erase(&T[32], 64);

        //            /* BLAKE2s: update keys */
        //            for (j = 0; j < 32; j += 8) {
        //                T[j + 48] = *((uint*)&Ba[bufptr_a + j]);
        //                T[j + 49] = *((uint*)&Bb[bufptr_b + j]);
        //                T[j + 50] = *((uint*)&Bc[bufptr_c + j]);
        //                T[j + 51] = *((uint*)&Bd[bufptr_d + j]);
        //                T[j + 52] = *((uint*)&Ba[bufptr_a + j + 4]);
        //                T[j + 53] = *((uint*)&Bb[bufptr_b + j + 4]);
        //                T[j + 54] = *((uint*)&Bc[bufptr_c + j + 4]);
        //                T[j + 55] = *((uint*)&Bd[bufptr_d + j + 4]);
        //            }
        //            neoscrypt_erase(&T[80], 128);

        //            /* BLAKE2s: compress IVs using keys */
        //            T[32] = 64;
        //            T[33] = 64;
        //            T[34] = 64;
        //            T[35] = 64;
        //            blake2s_compress_4way(&T[0]);

        //            /* BLAKE2s: update inputs */
        //            for (j = 0; j < 64; j += 8) {
        //                T[j + 48] = *((uint*)&Aa[bufptr_a + j]);
        //                T[j + 49] = *((uint*)&Ab[bufptr_b + j]);
        //                T[j + 50] = *((uint*)&Ac[bufptr_c + j]);
        //                T[j + 51] = *((uint*)&Ad[bufptr_d + j]);
        //                T[j + 52] = *((uint*)&Aa[bufptr_a + j + 4]);
        //                T[j + 53] = *((uint*)&Ab[bufptr_b + j + 4]);
        //                T[j + 54] = *((uint*)&Ac[bufptr_c + j + 4]);
        //                T[j + 55] = *((uint*)&Ad[bufptr_d + j + 4]);
        //            }

        //            /* BLAKE2s: compress using inputs */
        //            T[32] = 128;
        //            T[33] = 128;
        //            T[34] = 128;
        //            T[35] = 128;
        //            T[40] = ~0U;
        //            T[41] = ~0U;
        //            T[42] = ~0U;
        //            T[43] = ~0U;
        //            blake2s_compress_4way(&T[0]);

        //            bufptr_a = 0;
        //            bufptr_b = 0;
        //            bufptr_c = 0;
        //            bufptr_d = 0;
        //            for (j = 0; j < 32; j += 4) {
        //                bufptr_a += T[j];
        //                bufptr_a += (T[j] >> 8);
        //                bufptr_a += (T[j] >> 16);
        //                bufptr_a += (T[j] >> 24);
        //                bufptr_b += T[j + 1];
        //                bufptr_b += (T[j + 1] >> 8);
        //                bufptr_b += (T[j + 1] >> 16);
        //                bufptr_b += (T[j + 1] >> 24);
        //                bufptr_c += T[j + 2];
        //                bufptr_c += (T[j + 2] >> 8);
        //                bufptr_c += (T[j + 2] >> 16);
        //                bufptr_c += (T[j + 2] >> 24);
        //                bufptr_d += T[j + 3];
        //                bufptr_d += (T[j + 3] >> 8);
        //                bufptr_d += (T[j + 3] >> 16);
        //                bufptr_d += (T[j + 3] >> 24);
        //            }
        //            bufptr_a &= 0xFF;
        //            bufptr_b &= 0xFF;
        //            bufptr_c &= 0xFF;
        //            bufptr_d &= 0xFF;

        //            for (j = 0; j < 32; j += 8) {
        //                *((uint*)&Ba[bufptr_a + j]) ^= T[j];
        //                *((uint*)&Bb[bufptr_b + j]) ^= T[j + 1];
        //                *((uint*)&Bc[bufptr_c + j]) ^= T[j + 2];
        //                *((uint*)&Bd[bufptr_d + j]) ^= T[j + 3];
        //                *((uint*)&Ba[bufptr_a + j + 4]) ^= T[j + 4];
        //                *((uint*)&Bb[bufptr_b + j + 4]) ^= T[j + 5];
        //                *((uint*)&Bc[bufptr_c + j + 4]) ^= T[j + 6];
        //                *((uint*)&Bd[bufptr_d + j + 4]) ^= T[j + 7];
        //            }

        //            if (bufptr_a < 32)
        //                neoscrypt_copy(&Ba[256 + bufptr_a], &Ba[bufptr_a], 32 - bufptr_a);
        //            else if (bufptr_a > 224)
        //                neoscrypt_copy(&Ba[0], &Ba[256], bufptr_a - 224);
        //            if (bufptr_b < 32)
        //                neoscrypt_copy(&Bb[256 + bufptr_b], &Bb[bufptr_b], 32 - bufptr_b);
        //            else if (bufptr_b > 224)
        //                neoscrypt_copy(&Bb[0], &Bb[256], bufptr_b - 224);
        //            if (bufptr_c < 32)
        //                neoscrypt_copy(&Bc[256 + bufptr_c], &Bc[bufptr_c], 32 - bufptr_c);
        //            else if (bufptr_c > 224)
        //                neoscrypt_copy(&Bc[0], &Bc[256], bufptr_c - 224);
        //            if (bufptr_d < 32)
        //                neoscrypt_copy(&Bd[256 + bufptr_d], &Bd[bufptr_d], 32 - bufptr_d);
        //            else if (bufptr_d > 224)
        //                neoscrypt_copy(&Bd[0], &Bd[256], bufptr_d - 224);
        //        }
        //    }

        //    i = 256 - bufptr_a;
        //    if (i >= output_len) {
        //        neoscrypt_xor(&Ba[bufptr_a], &Aa[0], output_len);
        //        neoscrypt_copy(&output[0], &Ba[bufptr_a], output_len);
        //    } else {
        //        neoscrypt_xor(&Ba[bufptr_a], &Aa[0], i);
        //        neoscrypt_xor(&Ba[0], &Aa[i], output_len - i);
        //        neoscrypt_copy(&output[0], &Ba[bufptr_a], i);
        //        neoscrypt_copy(&output[i], &Ba[0], output_len - i);
        //    }
        //    i = 256 - bufptr_b;
        //    if (i >= output_len) {
        //        neoscrypt_xor(&Bb[bufptr_b], &Ab[0], output_len);
        //        neoscrypt_copy(&output[output_len], &Bb[bufptr_b], output_len);
        //    } else {
        //        neoscrypt_xor(&Bb[bufptr_b], &Ab[0], i);
        //        neoscrypt_xor(&Bb[0], &Ab[i], output_len - i);
        //        neoscrypt_copy(&output[output_len], &Bb[bufptr_b], i);
        //        neoscrypt_copy(&output[output_len + i], &Bb[0], output_len - i);
        //    }
        //    i = 256 - bufptr_c;
        //    if (i >= output_len) {
        //        neoscrypt_xor(&Bc[bufptr_c], &Ac[0], output_len);
        //        neoscrypt_copy(&output[2 * output_len], &Bc[bufptr_c], output_len);
        //    } else {
        //        neoscrypt_xor(&Bc[bufptr_c], &Ac[0], i);
        //        neoscrypt_xor(&Bc[0], &Ac[i], output_len - i);
        //        neoscrypt_copy(&output[2 * output_len], &Bc[bufptr_c], i);
        //        neoscrypt_copy(&output[2 * output_len + i], &Bc[0], output_len - i);
        //    }
        //    i = 256 - bufptr_d;
        //    if (i >= output_len) {
        //        neoscrypt_xor(&Bd[bufptr_d], &Ad[0], output_len);
        //        neoscrypt_copy(&output[3 * output_len], &Bd[bufptr_d], output_len);
        //    } else {
        //        neoscrypt_xor(&Bd[bufptr_d], &Ad[0], i);
        //        neoscrypt_xor(&Bd[0], &Ad[i], output_len - i);
        //        neoscrypt_copy(&output[3 * output_len], &Bd[bufptr_d], i);
        //        neoscrypt_copy(&output[3 * output_len + i], &Bd[0], output_len - i);
        //    }
        //}

        static uint cpu_vec_exts()
        {
            /* No assembly, no extensions */
            return (0);
        }

        //static void neoscrypt_blkcpy(void* dstp, void* srcp, uint len)
        //{
        //    uint* dst = (uint*)dstp;
        //    uint* src = (uint*)srcp;
        //    uint i;

        //    for (i = 0; i < (len / sizeof(uint)); i += 4) {
        //        dst[i] = src[i];
        //        dst[i + 1] = src[i + 1];
        //        dst[i + 2] = src[i + 2];
        //        dst[i + 3] = src[i + 3];
        //    }
        //}

        //static void neoscrypt_blkswp(void* blkAp, void* blkBp, uint len)
        //{
        //    uint* blkA = (uint*)blkAp;
        //    uint* blkB = (uint*)blkBp;
        //    uint t0, t1, t2, t3;
        //    uint i;

        //    for (i = 0; i < (len / sizeof(uint)); i += 4) {
        //        t0 = blkA[i];
        //        t1 = blkA[i + 1];
        //        t2 = blkA[i + 2];
        //        t3 = blkA[i + 3];
        //        blkA[i] = blkB[i];
        //        blkA[i + 1] = blkB[i + 1];
        //        blkA[i + 2] = blkB[i + 2];
        //        blkA[i + 3] = blkB[i + 3];
        //        blkB[i] = t0;
        //        blkB[i + 1] = t1;
        //        blkB[i + 2] = t2;
        //        blkB[i + 3] = t3;
        //    }
        //}

        //static void neoscrypt_blkxor(void* dstp, void* srcp, uint len)
        //{
        //    uint* dst = (uint*)dstp;
        //    uint* src = (uint*)srcp;
        //    uint i;

        //    for (i = 0; i < (len / sizeof(uint)); i += 4) {
        //        dst[i] ^= src[i];
        //        dst[i + 1] ^= src[i + 1];
        //        dst[i + 2] ^= src[i + 2];
        //        dst[i + 3] ^= src[i + 3];
        //    }
        //}

        static void neoscrypt_pack_4way(void* dstp, void* srcp, uint len)
        {
            uint* dst = (uint*)dstp;
            uint* src = (uint*)srcp;
            uint i, j;

            len >>= 4;

            for (i = 0, j = 0; j < len; i += 4, j++) {
                dst[i] = src[j];
                dst[i + 1] = src[j + len];
                dst[i + 2] = src[j + 2 * len];
                dst[i + 3] = src[j + 3 * len];
            }
        }

        static void neoscrypt_unpack_4way(void* dstp, void* srcp, uint len)
        {
            uint* dst = (uint*)dstp;
            uint* src = (uint*)srcp;
            uint i, j;

            len >>= 4;

            for (i = 0, j = 0; j < len; i += 4, j++) {
                dst[j] = src[i];
                dst[j + len] = src[i + 1];
                dst[j + 2 * len] = src[i + 2];
                dst[j + 3 * len] = src[i + 3];
            }
        }

        static void neoscrypt_xor_4way(void* dstp, void* srcAp, void* srcBp, void* srcCp, void* srcDp, uint len)
        {
            uint* dst = (uint*)dstp;
            uint* srcA = (uint*)srcAp;
            uint* srcB = (uint*)srcBp;
            uint* srcC = (uint*)srcCp;
            uint* srcD = (uint*)srcDp;
            uint i;

            for (i = 0; i < (len >> 2); i += 4) {
                dst[i] ^= srcA[i];
                dst[i + 1] ^= srcB[i + 1];
                dst[i + 2] ^= srcC[i + 2];
                dst[i + 3] ^= srcD[i + 3];
            }
        }

        #endregion

        #endregion
    }
}

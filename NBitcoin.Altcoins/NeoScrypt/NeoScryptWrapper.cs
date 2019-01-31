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
using System.Text;

namespace NeoScrypt
{
    public class NeoScryptWrapper
    {
        public static void neoscrypt(string password, ref byte[] output, uint profile)
        {
            NeoScrypt.neoscrypt(Encoding.ASCII.GetBytes(password), ref output, profile);
        }

        public static void neoscrypt_fastkdf(string password, uint password_len, string salt, uint salt_len, uint N, ref byte[] output, uint output_len)
        {
            NeoScrypt.neoscrypt_fastkdf(Encoding.ASCII.GetBytes(password), password_len, Encoding.ASCII.GetBytes(salt), salt_len, N, ref output, output_len);
        }

        public static void neoscrypt_blake2s(string input, uint input_size, string key, byte key_size, ref byte[] output, byte output_size)
        {
            NeoScrypt.neoscrypt_blake2s(Encoding.ASCII.GetBytes(input), input_size, Encoding.ASCII.GetBytes(key), key_size, ref output, output_size);
        }

        public static void neoscrypt_fastkdf(string password, uint password_len, byte[] salt, uint salt_len, uint N, ref byte[] output, uint output_len)
        {
            NeoScrypt.neoscrypt_fastkdf(Encoding.ASCII.GetBytes(password), password_len, salt, salt_len, N, ref output, output_len);
        }

        public static void neoscrypt_blake2s(string input, uint input_size, byte[] key, byte key_size, ref byte[] output, byte output_size)
        {
            NeoScrypt.neoscrypt_blake2s(Encoding.ASCII.GetBytes(input), input_size, key, key_size, ref output, output_size);
        }

    }
}
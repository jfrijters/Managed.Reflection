/*
  The MIT License (MIT) 
  Copyright (C) 2009-2012 Jeroen Frijters
  
  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:
  
  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.
  
  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  SOFTWARE.
*/
using System;
using System.IO;
using System.Security.Cryptography;

namespace Managed.Reflection
{
    public sealed class StrongNameKeyPair
    {
        private readonly byte[] keyPairArray;
        private readonly string keyPairContainer;

        public StrongNameKeyPair(string keyPairContainer)
        {
            if (keyPairContainer == null)
            {
                throw new ArgumentNullException("keyPairContainer");
            }
            this.keyPairContainer = keyPairContainer;
        }

        public StrongNameKeyPair(byte[] keyPairArray)
        {
            if (keyPairArray == null)
            {
                throw new ArgumentNullException("keyPairArray");
            }
            this.keyPairArray = (byte[])keyPairArray.Clone();
        }

        public StrongNameKeyPair(FileStream keyPairFile)
            : this(ReadAllBytes(keyPairFile))
        {
        }

        private static byte[] ReadAllBytes(FileStream keyPairFile)
        {
            if (keyPairFile == null)
            {
                throw new ArgumentNullException("keyPairFile");
            }
            byte[] buf = new byte[keyPairFile.Length - keyPairFile.Position];
            keyPairFile.Read(buf, 0, buf.Length);
            return buf;
        }

        public byte[] PublicKey
        {
            get
            {
                using (RSACryptoServiceProvider rsa = CreateRSA())
                {
                    byte[] cspBlob = rsa.ExportCspBlob(false);
                    byte[] publicKey = new byte[12 + cspBlob.Length];
                    Buffer.BlockCopy(cspBlob, 0, publicKey, 12, cspBlob.Length);
                    publicKey[1] = 36;
                    publicKey[4] = 4;
                    publicKey[5] = 128;
                    publicKey[8] = (byte)(cspBlob.Length >> 0);
                    publicKey[9] = (byte)(cspBlob.Length >> 8);
                    publicKey[10] = (byte)(cspBlob.Length >> 16);
                    publicKey[11] = (byte)(cspBlob.Length >> 24);
                    return publicKey;
                }
            }
        }

        internal RSACryptoServiceProvider CreateRSA()
        {
            try
            {
                if (keyPairArray != null)
                {
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.ImportCspBlob(keyPairArray);
                    return rsa;
                }
                else
                {
                    CspParameters parm = new CspParameters();
                    parm.KeyContainerName = keyPairContainer;
                    // MONOBUG Mono doesn't like it when Flags or KeyNumber are set
                    if (!Universe.MonoRuntime)
                    {
                        parm.Flags = CspProviderFlags.UseMachineKeyStore | CspProviderFlags.UseExistingKey;
                        parm.KeyNumber = 2; // Signature
                    }
                    return new RSACryptoServiceProvider(parm);
                }
            }
            catch
            {
                throw new ArgumentException("Unable to obtain public key for StrongNameKeyPair.");
            }
        }
    }
}

using System;
using System.Diagnostics;

namespace Noise
{
	/// <summary>
	/// A CipherState can encrypt and decrypt data based on its variables k
	/// (a cipher key of 32 bytes) and n (an 8-byte unsigned integer nonce).
	/// </summary>
	internal sealed class CipherState<CipherType> : IDisposable where CipherType : Cipher, new()
	{
		private const ulong MaxNonce = UInt64.MaxValue;

		private static readonly byte[] zeroLen = new byte[0];
		private static readonly byte[] zeros = new byte[32];

		private readonly CipherType cipher = new CipherType();
		private byte[] k;
		private ulong n;
		private bool disposed;

		/// <summary>
		/// Sets k = key. Sets n = 0.
		/// </summary>
		public void InitializeKey(byte[] key)
		{
			Debug.Assert(key == null || key.Length == Constants.KeySize);

			if (k != null)
			{
				Array.Clear(k, 0, k.Length);
			}

			k = key;
			n = 0;
		}

		/// <summary>
		/// Returns true if k is non-empty, false otherwise.
		/// </summary>
		public bool HasKey()
		{
			return k != null;
		}

		/// <summary>
		/// Sets n = nonce. This function is used for handling out-of-order transport messages.
		/// </summary>
		public void SetNonce(ulong nonce)
		{
			n = nonce;
		}

		/// <summary>
		/// If k is non-empty returns Encrypt(k, n++, ad, plaintext, ciphertext).
		/// Otherwise copies the plaintext to the ciphertext variable and returns
		/// the length of the plaintext.
		/// </summary>
		public int EncryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> ciphertext)
		{
			if (n == MaxNonce)
			{
				throw new OverflowException("Nonce has reached its maximum value.");
			}

			if (k == null)
			{
				plaintext.CopyTo(ciphertext);
				return plaintext.Length;
			}

			return cipher.Encrypt(k, n++, ad, plaintext, ciphertext);
		}

		/// <summary>
		/// If k is non-empty returns Decrypt(k, n++, ad, ciphertext, plaintext).
		/// Otherwise copies the ciphertext to the plaintext variable and returns
		/// the length of the ciphertext. If an authentication failure occurs in
		/// Decrypt() then n is not incremented and an error is signaled to the caller.
		/// </summary>
		public int DecryptWithAd(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> ciphertext, Span<byte> plaintext)
		{
			if (n == MaxNonce)
			{
				throw new OverflowException("Nonce has reached its maximum value.");
			}

			if (k == null)
			{
				ciphertext.CopyTo(plaintext);
				return ciphertext.Length;
			}

			int bytesRead = cipher.Decrypt(k, n, ad, ciphertext, plaintext);
			++n;

			return bytesRead;
		}

		/// <summary>
		/// Sets k = REKEY(k).
		/// </summary>
		public void Rekey()
		{
			k = k ?? new byte[Constants.KeySize];
			cipher.Encrypt(k, MaxNonce, zeroLen, zeros, k);
		}

		public void Dispose()
		{
			if (!disposed)
			{
				InitializeKey(null);
				disposed = true;
			}
		}
	}
}

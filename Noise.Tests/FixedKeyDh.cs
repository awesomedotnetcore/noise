namespace Noise.Tests
{
	internal class FixedKeyDh : Dh
	{
		private static readonly Curve25519 dh = new Curve25519();
		private readonly byte[] privateKey;

		public FixedKeyDh()
		{
		}

		public FixedKeyDh(byte[] privateKey)
		{
			this.privateKey = privateKey;
		}

		public int DhLen => dh.DhLen;

		public KeyPair GenerateKeyPair()
		{
			var publicKey = new byte[DhLen];
			Libsodium.crypto_scalarmult_curve25519_base(publicKey, privateKey);

			return new KeyPair(privateKey, publicKey);
		}

		public byte[] Dh(KeyPair keyPair, byte[] publicKey)
		{
			return dh.Dh(keyPair, publicKey);
		}
	}
}
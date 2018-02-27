using System;
using System.Linq;

namespace Lying_NTP
{
	public class NtpFrame
	{
		private readonly byte[] data;

		public NtpFrame(byte[] data)
		{
			var reversedData = data.ToArray();
			for (var i = 0; i < data.Length; i += 4)
				reversedData = Helper.SwapEndianness(reversedData, i);
			this.data = reversedData;
		}

		public byte[] Bytes
		{
			get
			{
				var reversedData = data.ToArray();
				for (var i = 0; i < data.Length; i += 4)
					reversedData = Helper.SwapEndianness(reversedData, i);
				return reversedData;
			}
		}

		public void FillFrame(DateTime lastUpdate, byte[] referenceTimestamp, byte[] currentTime)
		{
			FillHeaders();
			CopyTimestamps(referenceTimestamp, currentTime);
		}

		private void CopyTimestamps(byte[] referenceTimestamp, byte[] currentTime)
		{
			var transitTimestamp = data.Skip(NtpFields.TransmitTimestamp).Take(4).ToArray();
			transitTimestamp.CopyTo(data, NtpFields.OriginateTimestamp);

			var ipBytes = Helper.SwapEndianness(BitConverter.GetBytes(NtpValues.ReferenceId));
			ipBytes.CopyTo(data, NtpFields.ReferenceId);

			referenceTimestamp.CopyTo(data, NtpFields.ReferenceTimestamp);

			currentTime.CopyTo(data, NtpFields.ReceiveTimestamp);
			currentTime.CopyTo(data, NtpFields.TransmitTimestamp);
		}

		private void FillHeaders()
		{
			var version = data[0] & NtpValues.VersionMask; //строка вида 00_ХХХ_000
			data[NtpFields.Header] = (byte) (NtpValues.LeapAndModeMask | version);
			data[NtpFields.Stratum] = NtpValues.Stratum;
			data[NtpFields.PollInterval] = NtpValues.PollInterval;
			data[NtpFields.Precision] = unchecked((byte) NtpValues.Precision);
			data[NtpFields.RootDelay] = NtpValues.RootDelay;
			data[NtpFields.RootDispersion] = NtpValues.RootDispersion;
		}
	}
}
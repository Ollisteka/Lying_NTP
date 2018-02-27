namespace Lying_NTP
{
	public class Helper
	{
		public static byte[] SwapEndianness(byte[] data, int offset = 0)
		{
			var tmp = data[offset];
			data[offset] = data[offset + 3];
			data[offset + 3] = tmp;

			tmp = data[offset + 1];
			data[offset + 1] = data[offset + 2];
			data[offset + 2] = tmp;

			return data;
		}
	}
}
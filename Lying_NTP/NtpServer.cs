using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lying_NTP
{
	public class NtpServer
	{
		private const string ServerAddress = "time.windows.com";
		public const int Port = 123;
		private readonly uint fault;

		public NtpServer(uint fault = 0)
		{
			this.fault = fault;
		}

		public static byte[] GetDataFromServer(IPAddress serverAddress=null)
		{
			if (serverAddress == null)
			{
				var addresses = Dns.GetHostEntry(ServerAddress).AddressList;
				serverAddress = addresses[0];
			}
			var ntpData = new byte[48];
			/* Leap Indicator(LI) = 0 (no warning) 2 bit  (missing second)
			 Version Number (VN) NTP/SNTP version number = 4 3 bit
			 Mode = 3 (client) 3 bit */
			ntpData[0] = 0b00100011; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

			var ipEndPoint = new IPEndPoint(serverAddress, Port);

			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.Connect(ipEndPoint);

				socket.ReceiveTimeout = 3000;

				socket.Send(ntpData);
				socket.Receive(ntpData);
				socket.Close();
			}
			return ntpData;
		}

		public static DateTime FromBytesToDateTime(byte[] data, int pos)
		{
			ulong intpart = 0, fractpart = 0;

			for (int i = 0; i <= 3; i++)
			{
				intpart = 256 * intpart + data[pos + i];
			}
			for (int i = 4; i <= 7; i++)
			{
				fractpart = 256 * fractpart + data[pos + i];
			}
			ulong milliseconds = intpart * 1000 + (fractpart * 1000) / 0x100000000L;
			TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);
			DateTime time = new DateTime(1900, 1, 1);
			time += span;
			return time;
		}

		private static uint GetTimestamp(byte[] data, int startIndex)
		{
			//Seconds
			var intPart = BitConverter.ToUInt32(data, startIndex);

			//Seconds fraction (random, so I won't bother) 
			// ulong fractPart = BitConverter.ToUInt32(data, startIndex + 4);

			//Convert From big-endian to little-endian
			intPart = SwapEndianness(intPart);
			//fractPart = SwapEndianness(fractPart);

			return intPart;
		}
		// Compute the 8-byte array, given the date
		private byte[] SetDate(byte[] SNTPData, int offset, DateTime date)
		{
			ulong intpart = 0, fractpart = 0;
			DateTime StartOfCentury = new DateTime(1900, 1, 1, 0, 0, 0);    // January 1, 1900 12:00 AM

			ulong milliseconds = (ulong)(date - StartOfCentury).TotalMilliseconds;
			intpart = milliseconds / 1000;
			fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000;
			Console.WriteLine(StartOfCentury.AddSeconds(intpart));
			ulong temp = intpart;
			for (int i = 3; i >= 0; i--)
			{
				SNTPData[offset + i] = (byte)(temp % 256);
				temp = temp / 256;
			}

			temp = fractpart;
			for (int i = 7; i >= 4; i--)
			{
				SNTPData[offset + i] = (byte)(temp % 256);
				temp = temp / 256;
			}
			return SNTPData;
		}

		private byte[] GetFaultedTime(byte[] data)
		{
			//var answer = GetDataFromServer();

			/* Leap Indicator(LI) = 0 (no warning) 2 bit  (missing second)
			 Version Number (VN) NTP/SNTP version number = 4 3 bit
			 Mode = 3 (client) 3 bit */
			data[0] = 0b00100100; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 4 (Server Mode)
			data[1] = 1;
			//var date = DateTime.Now;
			data = SetDate(data, 24, DateTime.Now.AddSeconds(fault));
			
			data = SetDate(data, 32, DateTime.Now.AddSeconds(fault));
//			var seconds = (uint) DateTime.Now.Subtract(new DateTime(1900, 1, 1, 0, 0, 0)).TotalSeconds;
//			var recieveTime =  seconds + fault;
//
//			recieveTime = SwapEndianness(recieveTime);
//
//			var bytes = BitConverter.GetBytes(recieveTime);
//			
//			for (var i = 0; i < bytes.Length; i++)
//			{
//			//	data[16 + i] = bytes[i];
//				data[24 + i] = bytes[i];
//
//				data[32 + i] = bytes[i];
//			}
			Console.WriteLine(FromBytesToDateTime(data, 32));
			return data;
		}

		internal static uint SwapEndianness(uint x)
		{
			return ((x & 0x000000ff) << 24) +
							((x & 0x0000ff00) << 8) +
							((x & 0x00ff0000) >> 8) +
							((x & 0xff000000) >> 24);
		}

		private static bool PacketCorrect(byte[] data, out string error)
		{
			if (data.Length != 48)
			{
				error = $"The message's length should be 48 bytes, but was: {data.Length}";
				return false;
			}
			var mode = data[0] & 0b00000111;
			if (mode != 3)
			{
				error = Encoding.ASCII.GetString(data, 0, data.Length);
				return false;
			}
			error = "OK";
			return true;
		}
		public void Run()
		{
			var ipep = new IPEndPoint(IPAddress.Loopback, Port);
			var newsock = new UdpClient(ipep);
			while (true)
			{
				var sender = new IPEndPoint(IPAddress.Any, 0);
				Console.WriteLine("Waiting for a client...");
				var data = newsock.Receive(ref sender);

				if (!PacketCorrect(data, out var error))
				{
					PrintError($"Message received from {sender} was incorrect:", error);
					continue;
				}
				Console.WriteLine("Message received from {0}, sending response...\n", sender);
				data = GetFaultedTime(data);
				//data = Encoding.ASCII.GetBytes(GetTime().ToString(CultureInfo.InvariantCulture));
				newsock.Send(data, data.Length, sender);
			}
		}

		private void PrintError(string s, string error)
		{
			Console.WriteLine(s);
			Console.WriteLine(error);
		}
	}
}
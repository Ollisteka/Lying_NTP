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

		public static DateTime FromBytesToDateTime(byte[] data)
		{
			var transmitTimestamp = GetTimestamp(data, 40);
			var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0).AddSeconds((long) transmitTimestamp);
			return networkDateTime.ToLocalTime();
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

		private byte[] GetFaultedTime()
		{
			var answer = GetDataFromServer();
			var seconds = GetTimestamp(answer, 40);
			seconds += fault;
			seconds = SwapEndianness(seconds);
			var bytes = BitConverter.GetBytes(seconds);
			for (var i = 0; i < bytes.Length; i++)
				answer[40 + i] = bytes[i];
			return answer;
		}

		internal static uint SwapEndianness(uint x)
		{
			var e = Convert.ToString(x, 16);
			var a = Convert.ToString((x & 0x000000ff) << 24, 16);
			var b = Convert.ToString((x & 0x0000ff00) << 8, 16);
			var c = Convert.ToString((x & 0x00ff0000) >> 8, 16);
			var d = Convert.ToString((x & 0xff000000) >> 24, 16);
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
				data = GetFaultedTime();
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
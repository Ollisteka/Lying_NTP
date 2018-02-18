using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lying_NTP
{
	internal class NtpServer
	{
		private readonly int offset;

		public NtpServer(int offset=0)
		{
			this.offset = offset;
		}

		public int Port => 123;
		private string serverAddress => "time.windows.com";

		private DateTime AskServer(string serverName)
		{
			var ntpData = new byte[48];
			/* Leap Indicator(LI) = 0 (no warning) 2 bit  (missing second)
			 Version Number (VN) NTP/SNTP version number = 4 3 bit
			 Mode = 3 (client) 3 bit */
			ntpData[0] = 0b00100011; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

			var addresses = Dns.GetHostEntry(serverName).AddressList;

			var ipEndPoint = new IPEndPoint(addresses[0], Port);

			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.Connect(ipEndPoint);

				socket.ReceiveTimeout = 3000;

				socket.Send(ntpData);
				socket.Receive(ntpData);
				socket.Close();
			}

			var transmitTimestamp = GetTimestamp(ntpData, 40);

			var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0).AddSeconds((long) transmitTimestamp);
			return networkDateTime.ToLocalTime();
		}

		public ulong GetTimestamp(byte[] data, int startIndex)
		{
			//Seconds
			ulong intPart = BitConverter.ToUInt32(data, startIndex);

			//Seconds fraction (random, so I won't bother) 
			// ulong fractPart = BitConverter.ToUInt32(data, startIndex + 4);

			//Convert From big-endian to little-endian
			intPart = SwapEndianness(intPart);

			return intPart;
		}

		public DateTime GetCurrentTime()
		{
			var date = AskServer(serverAddress);
			return date.AddSeconds(offset);
		}

		private static uint SwapEndianness(ulong x)
		{
			return (uint) (((x & 0x000000ff) << 24) |
							((x & 0x0000ff00) << 8) |
							((x & 0x00ff0000) >> 8) |
							((x & 0xff000000) >> 24));
		}

		private static int SwapEndianness(byte val)
		{
			return ((val & 0xF) << 4)
					| ((val >> 4) & 0xF);
		}

		public void Run()
		{
			var data = new byte[1024];
			var ipep = new IPEndPoint(IPAddress.Loopback, Port);
			var newsock = new UdpClient(ipep);
			while (true)
			{
				var sender = new IPEndPoint(IPAddress.Any, 0);
				Console.WriteLine("Waiting for a client...");
				data = newsock.Receive(ref sender);
				var mode = data[0] & 0b00000111;
				if (mode == 3)
				{
					Console.WriteLine("Message received from {0}, sending response...\n", sender);
					data = Encoding.ASCII.GetBytes(GetCurrentTime().ToString(CultureInfo.InvariantCulture));
					newsock.Send(data, data.Length, sender);
				}
				else
				{
					Console.WriteLine("Message received from {0} was incorrect:", sender);
					Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
					Console.WriteLine();
				}
			}
		}
	}
}
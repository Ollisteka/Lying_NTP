using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lying_NTP
{
	class NtpClient
	{
		public const int NtpPort = 123;

		public static DateTime GetNetworkTime()
		{
			//default Windows time server
			const string ntpServer = "time.windows.com";

			// NTP message size - 16 bytes of the digest (RFC 2030)
			var ntpData = new byte[48];

			//Setting the Leap Indicator, Version Number and Mode values
			ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

			var addresses = Dns.GetHostEntry(ntpServer).AddressList;

			//The UDP port number assigned to NTP is 123
			var ipEndPoint = new IPEndPoint(addresses[0], NtpPort);
			//NTP uses UDP

			using (var socket = new Socket(AddressFamily.InterNetwork,
				SocketType.Dgram,
				ProtocolType.Udp))
			{
				socket.Connect(ipEndPoint);

				//Stops code hang if NTP is blocked
				socket.ReceiveTimeout = 3000;

				socket.Send(ntpData);
				socket.Receive(ntpData);
				socket.Close();
			}

			//Offset to get to the "Transmit Timestamp" field (time at which the reply 
			//departed the server for the client, in 64-bit timestamp format."
			const byte serverReplyTime = 40;

			//Get the seconds part
			ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

			//Get the seconds fraction
			ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

			//Convert From big-endian to little-endian
			intPart = SwapEndianness(intPart);
			fractPart = SwapEndianness(fractPart);

			var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

			//**UTC** time
			var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long) milliseconds);

			return networkDateTime.ToLocalTime();
		}

		// stackoverflow.com/a/3294698/162671

		private static uint SwapEndianness(ulong x)
		{
			return (uint) (((x & 0x000000ff) << 24) +
							((x & 0x0000ff00) << 8) +
							((x & 0x00ff0000) >> 8) +
							((x & 0xff000000) >> 24));
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("\nPress ESC to exit");
			Console.WriteLine("Press Enter to learn time");
			byte[] data = new byte[1024];
			IPEndPoint ipep = new IPEndPoint(IPAddress.Loopback, NtpClient.NtpPort);
			UdpClient newsock = new UdpClient(ipep);
			while (true)
			{
				IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

				Console.WriteLine("Waiting for a client...");
				data = newsock.Receive(ref sender);
				Console.WriteLine("Here");
				Console.WriteLine("Message received from {0}:", sender.ToString());
				Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
				string welcome = "Welcome to my test server";
				data = Encoding.ASCII.GetBytes(welcome);
				newsock.Send(data, data.Length, sender);
				data = Encoding.ASCII.GetBytes(NtpClient.GetNetworkTime().ToString());
				newsock.Send(data, data.Length, sender);

				switch (Console.ReadKey(true).Key)
				{
					case ConsoleKey.Escape:
						Environment.Exit(1);
						break;
					case ConsoleKey.Enter:
						Console.WriteLine(NtpClient.GetNetworkTime());
						break;
				}
			}
		}
	}
}
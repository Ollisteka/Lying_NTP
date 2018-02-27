using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lying_NTP
{
	public class NtpFrame
	{
		public const int LeapAndModeMask = 0b00_111_100; //LI = 0, Mode = 4
		public const int VersionMask = 0b00_111_000; // чтобы скопировать версию клиента
		public const int Stratum = 3; //получаем время не от самого точного сервера, а попроще
		public const int PollInterval = 4; //как часто можем опрашивать сервер 2*n
		public const int Precision = -6; //2*n
		public const int RootDelay = 1; //сколько времени в секундах понадобилось, чтобы сходить узнать время
		public const int RootDispersion = 1; //погрешность
		public static readonly int ReferenceId = 0b0111_1111_0000_0000_0000_0000_0000_0001;
	}

	public class NtpServer
	{
		private const string ServerAddress = "time.windows.com";
		public const int Port = 123;
		private readonly int fault;
		private DateTime lastUpdate = DateTime.MinValue;

		public NtpServer(int fault=0)
		{
			this.fault = fault;
		}

		private byte[] ReferenceTimestamp =>
			lastUpdate == DateTime.MinValue
				? new byte[8]
				: BitConverter.GetBytes((uint) lastUpdate.Subtract(new DateTime(1900, 1, 1)).TotalSeconds);

		private byte[] TimeOfDay =>
			BitConverter.GetBytes((uint) DateTime.UtcNow.Subtract(new DateTime(1900, 1, 1)).TotalSeconds + fault);

		public void Run(bool selfServer = true)
		{
			var ipep = new IPEndPoint(IPAddress.Loopback, Port);
			var newsock = new UdpClient(ipep);
			while (true)
			{
				var sender = new IPEndPoint(IPAddress.Any, 0);
				Console.WriteLine("Waiting for a client...");
				var data = newsock.Receive(ref sender);

				Console.WriteLine("Message received from {0}, sending response...\n", sender);
				if (selfServer)
					data = GenerateResponse(data);
				else
				{
					data = ResendToServer(data);
					data = CompromizeData(data);
				}

				newsock.Send(data, data.Length, sender);
			}
		}

		private byte[] GenerateResponse(byte[] data)
		{
			var reversedData = data.ToArray();

			for (var i = 0; i < data.Length; i += 4)
				reversedData = SwapEndianness(reversedData, i);

			reversedData = ChangeHeader(reversedData);

			for (var i = 0; i < data.Length; i += 4)
				reversedData = SwapEndianness(reversedData, i);
			return reversedData;
		}

		private byte[] ChangeHeader(byte[] reversedData)
		{
			var version = reversedData[0] & NtpFrame.VersionMask;
			reversedData[0] = (byte) (NtpFrame.LeapAndModeMask | version);
			reversedData[1] = NtpFrame.Stratum;
			reversedData[2] = NtpFrame.PollInterval;
			reversedData[3] = unchecked((byte)NtpFrame.Precision);
			reversedData[4] = NtpFrame.RootDelay;
			reversedData[8] = NtpFrame.RootDispersion;
			var ipBytes = SwapEndianness(BitConverter.GetBytes(NtpFrame.ReferenceId));
			for (var i = 0; i < 4; i++)
				reversedData[12 + i] = ipBytes[i];

			for (var i = 24; i < 32; i++) //сopy transit to origiante
				reversedData[i] = reversedData[i + 16];

			if (lastUpdate == DateTime.MinValue)
				lastUpdate = DateTime.Now;

			for (var i = 0; i < ReferenceTimestamp.Length; i++) //update reference
				reversedData[16 + i] = ReferenceTimestamp[i];
			var nowTime = TimeOfDay;
			for (var i = 0; i < nowTime.Length; i++)
			{
				reversedData[32 + i] = nowTime[i]; //recieve
				reversedData[40 + i] = nowTime[i]; //transmit
			}
			return reversedData;
		}

		private byte[] CompromizeData(byte[] data)
		{
			var seconds = ExtractTimestamp(data, 32) + fault;
			var reversedResultData = SwapEndianness(BitConverter.GetBytes(seconds).Take(4).ToArray());
			for (var i = 0; i < 4; i++)
			{
				data[32 + i] = reversedResultData[i];
				data[40 + i] = reversedResultData[i];
			}
			return data;
		}

		private static byte[] SwapEndianness(byte[] data, int offset = 0)
		{
			var tmp = data[offset];
			data[offset] = data[offset + 3];
			data[offset + 3] = tmp;

			tmp = data[offset + 1];
			data[offset + 1] = data[offset + 2];
			data[offset + 2] = tmp;

			return data;
		}

		private uint ExtractTimestamp(byte[] data, int offset)
		{
			var subArray = data.Skip(offset).Take(4).ToArray();
			var reversed = SwapEndianness(subArray);
			return BitConverter.ToUInt32(reversed, 0);
		}

		private byte[] ResendToServer(byte[] data)
		{
			var ipAddresses = Dns.GetHostEntry(ServerAddress).AddressList;
			var ipEndPoint = new IPEndPoint(ipAddresses[0], Port);

			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
			{
				socket.Connect(ipEndPoint);

				socket.ReceiveTimeout = 3000;

				socket.Send(data);
				socket.Receive(data);
				socket.Close();
			}

			return data;
		}

		private void PrintError(string s, string error)
		{
			Console.WriteLine(s);
			Console.WriteLine(error);
		}
	}
}
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lying_NTP
{
	public class NtpServer
	{
		private const string ServerAddress = "time.windows.com";
		public const int Port = 123;
		private readonly int fault;
		private DateTime lastUpdate = DateTime.MinValue;

		public NtpServer(int fault = 0)
		{
			this.fault = fault;
		}

		private byte[] ReferenceTimestamp =>
			lastUpdate == DateTime.MinValue
				? new byte[8]
				: BitConverter.GetBytes(GetElapsedSeconds(lastUpdate));

		private byte[] TimeOfDay =>
			BitConverter.GetBytes(GetElapsedSeconds(DateTime.UtcNow) + fault);

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
				{
					data = GenerateResponse(data);
				}
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
			var frame = new NtpFrame(data);
			if (lastUpdate == DateTime.MinValue)
				lastUpdate = DateTime.Now;
			frame.FillFrame(lastUpdate, ReferenceTimestamp, TimeOfDay);
			return frame.Bytes;
		}


		private byte[] CompromizeData(byte[] data)
		{
			var seconds = ExtractTimestamp(data, 32) + fault;
			var reversedResultData = Helper.SwapEndianness(BitConverter.GetBytes(seconds).Take(4).ToArray());
			for (var i = 0; i < 4; i++)
			{
				data[NtpFields.ReceiveTimestamp + i] = reversedResultData[i];
				data[NtpFields.TransmitTimestamp + i] = reversedResultData[i];
			}
			return data;
		}

		private uint ExtractTimestamp(byte[] data, int offset)
		{
			var subArray = data.Skip(offset).Take(4).ToArray();
			var reversed = Helper.SwapEndianness(subArray);
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

		private static uint GetElapsedSeconds(DateTime time)
		{
			return (uint)time.Subtract(new DateTime(1900, 1, 1)).TotalSeconds;
		}
	}
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DocoptNet;

namespace Lying_NTP
{
	internal class Program
	{
		private const string Usage = @"Lying NTP Server. Adds specified number of seconds to correct time.

	Usage:
	  Lying_NTP.exe
	  Lying_NTP.exe [-n NUM | --num=NUM]
	  Lying_NTP.exe (-h | --help)

	Options:
	  -n NUM --num=NUM   Specify how many seconds to add [default: 0]
	  -h --help          Show this screen.

	";

		private static void Main(string[] args)
		{
			var arguments = new Docopt().Apply(Usage, args, optionsFirst: true, exit: true);

			var offset = arguments["--num"].AsInt;
			if (offset < 0)
			{
				Console.WriteLine("Offset shouldd be non negative number");
				Environment.Exit(1);
			}
			var server = new NtpServer((uint)offset);
			Console.WriteLine("\nPress ESC to exit");
			Task.Run(() => Quit());
			Task.Run(() => RunClient());
			server.Run();
			
		}

		private static void RunClient()
		{
			while (true)
			{
				Thread.Sleep(1000);
				var ntpData = NtpServer.GetDataFromServer(IPAddress.Loopback);
				Console.WriteLine(NtpServer.FromBytesToDateTime(ntpData));
			}
		}

		private static void Quit()
		{
			while (true)
				if (Console.ReadKey().Key == ConsoleKey.Escape)
					Environment.Exit(1);
		}
	}
}
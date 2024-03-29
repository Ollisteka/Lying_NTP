﻿using System;
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
	  Lying_NTP.exe [-s | --self] [-n NUM | --num=NUM]
	  Lying_NTP.exe (-h | --help)

	Options:
	  -n NUM --num=NUM   Specify how many seconds to add [default: 0]
	  -h --help          Show this screen.
	  -s --self          The server will generate time and packet itself.

	";

		private static void Main(string[] args)
		{
			var arguments = new Docopt().Apply(Usage, args, optionsFirst: true, exit: true);

			var offset = arguments["--num"].AsInt;
			var self = arguments["--self"].IsTrue;
			var server = new NtpServer(offset);
			Console.WriteLine("\nPress ESC to exit");
			Task.Run(() => Quit());
			server.Run(self);
			
		}

		private static void Quit()
		{
			while (true)
				if (Console.ReadKey().Key == ConsoleKey.Escape)
					Environment.Exit(1);
		}
	}
}
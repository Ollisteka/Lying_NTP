using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Lying_NTP
{
	[TestFixture]
	public class NtpServer_Should
	{
		public NtpServer Server = new NtpServer();

		[TestCase(0xaabbccdd, ExpectedResult = 0xddccbbaa)]
		[TestCase(0xa000000d, ExpectedResult = 0xd000000a)]
		public uint SwapEndianness(uint input)
		{
			return NtpServer.SwapEndianness(input);
		}
	}
}

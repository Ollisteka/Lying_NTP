namespace Lying_NTP
{
	public class NtpValues
	{
		public const int LeapAndModeMask = 0b00_111_100; //LI = 0, Mode = 4 чтобы сохранить версию, и установить Li и mode
		public const int VersionMask = 0b00_111_000; // чтобы скопировать версию клиента
		public const int Stratum = 3; //получаем время не от самого точного сервера, а попроще
		public const int PollInterval = 4; //как часто можем опрашивать сервер 2*n
		public const int Precision = -6; //2*n
		public const int RootDelay = 0; //сколько времени в секундах понадобилось, чтобы сходить узнать время
		public const int RootDispersion = 1; //погрешность
		public static readonly int ReferenceId = 0b0111_1111_0000_0000_0000_0000_0000_0001;
	}
}
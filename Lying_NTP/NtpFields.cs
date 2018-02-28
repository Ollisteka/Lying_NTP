namespace Lying_NTP
{
	public class NtpFields
	{
		public const int Header = 0;
		public const int Stratum = 1;
		public const int PollInterval = 2; 
		public const int Precision = 3; 
		public const int RootDelay = 4;
		public const int RootDispersion = 8;
		public const int ReferenceId = 12;
		public const int ReferenceTimestamp = 16; //когда в последний раз обновляла время
		public const int OriginateTimestamp = 24; //когда клиент отправил запрос
		public const int ReceiveTimestamp = 32; //когда сервер получит ответ    =
		public const int TransmitTimestamp = 40; //когда сервер отправил ответ  =
		 // d = (Dest - Orig) - (Rec- Orig)     t = ((Rec - Orig) + (tran - Dest)) / 2.
		 // result = transm + t

	}
}
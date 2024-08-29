
namespace TwitchApiLib
{
	public class TwitchVideoMetadataResult
	{
		public int ErrorCode { get; }
		public TwitchVideoMetadata Metadata { get; }

		public TwitchVideoMetadataResult(TwitchVideoMetadata metadata, int errorCode)
		{
			Metadata = metadata;
			ErrorCode = errorCode;
		}
	}
}


namespace TwitchApiLib
{
	public class TwitchPlaylistResult
	{
		public TwitchPlaylist Playlist { get; }
		public int ErrorCode { get; }

		public TwitchPlaylistResult(TwitchPlaylist playlist, int errorCode)
		{
			Playlist = playlist;
			ErrorCode = errorCode;
		}
	}
}

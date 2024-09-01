
namespace TwitchApiLib
{
	public class TwitchVodPlaylistManifestResult
	{
		public TwitchVodPlaylistManifest PlaylistManifest { get; }
		public int ErrorCode { get; }

		public TwitchVodPlaylistManifestResult(TwitchVodPlaylistManifest playlistManifest, int errorCode)
		{
			PlaylistManifest = playlistManifest;
			ErrorCode = errorCode;
		}
	}
}


namespace TwitchApiLib
{
	public class TwitchPlaylistManifestResult
	{
		public TwitchPlaylistManifest PlaylistManifest { get; }
		public int ErrorCode { get; }

		public TwitchPlaylistManifestResult(TwitchPlaylistManifest playlistManifest, int errorCode)
		{
			PlaylistManifest = playlistManifest;
			ErrorCode = errorCode;
		}
	}
}

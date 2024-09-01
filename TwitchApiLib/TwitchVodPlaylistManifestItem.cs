using System;
using MultiThreadedDownloaderLib;

namespace TwitchApiLib
{
	public class TwitchVodPlaylistManifestItem
	{
		public int ResolutionWidth { get; }
		public int ResolutionHeight { get; }
		public int Bandwidth { get; }
		public int FrameRate { get; }
		public string Codecs { get; }
		public string FormatId { get; }
		public string PlaylistUrl { get; }

		public TwitchVodPlaylistManifestItem(int resolutionWidth, int resolutionHeight, int bandwidth,
			int frameRate, string codecs, string formatId, string playlistUrl)
		{
			ResolutionWidth = resolutionWidth;
			ResolutionHeight = resolutionHeight;
			Bandwidth = bandwidth;
			FrameRate = frameRate;
			Codecs = codecs;
			FormatId = formatId;
			PlaylistUrl = playlistUrl;
		}

		public bool IsAudioOnly()
		{
			return !string.IsNullOrEmpty(FormatId) && FormatId.Contains("audio_only");
		}

		public TwitchVodPlaylistResult GetPlaylist()
		{
			if (!string.IsNullOrEmpty(PlaylistUrl) && !string.IsNullOrWhiteSpace(PlaylistUrl))
			{
				FileDownloader d = new FileDownloader() { Url = PlaylistUrl };
				int errorCode = d.DownloadString(out string response);
				TwitchVodPlaylistResult playlistResult = errorCode == 200 ?
					new TwitchVodPlaylistResult(new TwitchVodPlaylist(response, PlaylistUrl), 200) :
					new TwitchVodPlaylistResult(null, errorCode);
				d.Dispose();
				return playlistResult;
			}

			return new TwitchVodPlaylistResult(null, 400);
		}

		public override string ToString()
		{
			string t = IsAudioOnly() ? string.Empty :
				$"Resolution: {ResolutionWidth}x{ResolutionHeight}, {FrameRate} fps{Environment.NewLine}";
			t += $"Bandwidth: {Bandwidth}{Environment.NewLine}" +
				$"Codecs: {Codecs}{Environment.NewLine}" +
				$"Format ID: {FormatId}{Environment.NewLine}" +
				$"Playlist URL: {PlaylistUrl}{Environment.NewLine}";
			return t;
		}
	}
}

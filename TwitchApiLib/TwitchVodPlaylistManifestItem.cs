using System;

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
		public bool IsBestQuality { get; }
		public TwitchVodPlaylist Playlist { get; set; }

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
			IsBestQuality = string.Equals(formatId, "chunked", StringComparison.OrdinalIgnoreCase);
		}

		public bool IsAudioOnly()
		{
			return !string.IsNullOrEmpty(FormatId) && FormatId.Contains("audio_only");
		}

		public TwitchVodPlaylistResult GetPlaylist()
		{
			if (!string.IsNullOrEmpty(PlaylistUrl) && !string.IsNullOrWhiteSpace(PlaylistUrl))
			{
				int errorCode = Utils.DownloadString(PlaylistUrl, out string response);
				TwitchVodPlaylistResult playlistResult = errorCode == 200 ?
					new TwitchVodPlaylistResult(new TwitchVodPlaylist(response, null, this), 200) :
					new TwitchVodPlaylistResult(null, errorCode);
				return playlistResult;
			}

			return new TwitchVodPlaylistResult(null, 400);
		}

		public int UpdatePlaylist()
		{
			int errorCode = Utils.DownloadString(PlaylistUrl, out string playlistRawData);
			if (errorCode == 200)
			{
				Playlist = new TwitchVodPlaylist(playlistRawData, PlaylistUrl, this);
				Playlist.Parse();
			}

			return errorCode;
		}

		public override string ToString()
		{
			string t = $"Format ID: {FormatId}{Environment.NewLine}" +
				$"The best quality: {(IsBestQuality ? "Yes" : "No")}{Environment.NewLine}";
			if (!IsAudioOnly())
			{
				t += $"Resolution: {ResolutionWidth}x{ResolutionHeight}{Environment.NewLine}";
				if (FrameRate > 0)
				{
					t += $"Frame rate: {FrameRate} fps{Environment.NewLine}";
				}
			}
			t += $"Bandwidth: {Bandwidth}{Environment.NewLine}" +
				$"Codecs: {Codecs}{Environment.NewLine}" +
				$"Playlist URL: {PlaylistUrl}{Environment.NewLine}";
			return t;
		}
	}
}

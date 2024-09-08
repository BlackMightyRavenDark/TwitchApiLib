using System;

namespace TwitchApiLib
{
	public class TwitchPlaylistManifestItem
	{
		public int ResolutionWidth { get; }
		public int ResolutionHeight { get; }
		public int Bandwidth { get; }
		public int FrameRate { get; }
		public string Codecs { get; }
		public string FormatId { get; }
		public string PlaylistUrl { get; }

		public TwitchPlaylistManifestItem(int resolutionWidth, int resolutionHeight, int bandwidth,
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

		public TwitchPlaylistResult GetPlaylist()
		{
			if (!string.IsNullOrEmpty(PlaylistUrl) && !string.IsNullOrWhiteSpace(PlaylistUrl))
			{
				int errorCode = Utils.DownloadString(PlaylistUrl, out string response);
				TwitchPlaylistResult playlistResult = errorCode == 200 ?
					new TwitchPlaylistResult(new TwitchPlaylist(response, null, this), 200) :
					new TwitchPlaylistResult(null, errorCode);
				return playlistResult;
			}

			return new TwitchPlaylistResult(null, 400);
		}

		public override string ToString()
		{
			string t = $"Format ID: {FormatId}{Environment.NewLine}";
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

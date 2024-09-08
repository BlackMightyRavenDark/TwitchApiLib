using System;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace TwitchApiLib
{
	public class TwitchChannelLiveInfo
	{
		public ulong Id { get; }
		public ulong UserId { get; }
		public string UserLogin { get; }
		public string UserName { get; }
		public ulong GameId { get; }
		public string GameName { get; }
		public string StreamType { get; }
		public string StreamTitle { get; }
		public uint ViewerCount { get; }
		public DateTime StartedAt { get; }
		public string LanguageCode { get; }
		public string ThumbnailUrlTemplate { get; }
		public string[] Tags { get; }
		public bool IsMature { get; }

		public TwitchChannelLiveInfo(
			ulong id, ulong userId, string userLogin, string userName,
			ulong gameId, string gameName, string streamType, string streamTitle,
			uint viewerCount, DateTime startedAt, string languageCode,
			string thumbnailUrlTemplate, string[] tags, bool isMature)
		{
			Id = id;
			UserId = userId;
			UserLogin = userLogin;
			UserName = userName;
			GameId = gameId;
			GameName = gameName;
			StreamType = streamType;
			StreamTitle = streamTitle;
			ViewerCount = viewerCount;
			StartedAt = startedAt;
			LanguageCode = languageCode;
			ThumbnailUrlTemplate = thumbnailUrlTemplate;
			Tags = tags;
			IsMature = isMature;
		}

		public static TwitchChannelLiveInfoResult Get(ulong channelId)
		{
			return Utils.GetChannelLiveInfo_Helix(channelId);
		}

		public static TwitchChannelLiveInfoResult Get(string userLogin)
		{
			return Utils.GetChannelLiveInfo_Helix(userLogin);
		}

		public int GetHlsPlaylistManifestUrl(Guid deviceId,
			out string playlistManifestUrl, out string errorMessage)
		{
			ITwitchPlaybackAccessToken accessToken =
				TwitchApiGql.GetChannelPlaybackAccessToken(UserLogin, deviceId);
			if (accessToken.ErrorCode == 200)
			{
				JObject token = accessToken.GetToken(out errorMessage);
				if (!string.IsNullOrEmpty(errorMessage))
				{
					playlistManifestUrl = null;
					return 400;
				}

				string tokenValue = token.Value<string>("value");
				string tokenSignature = token.Value<string>("signature");

				int randomInt = new Random((int)DateTime.UtcNow.Ticks).Next(999999);

				NameValueCollection query = System.Web.HttpUtility.ParseQueryString(string.Empty);
				query.Add("acmb", "e30=");
				query.Add("allow_source", "true");
				query.Add("fast_bread", "true");
				query.Add("p", randomInt.ToString());
				query.Add("player_backend", "mediaplayer");
				query.Add("playlist_include_framerate", "true");
				query.Add("reassignments_supported", "true");
				query.Add("sig", tokenSignature);
				query.Add("supported_codecs", "avc1");
				query.Add("transcode_mode", "cbr_v1");
				query.Add("cdm", "wv");
				query.Add("player_version", "1.20.0");
				query.Add("token", tokenValue);

				string usherUrlFormatted = string.Format(TwitchApiGql.TWITCH_USHER_HLS_URL_TEMPLATE, UserLogin);
				playlistManifestUrl = $"{usherUrlFormatted}?{query}";

				return 200;
			}

			playlistManifestUrl = null;
			errorMessage = accessToken.RawData;
			return accessToken.ErrorCode;
		}

		public int GetHlsPlaylistManifestUrl(out string playlistManifestUrl, out string errorMessage)
		{
			return GetHlsPlaylistManifestUrl(Guid.Empty, out playlistManifestUrl, out errorMessage);
		}

		public int GetHlsPlaylistManifestUrl(out string playlistManifestUrl)
		{
			return GetHlsPlaylistManifestUrl(out playlistManifestUrl, out _);
		}

		public TwitchPlaylistManifestResult GetHlsPlaylistManifest()
		{
			int errorCode = GetHlsPlaylistManifestUrl(out string manifestUrl);
			if (errorCode == 200)
			{
				errorCode = Utils.DownloadString(manifestUrl, out string manifestText);
				TwitchPlaylistManifest playlistManifest = errorCode == 200 ?
					new TwitchPlaylistManifest(manifestText) : null;
				return new TwitchPlaylistManifestResult(playlistManifest, errorCode);
			}

			return new TwitchPlaylistManifestResult(null, errorCode);
		}

		public string FormatThumbnailTemplateUrl(ushort imageWidth, ushort imageHeight)
		{
			return ThumbnailUrlTemplate?
				.Replace("{width}", imageWidth.ToString())
				.Replace("{height}", imageHeight.ToString());
		}
	}
}

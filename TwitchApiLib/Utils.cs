using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using MultiThreadedDownloaderLib;
using static TwitchApiLib.TwitchApi;

namespace TwitchApiLib
{
	public static class Utils
	{
		const string TWITCH_VOD_PLAYLIST_ARCHIEVE_TEMPLATE_URL = "https://{0}.cloudfront.net/{1}/{2}/index-dvr.m3u8";
		const string TWITCH_PLAYLIST_HIGHLIGHT_TEMPLATE_URL = "https://{0}.cloudfront.net/{1}/{2}/highlight-{3}.m3u8";

		public static readonly TwitchHelixOauthToken TwitchHelixOauthToken = new TwitchHelixOauthToken();

		public static TwitchUserResult GetUserInfo(string userLogin)
		{
			lock (_twitchUsers)
			{
				if (_twitchUsers.ContainsKey(userLogin))
				{
					return new TwitchUserResult(_twitchUsers[userLogin], 200);
				}
			}

			string url = GenerateUserInfoRequestUrl(userLogin);
			int errorCode = HttpGet_Helix(url, out string response);
			if (errorCode == 200)
			{
				if (!IsUserExists(response))
				{
					return new TwitchUserResult(null, 404);
				}

				TwitchUser twitchUser = ParseTwitchUserInfo(response);
				if (twitchUser == null)
				{
					return new TwitchUserResult(null, 400);
				}

				AddUser(twitchUser);
				return new TwitchUserResult(twitchUser, errorCode);
			}

			return new TwitchUserResult(null, errorCode);
		}

		internal static TwitchVodResult GetTwitchVodInfo(ulong vodId)
		{
			string url = GenerateVodInfoRequestUrl(vodId);
			int errorCode = HttpGet_Helix(url, out string response);
			if (errorCode == 200)
			{
				JObject json = TryParseJson(response, out string parsingResult);
				if (json == null)
				{
					return new TwitchVodResult(null, 400, parsingResult, response);
				}

				JArray jaData = json.Value<JArray>("data");
				if (jaData == null)
				{
					return new TwitchVodResult(null, 204, "'data' not found", response);
				}

				if (jaData.Count == 0)
				{
					return new TwitchVodResult(null, 204, "The 'data' is empty", response);
				}

				return ParseVodInfo(jaData[0] as JObject);
			}

			return new TwitchVodResult(null, errorCode, null, response);
		}

		internal static TwitchVodPageResult GetChannelVideosPage(
			string channelId, uint maxVideos, string pageToken = null)
		{
			int errorCode = GetHelixOauthToken(out string token);
			if (errorCode == 200)
			{
				string url = GenerateChannelVideosRequestUrl(channelId, maxVideos, pageToken);

				FileDownloader d = new FileDownloader() { Url = url };
				d.Headers.Add("Client-ID", TWITCH_CLIENT_ID);
				d.Headers.Add("Authorization", "Bearer " + token);
				d.Headers.Add("User-Agent", GetUserAgent());

				errorCode = d.DownloadString(out string response);
				d.Dispose();

				if (errorCode == 200)
				{
					return new TwitchVodPageResult(new TwitchVodPage(response), errorCode);
				}
			}
			return new TwitchVodPageResult(null, errorCode);
		}

		public static TwitchUser ParseTwitchUserInfo(string rawData)
		{
			JObject json = TryParseJson(rawData);
			if (json == null)
			{
				return null;
			}

			JArray jaData = json.Value<JArray>("data");
			if (jaData == null || jaData.Count == 0)
			{
				return null;
			}

			ulong userId = jaData[0].Value<ulong>("id");
			string userLogin = jaData[0].Value<string>("login");
			string displayName = jaData[0].Value<string>("display_name");
			string userType = jaData[0].Value<string>("type");
			string broadcasterTypeString = jaData[0].Value<string>("broadcaster_type");
			string description = jaData[0].Value<string>("description");
			string profileImageUrl = jaData[0].Value<string>("profile_image_url");
			string offlineImageUrl = jaData[0].Value<string>("offline_image_url");
			ulong viewCount = jaData[0].Value<ulong>("view_count");
			string createdAt = jaData[0].Value<string>("created_at");
			DateTime creationDate = ParseDateTime(createdAt);

			TwitchBroadcasterType broadcasterType = GetBroadcasterType(broadcasterTypeString);
			TwitchPlaybackAccessMode playbackAccessMode = TwitchApiGql.GetChannelPlaybackAccessMode(userLogin, out _);

			return new TwitchUser(userId, userLogin, displayName, userType, broadcasterType, playbackAccessMode,
				description, profileImageUrl, offlineImageUrl, viewCount, creationDate, rawData);
		}

		public static TwitchChannelLiveInfo ParseChannelLiveInfo(JObject liveInfo)
		{
			try
			{
				JArray jaData = liveInfo.Value<JArray>("data");
				if (jaData.Count > 0)
				{
					JObject jData = jaData[0] as JObject;
					ulong streamId = jData.Value<ulong>("id");
					ulong userId = jData.Value<ulong>("user_id");
					string userLogin = jData.Value<string>("user_login");
					string userName = jData.Value<string>("user_name");
					ulong gameId = jData.Value<ulong>("game_id");
					string gameName = jData.Value<string>("game_name");
					string streamType = jData.Value<string>("type");
					string streamTitle = jData.Value<string>("title");
					uint viewerCount = jData.Value<uint>("viewer_count");
					DateTime startedAt = ParseDateTime(jData.Value<string>("started_at"));
					string languageCode = jData.Value<string>("language");
					string thumbnailUrlTemplate = jData.Value<string>("thumbnail_url");
					string[] tags = jData.Value<JArray>("tags")?.ToObject<string[]>();
					bool isMature = jData.Value<bool>("is_mature");

					TwitchChannelLiveInfo live = new TwitchChannelLiveInfo(
						streamId, userId, userLogin, userName, gameId, gameName,
						streamType, streamTitle, viewerCount, startedAt,
						languageCode, thumbnailUrlTemplate, tags, isMature);
					return live;
				}
			} catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}

			return null;
		}

		public static TwitchVodResult ParseVodInfo(JObject vodInfo)
		{
			try
			{
				ulong vodId = vodInfo.Value<ulong>("id");
				JToken jt = vodInfo.Value<JToken>("stream_id");
				ulong streamId = jt != null && jt.Type != JTokenType.Null ? jt.Value<ulong>() : 0U;
				string userLogin = vodInfo.Value<string>("user_login");
				string title = vodInfo.Value<string>("title");
				string description = vodInfo.Value<string>("description");
				string createdAt = vodInfo.Value<string>("created_at");
				DateTime creationDate = ParseDateTime(createdAt);
				string publishedAt = vodInfo.Value<string>("published_at");
				DateTime publishedDate = ParseDateTime(publishedAt);
				string url = vodInfo.Value<string>("url");
				string thumbnailTemplateUrl = vodInfo.Value<string>("thumbnail_url");
				string viewable = vodInfo.Value<string>("viewable");
				ulong viewCount = vodInfo.Value<ulong>("view_count");
				string language = vodInfo.Value<string>("language");
				string vodTypeString = vodInfo.Value<string>("type");

				TwitchVodType vodType = GetVodType(vodTypeString);

				TimeSpan duration = TimeSpan.Zero;

				TwitchUserResult twitchUserResult = TwitchUser.Get(userLogin);
				DateTime deletionDeletion = DateTime.MaxValue;
				if (vodType == TwitchVodType.Archive && twitchUserResult.ErrorCode == 200 && twitchUserResult.User != null)
				{
					bool isPartner = twitchUserResult.User.BroadcasterType == TwitchBroadcasterType.Partner;
					deletionDeletion = creationDate.AddDays(isPartner ? 60.0 : 14.0);
				}

				TwitchGame game;
				TwitchVideoMetadataResult videoMetadataResult = TwitchApiGql.GetVodMetadata(vodId.ToString(), userLogin);
				if (videoMetadataResult.ErrorCode == 200)
				{
					int seconds = videoMetadataResult.Metadata.GetVideoLengthSeconds();
					if (seconds > 0)
					{
						duration = TimeSpan.FromSeconds(seconds);
					}

					game = videoMetadataResult.Metadata.GetGameInfo();
				}
				else
				{
					game = TwitchGame.CreateUnknownGame();
				}

				TwitchPlaybackAccessMode playbackAccessMode = TwitchApiGql.GetVodPlaybackAccessMode(vodId.ToString(), out _);
				ExtractVodSpecialDataFromThumbnailUrl(thumbnailTemplateUrl, out string specialId, out string serverId);
				_ = GetVodPlaylistUrl(serverId, specialId, "chunked", vodId.ToString(), vodType,
					playbackAccessMode == TwitchPlaybackAccessMode.SubscribersOnly, out string playlistUrl);

				TwitchVod vod = new TwitchVod(vodId, title, description, duration, game, creationDate,
					publishedDate, deletionDeletion, url, thumbnailTemplateUrl, viewable, viewCount,
					language, vodType, playbackAccessMode, streamId, twitchUserResult.User,
					playlistUrl, null, vodInfo.ToString(), videoMetadataResult.Metadata);
				return new TwitchVodResult(vod, 200, null, vodInfo.ToString());
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				return new TwitchVodResult(null, ex.HResult, ex.Message, vodInfo.ToString());
			}
		}

		public static TwitchVodResult ParseVodInfo(string rawVodInfo)
		{
			JObject json = TryParseJson(rawVodInfo, out string errorMessage);
			return json != null ? ParseVodInfo(json) : new TwitchVodResult(null, 400, errorMessage, rawVodInfo);
		}

		public static TwitchGame ParseGameInfo(string rawGameInfo)
		{
			JObject json = TryParseJson(rawGameInfo);
			JObject jGame = json?.Value<JObject>("data")?.Value<JObject>("video")?.Value<JObject>("game");
			if (jGame != null)
			{
				string title = jGame.Value<string>("name");
				if (!ulong.TryParse(jGame.Value<string>("id"), out ulong id))
				{
					id = 0UL;
				}

				string boxArlUrl = id > 0UL ? TwitchGame.FormatPreviewImageTemplateUrl(id, 0, 0) : null;

				return new TwitchGame(title, title, id, boxArlUrl, rawGameInfo);
			}

			return null;
		}

		public static string GenerateUserInfoRequestUrl(string userLogin)
		{
			return $"{TWITCH_API_HELIX_USERS_ENDPOINT_URL}?login={userLogin}";
		}

		public static string GenerateVodInfoRequestUrl(ulong vodId)
		{
			return $"{TWITCH_API_HELIX_VIDEOS_ENDPOINT_URL}?id={vodId}";
		}

		public static string GenerateChannelVideosRequestUrl(
			string userId, uint videosPerPage, string pageToken)
		{
			NameValueCollection query = System.Web.HttpUtility.ParseQueryString(string.Empty);
			query.Add("user_id", userId);
			if (videosPerPage > 0U)
			{
				if (videosPerPage > 100U) { videosPerPage = 100U; }
				query.Add("first", videosPerPage.ToString());
			}
			if (!string.IsNullOrEmpty(pageToken) && !string.IsNullOrWhiteSpace(pageToken))
			{
				query.Add("after", pageToken);
			}

			return $"{TWITCH_API_HELIX_VIDEOS_ENDPOINT_URL}?{query}";
		}

		private static TwitchBroadcasterType GetBroadcasterType(string broadcasterType)
		{
			if (string.IsNullOrEmpty(broadcasterType) || string.IsNullOrWhiteSpace(broadcasterType))
			{
				return TwitchBroadcasterType.Undefined;
			}
			else
			{
				broadcasterType = broadcasterType.ToLower();
				switch (broadcasterType)
				{
					case "affiliate":
						return TwitchBroadcasterType.Affiliate;

					case "partner":
						return TwitchBroadcasterType.Partner;
				}
			}

			return TwitchBroadcasterType.Unknown;
		}

		private static TwitchVodType GetVodType(string vodType)
		{
			if (string.IsNullOrEmpty(vodType) || string.IsNullOrWhiteSpace(vodType))
			{
				return TwitchVodType.Undefined;
			}
			else
			{
				vodType = vodType.ToLower();
				switch (vodType)
				{
					case "archive":
						return TwitchVodType.Archive;

					case "highlight":
						return TwitchVodType.Highlight;

					case "upload":
						return TwitchVodType.Upload;
				}
			}

			return TwitchVodType.Unknown;
		}

		private static bool IsUserExists(string searchResultsJson)
		{
			JObject json = TryParseJson(searchResultsJson);
			if (json != null)
			{
				JArray jaData = json.Value<JArray>("data");
				return jaData != null && jaData.Count > 0;
			}

			return false;
		}

		public static int GetHelixOauthToken(string clientId, out string responseToken)
		{
			int errorCode = TwitchHelixOauthToken.IsExpired() ? TwitchHelixOauthToken.Update(clientId) : 200;
			responseToken = errorCode == 200 ? TwitchHelixOauthToken.AccessToken : null;
			return errorCode;
		}

		public static int GetHelixOauthToken(out string responseToken)
		{
			return GetHelixOauthToken(TWITCH_CLIENT_ID, out responseToken);
		}

		public static TwitchChannelLiveInfoResult GetChannelLiveInfo_Helix(ulong channelId)
		{
			string url = $"{TWITCH_API_HELIX_STREAMS_ENDPOINT_URL}?user_id={channelId}";
			int errorCode = HttpGet_Helix(url, out string response);
			if (errorCode == 200)
			{
				JObject json = TryParseJson(response);
				if (json == null)
				{
					return new TwitchChannelLiveInfoResult(null, 204);
				}

				TwitchChannelLiveInfo channelLiveInfo = ParseChannelLiveInfo(json);
				if (channelLiveInfo == null) { errorCode = 204; }
				return new TwitchChannelLiveInfoResult(channelLiveInfo, errorCode);
			}

			return new TwitchChannelLiveInfoResult(null, errorCode);
		}

		public static TwitchChannelLiveInfoResult GetChannelLiveInfo_Helix(string userLogin)
		{
			TwitchUserResult userResult = TwitchUser.Get(userLogin);
			return userResult.ErrorCode == 200 ?
				GetChannelLiveInfo_Helix(userResult.User.Id) :
				new TwitchChannelLiveInfoResult(null, userResult.ErrorCode);
		}

		private static int GetVodPlaybackAccessToken(string vodId, out JObject token, out string errorText)
		{
			JObject body = TwitchApiGql.GeneratePlaybackAccessTokenRequestBody(vodId, string.Empty);
			int errorCode = HttpPost(TwitchApiGql.TWITCH_GQL_API_URL, body.ToString(), out string response);
			if (errorCode == 200)
			{
				JObject json = TryParseJson(response);
				if (json != null)
				{
					token = json.Value<JObject>("data")?.Value<JObject>("videoPlaybackAccessToken");
					if (token != null)
					{
						errorText = null;
						return 200;
					}
				}

				token = null;
				errorText = "Token not found";
				return 204;
			}

			token = null;
			errorText = null;
			return errorCode;
		}

		private static int GetVodPlaybackAccessToken(string vodId, out JObject token)
		{
			return GetVodPlaybackAccessToken(vodId, out token, out _);
		}

		public static int GetVodPlaybackAccessToken(ulong vodId, out JObject token, out string errorText)
		{
			return GetVodPlaybackAccessToken(vodId.ToString(), out token, out errorText);
		}

		public static int GetVodPlaybackAccessToken(ulong vodId, out JObject token)
		{
			return GetVodPlaybackAccessToken(vodId, out token, out _);
		}

		public static int GetVodPlaylistManifestUrl(string vodId, bool isSubscribersOnly, out string playListManifestUrl)
		{
			if (isSubscribersOnly)
			{
				playListManifestUrl = null;
				return 403;
			}

			int errorCode = GetVodPlaybackAccessToken(vodId, out JObject jToken);
			playListManifestUrl = errorCode == 200 ? TwitchApiGql.GenerateVodPlaylistManifestUrl(vodId.ToString(), jToken) : null;

			return errorCode;
		}

		public static int GetVodPlaylistManifestUrl(ulong vodId, bool isSubscribersOnly, out string playListManifestUrl)
		{
			return GetVodPlaylistManifestUrl(vodId.ToString(), isSubscribersOnly, out playListManifestUrl);
		}

		public static int GetVodPlaylistManifestUrl(TwitchVod vod, out string playListManifestUrl)
		{
			return GetVodPlaylistManifestUrl(vod.Id, vod.IsSubscribersOnly, out playListManifestUrl);
		}

		public static TwitchVodPlaylistManifestResult GetVodPlaylistManifest(string vodId, bool isSubscribersOnly)
		{
			if (isSubscribersOnly)
			{
				return new TwitchVodPlaylistManifestResult(null, 403);
			}

			int errorCode = GetVodPlaylistManifestUrl(vodId, false, out string playlistManifestUrl);
			if (errorCode == 200)
			{
				FileDownloader d = new FileDownloader() { Url = playlistManifestUrl };
				errorCode = d.DownloadString(out string manifestRaw);
				if (errorCode == 200)
				{
					TwitchVodPlaylistManifest playlistManifest = new TwitchVodPlaylistManifest(manifestRaw);
					return new TwitchVodPlaylistManifestResult(playlistManifest, 200);
				}
			}

			return new TwitchVodPlaylistManifestResult(null, errorCode);
		}

		public static TwitchVodPlaylistManifestResult GetVodPlaylistManifest(ulong vodId, bool isSubscribersOnly)
		{
			return GetVodPlaylistManifest(vodId.ToString(), isSubscribersOnly);
		}

		public static TwitchVodPlaylistManifestResult GetVodPlaylistManifest(TwitchVod vod)
		{
			return GetVodPlaylistManifest(vod.Id.ToString(), vod.IsSubscribersOnly);
		}

		public static void ExtractVodSpecialDataFromThumbnailUrl(string thumbnailUrl,
			out string specialId, out string serverId)
		{
			if (string.IsNullOrEmpty(thumbnailUrl) || string.IsNullOrWhiteSpace(thumbnailUrl))
			{
				specialId = serverId = null;
				return;
			}

			bool isUploadThumbnailUrl = thumbnailUrl.Contains("/index-");
			if (isUploadThumbnailUrl)
			{
				//There is impossible to get data.
				specialId = serverId = null;
				return;
			}

			string t = thumbnailUrl.Remove(0, thumbnailUrl.IndexOf("/cf_vods/") + 9);
			int n = t.IndexOf("/");
			serverId = t.Substring(0, n);
			t = t.Remove(0, n + 1);
			specialId = t.Substring(0, t.IndexOf("/"));
		}

		public static int GetVodPlaylistUrl(string serverId, string vodSpecialId, string formatId,
			string vodId, TwitchVodType vodType, bool isSubscribersOnly, out string playlistUrl)
		{
			if (!isSubscribersOnly)
			{
				TwitchVodPlaylistManifestResult playlistManifestResult = GetVodPlaylistManifest(vodId, false);
				if (playlistManifestResult.ErrorCode == 200)
				{
					TwitchVodPlaylistManifestItem manifestItem = playlistManifestResult.PlaylistManifest[formatId];
					if (manifestItem != null)
					{
						playlistUrl = manifestItem.PlaylistUrl;
						return 200;
					}
					else if (playlistManifestResult.PlaylistManifest.Count > 0)
					{
						playlistManifestResult.PlaylistManifest.SortByBandwidth();
						playlistUrl = playlistManifestResult.PlaylistManifest[0].PlaylistUrl;
						return 200;
					}
					else
					{
						playlistUrl = null;
						return 404;
					}
				}
			}

			if (string.IsNullOrEmpty(serverId) || string.IsNullOrWhiteSpace(serverId) ||
				string.IsNullOrEmpty(vodSpecialId) || string.IsNullOrWhiteSpace(vodSpecialId) ||
				string.IsNullOrEmpty(formatId) || string.IsNullOrWhiteSpace(formatId))
			{
				playlistUrl = null;
				return 400;
			}

			switch (vodType)
			{
				case TwitchVodType.Highlight:
					if (string.IsNullOrEmpty(vodId) || string.IsNullOrWhiteSpace(vodId))
					{
						playlistUrl = null;
						return 400;
					}

					playlistUrl = string.Format(TWITCH_PLAYLIST_HIGHLIGHT_TEMPLATE_URL, serverId, vodSpecialId, formatId, vodId);
					return 200;

				case TwitchVodType.Upload:
					//There is impossible to get URL.
					playlistUrl = null;
					return 404;

				default:
					playlistUrl = string.Format(TWITCH_VOD_PLAYLIST_ARCHIEVE_TEMPLATE_URL, serverId, vodSpecialId, formatId);
					return 200;
			}
		}

		public static int GetVodPlaylistUrl(string serverId, string vodSpecialId, string formatId, out string playlistUrl)
		{
			return GetVodPlaylistUrl(serverId, vodSpecialId, formatId, null, TwitchVodType.Archive, false, out playlistUrl);
		}

		public static int GetVodPlaylistUrl(TwitchVod vod, string formatId, out string playlistUrl)
		{
			vod.GetSpecialData(out string specialId, out string serverId);
			return GetVodPlaylistUrl(serverId, specialId, formatId, vod.Id.ToString(),
				vod.VodType, vod.IsSubscribersOnly, out playlistUrl);
		}

		public static int GetVodPlaylistUrl(TwitchVod vod, out string playlistUrl)
		{
			return GetVodPlaylistUrl(vod, "chunked", out playlistUrl);
		}

		internal static Dictionary<string, string> SplitStringToKeyValues(
			string inputString, string keySeparator, char valueSeparator)
		{
			if (string.IsNullOrEmpty(inputString) || string.IsNullOrWhiteSpace(inputString))
			{
				return null;
			}
			string[] keyValues = inputString.Split(new string[] { keySeparator }, StringSplitOptions.None);
			Dictionary<string, string> dict = new Dictionary<string, string>();
			for (int i = 0; i < keyValues.Length; ++i)
			{
				string[] t = keyValues[i].Split(valueSeparator);
				string value = t.Length > 1 ? t[1] : string.Empty;
				dict[t[0]] = value;
			}
			return dict;
		}

		public static bool IsGmt(this DateTime dateTime)
		{
			return dateTime.Kind == DateTimeKind.Utc;
		}

		public static string FormatDateTime(this DateTime dateTime)
		{
			string t = dateTime.ToString("yyyy.MM.dd HH:mm:ss");
			return dateTime.IsGmt() ? $"{t} GMT" : t;
		}

		internal static DateTime ParseDateTime(string dateTimeString, string format = "MM/dd/yyyy HH:mm:ss")
		{
			if (DateTime.TryParseExact(dateTimeString, format, null,
				System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime dateTime))
			{
				return dateTime.ToUniversalTime();
			}

			return DateTime.MaxValue;
		}

		public static int HttpGet_Helix(string url, out string response)
		{
			int errorCode = GetHelixOauthToken(out string token);
			if (errorCode == 200)
			{
				FileDownloader d = new FileDownloader() { Url = url };
				d.Headers.Add("Client-ID", TWITCH_CLIENT_ID);
				d.Headers.Add("Authorization", "Bearer " + token);
				d.Headers.Add("User-Agent", GetUserAgent());

				return d.DownloadString(out response);
			}

			response = null;
			return errorCode;
		}

		public static int HttpPost(string url, out string response)
		{
			return HttpPost(url, null, out response);
		}

		public static int HttpPost(string url, string body, out string response)
		{
			try
			{
				NameValueCollection headers = null;
				if (!string.IsNullOrEmpty(body))
				{
					headers = new NameValueCollection
					{
						{ "Content-Type", "application/json" },
						{ "Client-ID", TwitchApiGql.TWITCH_GQL_CLIENT_ID },
						{ "User-Agent", GetUserAgent() }
					};
				}

				using (HttpRequestResult requestResult = HttpRequestSender.Send("POST", url, body, headers))
				{
					return requestResult.WebContent.ContentToString(out response);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				response = ex.Message;
				return ex.HResult;
			}
		}

		internal static JObject TryParseJson(string jsonString, out string errorMessage)
		{
			try
			{
				errorMessage = null;
				return JObject.Parse(jsonString);
			} catch (Exception ex)
			{
				errorMessage = ex.Message;
				return null;
			}
		}

		internal static JObject TryParseJson(string jsonString)
		{
			return TryParseJson(jsonString, out _);
		}

		internal static JArray TryParseJsonArray(string jsonString, out string errorMessage)
		{
			try
			{
				errorMessage = null;
				return JArray.Parse(jsonString);
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return null;
			}
		}
	}
}

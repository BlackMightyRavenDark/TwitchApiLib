using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using MultiThreadedDownloaderLib;
using static TwitchApiLib.TwitchApi;

namespace TwitchApiLib
{
	public static class Utils
	{
		const string TWITCH_VOD_PLAYLIST_ARCHIEVE_URL_TEMPLATE = "https://{0}.cloudfront.net/{1}/{2}/index-dvr.m3u8";
		const string TWITCH_PLAYLIST_HIGHLIGHT_URL_TEMPLATE = "https://{0}.cloudfront.net/{1}/{2}/highlight-{3}.m3u8";

		public static readonly TwitchHelixOauthToken TwitchHelixOauthToken = new TwitchHelixOauthToken();

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
			}
#if DEBUG
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
#else
			catch
			{
#endif
			}

			return null;
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

				string boxArlUrl = id > 0UL ? TwitchGame.FormatThumbnailTemplateUrl(id, 52, 72) : TwitchGame.UNKNOWN_GAME_BOXART_URL;

				return new TwitchGame(title, title, id, boxArlUrl, rawGameInfo);
			}

			return null;
		}

		public static string GenerateUserInfoRequestUrl(string[] userLogins)
		{
			string logins = string.Empty;
			foreach (string login in userLogins)
			{
				logins += $"login={login}&";
			}
			if (logins.Length > 1) { logins = logins.Substring(0, logins.Length - 1); }
			return $"{TWITCH_API_HELIX_USERS_ENDPOINT_URL}?{logins}";
		}

		public static string GenerateUserInfoRequestUrl(string userLogin)
		{
			return GenerateUserInfoRequestUrl(new string[] { userLogin });
		}

		public static string GenerateUserInfoRequestUrl(ulong[] userIds)
		{
			string ids = string.Empty;
			foreach (ulong id in userIds)
			{
				ids += $"id={id}&";
			}
			if (ids.Length > 1) { ids = ids.Substring(0, ids.Length - 1); }
			return $"{TWITCH_API_HELIX_USERS_ENDPOINT_URL}?{ids}";
		}

		public static string GenerateUserInfoRequestUrl(ulong userId)
		{
			return GenerateUserInfoRequestUrl(new ulong[] { userId });
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

		public static string GenerateChannelLiveInfoRequestUrl(ulong channelId)
		{
			return $"{TWITCH_API_HELIX_STREAMS_ENDPOINT_URL}?user_id={channelId}";
		}

		public static int FindRawUserInfo(string[] userLogins, out JObject result)
		{
			string url = GenerateUserInfoRequestUrl(userLogins);
			return FindRawUserInfoByUrl(url, out result);
		}

		public static int FindRawUserInfo(string userLogin, out JObject result)
		{
			return FindRawUserInfo(new string[] { userLogin }, out result);
		}

		public static int FindRawUserInfo(ulong[] userIds, out JObject result)
		{
			string url = GenerateUserInfoRequestUrl(userIds);
			return FindRawUserInfoByUrl(url, out result);
		}

		public static int FindRawUserInfo(ulong userId, out JObject result)
		{
			return FindRawUserInfo(new ulong[] { userId }, out result);
		}

		private static int FindRawUserInfoByUrl(string url, out JObject result)
		{
			int errorCode = HttpGet_Helix(url, out string response, out _);
			if (errorCode == 200)
			{
				result = TryParseJson(response);
				return result != null ? 200 : 400;
			}

			result = null;
			return errorCode;
		}

		internal static TwitchBroadcasterType GetBroadcasterType(string broadcasterType)
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

		internal static TwitchVodType GetVodType(string vodType)
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

		public static int GetHelixOauthToken(string clientId, string clientSecretKey, out string responseToken, out string errorMessage)
		{
			lock (TwitchHelixOauthToken)
			{
				errorMessage = null;
				int errorCode = TwitchHelixOauthToken.IsExpired() ? TwitchHelixOauthToken.Update(clientId, clientSecretKey, out errorMessage) : 200;
				responseToken = errorCode == 200 ? TwitchHelixOauthToken.AccessToken : null;
				return errorCode;
			}
		}

		public static int GetHelixOauthToken(TwitchApplication application, out string responseToken, out string errorMessage)
		{
			return GetHelixOauthToken(application.ClientId, application.ClientSecretKey, out responseToken, out errorMessage);
		}

		public static int GetHelixOauthToken(out string responseToken, out string errorMessage)
		{
			TwitchApplication application = GetApplication();
			return GetHelixOauthToken(application, out responseToken, out errorMessage);
		}

		public static TwitchChannelLiveInfoResult GetChannelLiveInfo_Helix(ulong channelId)
		{
			string url = GenerateChannelLiveInfoRequestUrl(channelId);
			int errorCode = HttpGet_Helix(url, out string response, out _);
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

		private static int GetVodPlaybackAccessToken(string vodId,
			out ITwitchPlaybackAccessToken token, out string errorText)
		{
			JObject body = TwitchApiGql.GeneratePlaybackAccessTokenRequestBody(vodId, string.Empty);
			int errorCode = HttpPost(TwitchApiGql.TWITCH_GQL_API_URL, body.ToString(), out string response);
			if (errorCode == 200)
			{
				token = new TwitchVideoPlaybackAccessToken(response, 200);
				errorText = null;
				return 200;
			}

			token = null;
			errorText = response;
			return errorCode;
		}

		private static int GetVodPlaybackAccessToken(string vodId, out ITwitchPlaybackAccessToken token)
		{
			return GetVodPlaybackAccessToken(vodId, out token, out _);
		}

		public static int GetVodPlaybackAccessToken(ulong vodId,
			out ITwitchPlaybackAccessToken token, out string errorText)
		{
			return GetVodPlaybackAccessToken(vodId.ToString(), out token, out errorText);
		}

		public static int GetVodPlaybackAccessToken(ulong vodId, out ITwitchPlaybackAccessToken token)
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

			int errorCode = GetVodPlaybackAccessToken(vodId, out ITwitchPlaybackAccessToken token);
			playListManifestUrl = errorCode == 200 ? TwitchApiGql.GenerateVodPlaylistManifestUrl(vodId.ToString(), token) : null;

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

		public static int GenerateVodPlaylistUrl(string serverId, string vodSpecialId, string formatId,
			string vodId, TwitchVodType vodType, out string playlistUrl)
		{
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

					playlistUrl = string.Format(TWITCH_PLAYLIST_HIGHLIGHT_URL_TEMPLATE, serverId, vodSpecialId, formatId, vodId);
					return 200;

				case TwitchVodType.Upload:
					//There is impossible to get URL.
					playlistUrl = null;
					return 404;

				default:
					playlistUrl = string.Format(TWITCH_VOD_PLAYLIST_ARCHIEVE_URL_TEMPLATE, serverId, vodSpecialId, formatId);
					return 200;
			}
		}

		public static int GenerateVodPlaylistUrl(string serverId, string vodSpecialId, string formatId, out string playlistUrl)
		{
			return GenerateVodPlaylistUrl(serverId, vodSpecialId, formatId, null, TwitchVodType.Archive, out playlistUrl);
		}

		public static int GenerateVodPlaylistUrl(TwitchVod vod, string formatId, out string playlistUrl)
		{
			vod.ExtractSpecialData(out string specialId, out string serverId);
			return GenerateVodPlaylistUrl(serverId, specialId, formatId, vod.Id.ToString(), vod.VodType, out playlistUrl);
		}

		public static int GenerateVodPlaylistUrl(TwitchVod vod, out string playlistUrl)
		{
			return GenerateVodPlaylistUrl(vod, "chunked", out playlistUrl);
		}

		public static TwitchPlaybackAccessMode GetPlaybackAccessMode(
			this ITwitchPlaybackAccessToken twitchPlaybackAccessToken)
		{
			JObject jPlaybackAccessToken = twitchPlaybackAccessToken.GetToken();
			if (jPlaybackAccessToken != null)
			{
				JObject tokenValue = TryParseJson(jPlaybackAccessToken.Value<string>("value"));
				JArray jaRestrictedBitrates = tokenValue?.Value<JObject>("chansub")?.Value<JArray>("restricted_bitrates");
				if (jaRestrictedBitrates != null)
				{
					bool isPrime = jaRestrictedBitrates.Count > 0;
					return isPrime ? TwitchPlaybackAccessMode.SubscribersOnly : TwitchPlaybackAccessMode.Free;
				}
			}

			return TwitchPlaybackAccessMode.Unknown;
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

		internal static TimeSpan ParseVodDuration(string hmsString)
		{
			int h = extract(hmsString, @"(\d{1,2})h");
			int m = extract(hmsString, @"(\d{1,2})m");
			int s = extract(hmsString, @"(\d{1,2})s");
			return TimeSpan.FromSeconds(h * 3600 + m * 60 + s);

			int extract(string inputString, string regexp)
			{
				Regex regex = new Regex(regexp);
				Match match = regex.Match(inputString);
				if (match.Success && int.TryParse(match.Groups[1].Value, out int n)) { return n; }
				return 0;
			}
		}

		public static int HttpGet_Helix(string url, out string response, out string errorMessage)
		{
			TwitchApplication application = GetApplication();
			int errorCode = GetHelixOauthToken(application, out string token, out errorMessage);
			if (errorCode == 200)
			{
				FileDownloader d = MakeTwitchApiBearerClient(application.ClientId, token);
				d.Url = url;
				return d.DownloadString(out response);
			}

			response = null;
			return errorCode;
		}

		public static int HttpPost(string url, string body,
			WebHeaderCollection headers, out string response)
		{
			try
			{
				using (HttpRequestResult requestResult = HttpRequestSender.Send("POST", url, body, Encoding.UTF8, headers))
				{
					if (requestResult.ErrorCode != 200 && requestResult.ErrorCode != 206)
					{
						response = requestResult.ErrorMessage;
						return requestResult.ErrorCode;
					}

					int errorCode = requestResult.GetContent(out response);
					if (errorCode != 200) { return errorCode; }

					return requestResult.WebContent.ContentToString(out response);
				}
			}
			catch (Exception ex)
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine(ex.Message);
#endif
				response = ex.Message;
				return ex.HResult;
			}
		}

		public static int HttpPost(string url, string body, out string response)
		{
			WebHeaderCollection headers = null;
			if (!string.IsNullOrEmpty(body))
			{
				string userAgent = GetUserAgent();
				headers = new WebHeaderCollection
				{
					{ "Content-Type", "application/json" },
					{ "Client-ID", TwitchApiGql.TWITCH_GQL_CLIENT_ID },
					{ "User-Agent", userAgent }
				};
			}

			return HttpPost(url, body, headers, out response);
		}

		public static int HttpPost(string url, out string response)
		{
			return HttpPost(url, null, out response);
		}

		internal static int DownloadString(string url, out string responseString,
			FileDownloader downloader = null)
		{
			bool ownDownloader = downloader == null;
			if (ownDownloader)
			{
				downloader = MakeDefaultDownloader();
				downloader.Url = url;
				downloader.SkipHeaderRequest = true;
			}

			int errorCode = downloader.DownloadString(out responseString);
			if (ownDownloader) { downloader.Dispose(); }
			return errorCode;
		}

		internal static FileDownloader MakeTwitchApiBearerClient(string clientId, string bearerAuthorizationToken)
		{
			FileDownloader d = MakeDefaultDownloader();
			d.Headers.Add("Client-ID", clientId);
			if (!string.IsNullOrEmpty(bearerAuthorizationToken) && !string.IsNullOrWhiteSpace(bearerAuthorizationToken))
			{
				d.Headers.Add("Authorization", "Bearer " + bearerAuthorizationToken);
			}

			return d;
		}

		internal static FileDownloader MakeDefaultDownloader()
		{
			int timeout = GetConnectionTimeout();
			string userAgent = GetUserAgent();
			FileDownloader d = new FileDownloader() { ConnectionTimeout = timeout };
			d.Headers.Add("User-Agent", userAgent);
			return d;
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

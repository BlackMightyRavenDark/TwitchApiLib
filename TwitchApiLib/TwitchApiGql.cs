using System;
using System.Collections.Specialized;
using System.Web;
using Newtonsoft.Json.Linq;
using static TwitchApiLib.TwitchApi;

namespace TwitchApiLib
{
	/// <summary>
	/// WARNING!!! The GQL API is unofficial! Do not use it while you signed in!
	/// Also remember that it's may be unexpectedly changed/stopped at any time!
	/// </summary>
	public static class TwitchApiGql
	{
		public const string TWITCH_GQL_API_URL = "https://gql.twitch.tv/gql";
		public const string TWITCH_GQL_CLIENT_ID = "kimne78kx3ncx6brgo4mv6wki5h1ko";
		public const string TWITCH_USHER_VOD_URL_TEMPLATE = "https://usher.ttvnw.net/vod/{0}.m3u8";
		public const string TWITCH_USHER_HLS_URL_TEMPLATE = "https://usher.ttvnw.net/api/channel/hls/{0}.m3u8";

		public static TwitchVideoMetadataResult GetVodMetadata(string vodId, string channelLogin)
		{
			JArray body = GenerateVodInfoRequestBody(vodId, channelLogin);
			int errorCode = Utils.HttpPost(TWITCH_GQL_API_URL, body.ToString(), out string response);
			if (errorCode == 200 && Utils.TryParseJsonArray(response, out _) == null)
			{
				errorCode = 400;
			}

			return new TwitchVideoMetadataResult(new TwitchVideoMetadata(response), errorCode);
		}

		public static JArray GenerateVodInfoRequestBody(string vodId, string channelLogin)
		{
			const string hashValue = "cb3b1eb2f2d2b2f65b8389ba446ec521d76c3aa44f5424a1b1d235fe21eb4806";
			JObject jPersistedQuery = new JObject()
			{
				["version"] = 1,
				["sha256Hash"] = hashValue
			};

			JObject jExtensions = new JObject()
			{
				["persistedQuery"] = jPersistedQuery
			};

			JObject jVariables = new JObject()
			{
				["channelLogin"] = channelLogin,
				["videoID"] = vodId
			};

			JObject json = new JObject()
			{
				["operationName"] = "VideoMetadata",
				["variables"] = jVariables,
				["extensions"] = jExtensions
			};

			JArray jArray = new JArray() { json };
			return jArray;
		}

		public static JObject GenerateVodGameInfoRequestBody(string vodId)
		{
			const string hashValue = "38bbbbd9ae2e0150f335e208b05cf09978e542b464a78c2d4952673cd02ea42b";
			JObject jPersistedQuery = new JObject()
			{
				["version"] = 1,
				["sha256Hash"] = hashValue
			};

			JObject jExtensions = new JObject()
			{
				["persistedQuery"] = jPersistedQuery
			};

			JObject jVariables = new JObject()
			{
				["videoID"] = vodId,
				["hasVideoID"] = true
			};

			JObject json = new JObject()
			{
				["operationName"] = "WatchTrackQuery",
				["variables"] = jVariables,
				["extensions"] = jExtensions
			};

			return json;
		}

		public static JObject GeneratePlaybackAccessTokenRequestBody(
			string vodId, string userLogin, bool isVod, bool isLive,
			bool getStreamPlaybackToken, string playerType = "embed")
		{
			if (getStreamPlaybackToken)
			{
				const string queryString = "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, " +
					"$vodID: ID!, $isVod: Boolean!, $playerType: String!) " +
					"{streamPlaybackAccessToken(channelName: $login, params: " +
					"{platform: \"web\", playerBackend: \"mediaplayer\", playerType: $playerType}) " +
					"@include(if: $isLive) {value signature __typename} " +
					"videoPlaybackAccessToken(id: $vodID, params: {platform: \"web\", playerBackend: " +
					"\"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {value signature __typename}}";

				JObject jVariablesLive = new JObject()
				{
					["isLive"] = true,
					["login"] = userLogin,
					["isVod"] = false,
					["vodID"] = "",
					["playerType"] = playerType
				};

				JObject jsonLive = new JObject()
				{
					["operationName"] = "PlaybackAccessToken_Template",
					["query"] = queryString,
					["variables"] = jVariablesLive
				};

				return jsonLive;
			}

			const string hashValue = "0828119ded1c13477966434e15800ff57ddacf13ba1911c129dc2200705b0712";
			JObject jPersistedQuery = new JObject()
			{
				["version"] = 1,
				["sha256Hash"] = hashValue
			};

			JObject jExtensions = new JObject()
			{
				["persistedQuery"] = jPersistedQuery
			};

			JObject jVariables = new JObject()
			{
				["isLive"] = isLive,
				["login"] = userLogin,
				["isVod"] = isVod,
				["vodID"] = vodId,
				["playerType"] = playerType
			};

			JObject json = new JObject()
			{
				["operationName"] = "PlaybackAccessToken",
				["extensions"] = jExtensions,
				["variables"] = jVariables
			};

			return json;
		}

		public static JObject GeneratePlaybackAccessTokenRequestBody(string vodId, string userLogin)
		{
			bool isVod = !string.IsNullOrEmpty(vodId) && !string.IsNullOrWhiteSpace(vodId);
			bool isLogin = !string.IsNullOrEmpty(userLogin) && !string.IsNullOrWhiteSpace(userLogin);
			return GeneratePlaybackAccessTokenRequestBody(vodId, userLogin, isVod, isLogin, false);
		}

		public static JObject GeneratePlaybackAccessTokenRequestBody(string vodId)
		{
			return GeneratePlaybackAccessTokenRequestBody(vodId, string.Empty, true, false, false);
		}

		public static JObject GenerateStreamPlaybackAccessTokenRequestBody(
			string channelName, string playerType = "site")
		{
			return GeneratePlaybackAccessTokenRequestBody(null, channelName, false, true, true, playerType);
		}

		public static string GenerateVodPlaylistManifestUrl(string vodId, JObject vodPlaybackAccessToken)
		{
			string tokenValue = vodPlaybackAccessToken.Value<string>("value");
			string tokenSignature = vodPlaybackAccessToken.Value<string>("signature");

			string usherUrl = string.Format(TWITCH_USHER_VOD_URL_TEMPLATE, vodId);
			Random random = new Random((int)DateTime.UtcNow.Ticks);
			int randomNumber = random.Next(999999);

			NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
			query.Add("player", "twitchweb");
			query.Add("allow_audio_only", "true");
			query.Add("allow_source", "true");
			query.Add("type", "any");
			query.Add("nauth", tokenValue);
			query.Add("nauthsig", tokenSignature);
			query.Add("p", randomNumber.ToString());

			string url = $"{usherUrl}?{query}";
			return url;
		}

		public static string GenerateVodPlaylistManifestUrl(string vodId,
			ITwitchPlaybackAccessToken vodPlaybackAccessToken)
		{
			JObject jsonToken = vodPlaybackAccessToken.GetToken();
			return jsonToken != null ? GenerateVodPlaylistManifestUrl(vodId, jsonToken) : null;
		}

		public static ITwitchPlaybackAccessToken GetChannelPlaybackAccessToken(
			string channelName, Guid deviceId)
		{
			JObject body = GenerateStreamPlaybackAccessTokenRequestBody(channelName);

			/*
			 * Заголовок "Device-ID" нужен для предотвращения показа рекламы в начале видео.
			 * Если этого заголовка нет, твич вставляет 15-секундную рекламную вставку в начало каждого плейлиста.
			 * TODO: Каким-то образом получить правильное значение "deviceId" из API.
			 */
			while (deviceId == Guid.Empty) { deviceId = Guid.NewGuid(); }

			string userAgent = GetUserAgent();

			NameValueCollection headers = new NameValueCollection()
			{
				{ "User-Agent", userAgent },
				{ "Host", "gql.twitch.tv" },
				{ "Client-ID", TWITCH_GQL_CLIENT_ID },
				{ "Content-Type", "text/plain; charset=UTF-8" }
			};
			if (deviceId != null)
			{
				headers.Add("Device-ID", deviceId.ToString());
			}

			int errorCode = Utils.HttpPost(TWITCH_GQL_API_URL, body.ToString(), headers, out string response);
			ITwitchPlaybackAccessToken token = new TwitchStreamPlaybackAccessToken(response, errorCode);
			return token;
		}

		public static ITwitchPlaybackAccessToken GetChannelPlaybackAccessToken(string channelName)
		{
			return GetChannelPlaybackAccessToken(channelName, Guid.Empty);
		}

		public static TwitchGameResult GetVodGameInfo(string vodId)
		{
			JObject body = GenerateVodGameInfoRequestBody(vodId);
			int errorCode = Utils.HttpPost(TWITCH_GQL_API_URL, body.ToString(), out string response);
			if (errorCode == 200)
			{
				TwitchGame game = Utils.ParseGameInfo(response);
				return new TwitchGameResult(game, game == null ? 400 : 200);
			}

			return new TwitchGameResult(TwitchGame.CreateUnknownGame(), 204);
		}

		public static TwitchPlaybackAccessMode GetPlaybackAccessMode(
			string channelName, string vodId, out string errorText)
		{
			errorText = null;
			if (string.IsNullOrEmpty(channelName) || string.IsNullOrWhiteSpace(channelName))
			{
				channelName = string.Empty;
			}

			JObject body = GeneratePlaybackAccessTokenRequestBody(vodId, channelName.ToLower());
			int errorCode = Utils.HttpPost(TWITCH_GQL_API_URL, body.ToString(), out string response);
			if (errorCode == 200)
			{
				ITwitchPlaybackAccessToken playbackAccessToken = !string.IsNullOrEmpty(channelName) ?
					(ITwitchPlaybackAccessToken)new TwitchStreamPlaybackAccessToken(response, 200) :
					(ITwitchPlaybackAccessToken)new TwitchVideoPlaybackAccessToken(response, 200);
				return playbackAccessToken.GetPlaybackAccessMode();
			}

			return TwitchPlaybackAccessMode.Unknown;
		}

		public static TwitchPlaybackAccessMode GetChannelPlaybackAccessMode(
			string channelName, out string errorText)
		{
			return GetPlaybackAccessMode(channelName, string.Empty, out errorText);
		}

		public static TwitchPlaybackAccessMode GetVodPlaybackAccessMode(
			string vodId, out string errorText)
		{
			return GetPlaybackAccessMode(string.Empty, vodId, out errorText);
		}
	}
}

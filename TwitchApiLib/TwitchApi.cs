using System.Collections.Generic;

namespace TwitchApiLib
{
	public static class TwitchApi
	{
		public const string TWITCH_URL = "https://twitch.tv";
		public const string TWITCH_API_HELIX_USERS_ENDPOINT_URL = "https://api.twitch.tv/helix/users";
		public const string TWITCH_API_HELIX_VIDEOS_ENDPOINT_URL = "https://api.twitch.tv/helix/videos";
		public const string TWITCH_API_HELIX_STREAMS_ENDPOINT_URL = "https://api.twitch.tv/helix/streams";

		public const string TWITCH_CLIENT_ID = "gs7pui3law5lsi69yzi9qzyaqvlcsy";

		private static string _userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:143.0) Gecko/20100101 Firefox/143.0";
		private static int _connectionTimeout = 10000;
		private static object _connectionTimeoutLocker = new object();

		public enum TwitchVodType { Undefined, Archive, Highlight, Upload, Unknown }
		public enum TwitchBroadcasterType { Undefined, Affiliate, Partner, Unknown }
		public enum TwitchPlaybackAccessMode { Free, SubscribersOnly, Unknown }

		internal static Dictionary<string, TwitchUser> _twitchUsers = new Dictionary<string, TwitchUser>();
		internal static Dictionary<ulong, TwitchGame> _twitchGames = new Dictionary<ulong, TwitchGame>();

		public static string GetUserAgent()
		{
			lock (_userAgent)
			{
				return _userAgent;
			}
		}

		public static void SetUserAgent(string userAgent)
		{
			lock (_userAgent)
			{
				_userAgent = userAgent;
			}
		}

		public static int GetConnectionTimeout()
		{
			lock (_connectionTimeoutLocker)
			{
				return _connectionTimeout;
			}
		}

		public static void SetConnectionTimeout(int timeout)
		{
			lock (_connectionTimeoutLocker)
			{
				_connectionTimeout = timeout;
			}
		}

		internal static void AddUser(TwitchUser user)
		{
			lock (_twitchUsers)
			{
				if (!_twitchUsers.ContainsKey(user.Login))
				{
					_twitchUsers[user.Login] = user;
				}
			}
		}

		internal static void AddGame(TwitchGame game)
		{
			lock (_twitchGames)
			{
				if (!_twitchGames.ContainsKey(game.Id))
				{
					_twitchGames[game.Id] = game;
				}
			}
		}
	}
}

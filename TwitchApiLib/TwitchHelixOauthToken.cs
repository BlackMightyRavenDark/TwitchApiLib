using System;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace TwitchApiLib
{
	public class TwitchHelixOauthToken
	{
		public const string TWITCH_HELIX_OAUTH_TOKEN_URL = "https://id.twitch.tv/oauth2/token";
		public const string TWITCH_CLIENT_SECRET = "srr2yi260t15ir6w0wq5blir22i9pq";

		public string AccessToken { get; private set; } = null;
		public DateTime ExpirationDate { get; private set; } = DateTime.MinValue;

		public void Reset()
		{
			AccessToken = null;
			ExpirationDate = DateTime.MinValue;
		}

		public int Update(string clientId, string clientSecretKey = null)
		{
			string url = FormatTokenUrl(clientId, clientSecretKey ?? TWITCH_CLIENT_SECRET);
			int errorCode = Utils.HttpPost(url, out string response);
			if (errorCode == 200)
			{
				try
				{
					JObject json = Utils.TryParseJson(response);
					if (json == null)
					{
						AccessToken = null;
						ExpirationDate = DateTime.MinValue;
						return 400;
					}

					AccessToken = json.Value<string>("access_token");
					long expiresIn = json.Value<long>("expires_in");
					ExpirationDate = DateTime.Now.Add(TimeSpan.FromSeconds(expiresIn));
				}
#if DEBUG
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
#else
				catch
				{
#endif
					AccessToken = null;
					ExpirationDate = DateTime.MinValue;
					return 400;
				}
			}

			return errorCode;
		}

		public bool IsExpired()
		{
			return ExpirationDate < DateTime.Now ||
				string.IsNullOrEmpty(AccessToken) || string.IsNullOrWhiteSpace(AccessToken);
		}

		public static string FormatTokenUrl(string clientId, string clientSecretKey)
		{
			NameValueCollection query = System.Web.HttpUtility.ParseQueryString(string.Empty);
			query.Add("client_id", clientId);
			query.Add("client_secret", clientSecretKey);
			query.Add("grant_type", "client_credentials");

			return $"{TWITCH_HELIX_OAUTH_TOKEN_URL}?{query}";
		}
	}
}

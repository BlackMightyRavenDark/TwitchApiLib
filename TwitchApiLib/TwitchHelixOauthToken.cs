using System;
using Newtonsoft.Json.Linq;

namespace TwitchApiLib
{
	public class TwitchHelixOauthToken
	{
		public const string TWITCH_HELIX_OAUTH_TOKEN_URL_TEMPLATE =
			"https://id.twitch.tv/oauth2/token?client_id={0}&client_secret={1}&grant_type=client_credentials";
		public const string TWITCH_CLIENT_SECRET = "srr2yi260t15ir6w0wq5blir22i9pq";

		public string AccessToken { get; private set; } = null;
		public DateTime ExpireDate { get; private set; } = DateTime.MinValue;

		public void Reset()
		{
			AccessToken = null;
			ExpireDate = DateTime.MinValue;
		}

		public int Update(string clientId)
		{
			string url = string.Format(TWITCH_HELIX_OAUTH_TOKEN_URL_TEMPLATE, clientId, TWITCH_CLIENT_SECRET);
			int errorCode = Utils.HttpPost(url, out string response);
			if (errorCode == 200)
			{
				try
				{
					JObject json = Utils.TryParseJson(response);
					if (json == null)
					{
						AccessToken = null;
						ExpireDate = DateTime.MinValue;
						return 400;
					}

					AccessToken = json.Value<string>("access_token");
					long expiresIn = json.Value<long>("expires_in");
					ExpireDate = DateTime.Now.Add(TimeSpan.FromSeconds(expiresIn));
				} catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
					AccessToken = null;
					ExpireDate = DateTime.MinValue;
					return 400;
				}
			}

			return errorCode;
		}

		public bool IsExpired()
		{
			return ExpireDate >= DateTime.Now ||
				string.IsNullOrEmpty(AccessToken) || string.IsNullOrWhiteSpace(AccessToken);
		}
	}
}

using System;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace TwitchApiLib
{
	public class TwitchHelixOauthToken
	{
		public string AccessToken { get; private set; } = null;
		public string AccessTokenType { get; private set; } = null;
		public DateTime ExpirationDate { get; private set; } = DateTime.MinValue;
		public DateTime LastUpdateDate { get; private set; } = DateTime.MinValue;
		public string RawData { get; private set; } = null;

		public const string TWITCH_HELIX_OAUTH_TOKEN_URL = "https://id.twitch.tv/oauth2/token";

		public delegate void TwitchHelixOauthTokenUpdatingDelegate(object sender);
		public delegate void TwitchHelixOauthTokenUpdatedDelegate(object sender, int errorCode, string errorMessage);
		public TwitchHelixOauthTokenUpdatingDelegate TokenUpdating { get; set; }
		public TwitchHelixOauthTokenUpdatedDelegate TokenUpdated { get; set; }

		public void Reset()
		{
			AccessToken = AccessTokenType = RawData = null;
			ExpirationDate = LastUpdateDate = DateTime.MinValue;
		}

		public int Update(string clientId, string secretKey, out string errorMessage)
		{
			TokenUpdating?.Invoke(this);
			DateTime updateStarted = DateTime.UtcNow;
			string url = FormatTokenUrl(clientId, secretKey);
			int errorCode = Utils.HttpPost(url, out string response);
			if (errorCode == 200)
			{
				try
				{
					JObject json = Utils.TryParseJson(response, out errorMessage);
					if (json == null)
					{
						Reset();
						TokenUpdated?.Invoke(this, 400, errorMessage);
						return 400;
					}

					RawData = response;
					AccessToken = json.Value<string>("access_token");
					AccessTokenType = json.Value<string>("token_type");
					long expiresIn = json.Value<long>("expires_in");
					DateTime date = updateStarted.Add(TimeSpan.FromSeconds(expiresIn));
					ExpirationDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, 0, DateTimeKind.Utc);
					LastUpdateDate = updateStarted;

					TokenUpdated?.Invoke(this, errorCode, null);
					return errorCode;
				}
				catch (Exception ex)
				{
					errorMessage = ex.Message;
#if DEBUG
					System.Diagnostics.Debug.WriteLine(errorMessage);
#endif
					Reset();
					TokenUpdated?.Invoke(this, 400, errorMessage);
					return 400;
				}
			}
			else
			{
				errorMessage = response;
			}

			TokenUpdated?.Invoke(this, errorCode, errorMessage);
			return errorCode;
		}

		public int Update(TwitchApplication application, out string errorMessage)
		{
			return Update(application.ClientId, application.ClientSecretKey, out errorMessage);
		}

		public bool IsExpired()
		{
			return ExpirationDate <= DateTime.UtcNow ||
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

		public static string FormatTokenUrl(TwitchApplication application)
		{
			return FormatTokenUrl(application.ClientId, application.ClientSecretKey);
		}
	}
}

using System;
using Newtonsoft.Json.Linq;
using static TwitchApiLib.TwitchApi;

namespace TwitchApiLib
{
	public class TwitchVideoMetadata
	{
		public string RawData { get; }

		private JArray _parsedData;

		public TwitchVideoMetadata(string rawData)
		{
			RawData = rawData;
		}

		public int GetVideoLengthSeconds()
		{
			try
			{
				JArray jsonArr = Parse();
				if (jsonArr == null) { return 0; }

				JObject jVideo = jsonArr[0].Value<JObject>("data")?.Value<JObject>("video");
				if (jVideo != null)
				{
					return jVideo.Value<int>("lengthSeconds");
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

			return 0;
		}

		public TwitchGame GetGameInfo()
		{
			JArray jsonArr = Parse();
			if (jsonArr != null && jsonArr.Count > 0 && jsonArr[0] != null)
			{
				JObject jGame = jsonArr[0].Value<JObject>("data")?.Value<JObject>("video")?.Value<JObject>("game");
				if (jGame != null)
				{
					string idString = jGame.Value<string>("id");
					if (!ulong.TryParse(idString, out ulong id))
					{
						id = 0UL;
					}

					if (_twitchGames.ContainsKey(id) && _twitchGames.TryGetValue(id, out TwitchGame cachedGame))
					{
						return cachedGame;
					}

					string title = jGame.Value<string>("name");
					string displayName = jGame.Value<string>("displayName");
					string boxArtUrl = jGame.Value<string>("boxArtURL");

					if (string.IsNullOrEmpty(displayName))
					{
						displayName = title;
					}

					TwitchGame game = new TwitchGame(title, displayName, id, boxArtUrl, jGame.ToString());
					_twitchGames[id] = game;

					return game;
				}
			}

			return TwitchGame.CreateUnknownGame();
		}

		public JArray Parse(out string errorMessage)
		{
			if (_parsedData == null) { _parsedData = Utils.TryParseJsonArray(RawData, out errorMessage); }
			else { errorMessage = null; }
			return _parsedData;
		}

		public JArray Parse()
		{
			return Parse(out _);
		}
	}
}

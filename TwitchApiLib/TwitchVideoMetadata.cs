using System;
using Newtonsoft.Json.Linq;
using static TwitchApiLib.TwitchApi;

namespace TwitchApiLib
{
	public class TwitchVideoMetadata
	{
		public string RawData { get; }

		public TwitchVideoMetadata(string rawData)
		{
			RawData = rawData;
		}

		public int GetVideoLengthSeconds()
		{
			if (RawData == null) { return 0; }

			try
			{
				JArray jsonArr = Utils.TryParseJsonArray(RawData, out _);
				if (jsonArr != null)
				{
					JObject jVideo = jsonArr[0].Value<JObject>("data")?.Value<JObject>("video");
					if (jVideo != null)
					{
						return jVideo.Value<int>("lengthSeconds");
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}

			return 0;
		}

		public TwitchGame GetGameInfo()
		{
			if (string.IsNullOrEmpty(RawData)) { return null; }

			JArray jsonArr = Utils.TryParseJsonArray(RawData, out _);
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

					lock (_twitchGames)
					{
						if (_twitchGames.ContainsKey(id)) { return _twitchGames[id]; }
					}

					string title = jGame.Value<string>("name");
					string displayName = jGame.Value<string>("displayName");
					string boxArtUrl = jGame.Value<string>("boxArtURL");

					if (string.IsNullOrEmpty(displayName))
					{
						displayName = title;
					}

					TwitchGame game = new TwitchGame(title, displayName, id, boxArtUrl, jGame.ToString());
					AddGame(game);

					return game;
				}
			}

			return TwitchGame.CreateUnknownGame();
		}
	}
}

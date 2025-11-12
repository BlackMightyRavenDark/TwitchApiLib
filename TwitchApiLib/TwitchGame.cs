using System;
using System.IO;
using MultiThreadedDownloaderLib;

namespace TwitchApiLib
{
	public class TwitchGame : IDisposable
	{
		public string Title { get; }
		public string DisplayName { get; }
		public ulong Id { get; }
		public string BoxArtUrl { get; }
		public string ActualThumbnailUrl { get; private set; }
		public bool IsKnown => Id > 0UL;
		public Stream ThumbnailImageData { get; private set; }
		public string RawData { get; }

		public const string GAME_THUMBNAIL_URL_TEMPLATE = "https://static-cdn.jtvnw.net/ttv-boxart/<id>_IGDB-<width>x<height>.jpg";
		public const string NON_GAME_THUMBNAIL_URL_TEMPLATE = "https://static-cdn.jtvnw.net/ttv-boxart/<id>-<width>x<height>.jpg";
		public const string UNKNOWN_GAME_BOXART_URL = "https://static-cdn.jtvnw.net/ttv-boxart/404_boxart.png";

		public TwitchGame(string title, string displayName, ulong id,
			string boxArtUrl, string rawData)
		{
			Title = title;
			DisplayName = displayName;
			Id = id;
			BoxArtUrl = boxArtUrl;
			ActualThumbnailUrl = boxArtUrl;
			RawData = rawData;
		}

		public void Dispose()
		{
			DisposeThumbnailImageData();
		}

		public static TwitchGame CreateUnknownGame()
		{
			return new TwitchGame(null, null, 0UL, UNKNOWN_GAME_BOXART_URL, null);
		}

		public int ReceiveThumbnail(string imageUrl)
		{
			if (ThumbnailImageData != null) { return 200; }

			ActualThumbnailUrl = imageUrl;
			ThumbnailImageData = new MemoryStream();

			int timeout = TwitchApi.GetConnectionTimeout();
			FileDownloader d = new FileDownloader() { Url = imageUrl, ConnectionTimeout = timeout };
			int errorCode = d.Download(ThumbnailImageData);
			d.Dispose();

			if (errorCode != 200) { DisposeThumbnailImageData(); }

			return errorCode;
		}

		public int ReceiveThumbnail(ushort width, ushort height)
		{
			if (ThumbnailImageData != null) { return 200; }

			string url = BoxArtUrl;
			if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url))
			{
				if (IsKnown)
				{
					url = FormatThumbnailTemplateUrl(Id, width, height);
				}
				else
				{
					return 404;
				}
			}
			else
			{
				url = url
					.Replace("{width}", width.ToString())
					.Replace("{height}", height.ToString());
			}

			return ReceiveThumbnail(url);
		}

		public void DisposeThumbnailImageData()
		{
			if (ThumbnailImageData != null)
			{
				ThumbnailImageData.Close();
				ThumbnailImageData = null;
			}
		}

		public static string FormatThumbnailTemplateUrl(ulong gameId, ushort width, ushort height)
		{
			const ulong magicNumber = 509660UL;
			string template = gameId <= magicNumber ? NON_GAME_THUMBNAIL_URL_TEMPLATE : GAME_THUMBNAIL_URL_TEMPLATE;
			return template.Replace("<id>", gameId.ToString())
				.Replace("<width>", width.ToString())
				.Replace("<height>", height.ToString());
		}
	}
}

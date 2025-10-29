using System;

namespace TwitchApiLib
{
	public class TwitchVodChunk
	{
		public int Id { get; }
		public string FileName { get; private set; }
		public string FileUrl { get; }
		public double Offset { get; }
		public double Duration { get; }
		public DateTime CreationDate { get; }
		public bool IsLiveStreamChunk { get; }

		public enum TwitchVodChunkState { NotMuted, Muted, Unmuted };

		public TwitchVodChunk(int id, string fileName, string fileUrl,
			double offset, double duration, DateTime creationDate, bool isLiveStreamChunk)
		{
			Id = id;
			FileName = fileName;
			FileUrl = fileUrl;
			Offset = offset;
			Duration = duration;
			CreationDate = creationDate;
			IsLiveStreamChunk = isLiveStreamChunk;
		}

		public TwitchVodChunk(TwitchVodChunk chunk) :
			this(chunk.Id, chunk.FileName, chunk.FileUrl, chunk.Offset,
				chunk.Duration, chunk.CreationDate, chunk.IsLiveStreamChunk)
		{ }

		public string ExtractNumberFromFileName()
		{
			if (string.IsNullOrEmpty(FileName) || string.IsNullOrWhiteSpace(FileName))
			{
				return null;
			}

			int n = FileName.IndexOf(
				GetState() == TwitchVodChunkState.NotMuted ? ".ts" : "-",
				StringComparison.OrdinalIgnoreCase);
			if (n < 0) { return null; }

			return FileName.Substring(0, n);
		}

		public int GetIdFromFileName()
		{
			string numberString = ExtractNumberFromFileName();
			return !string.IsNullOrEmpty(numberString) &&
				int.TryParse(numberString, out int number) ? number : -1;
		}

		public TwitchVodChunkState GetState()
		{
			if (FileName.EndsWith("-muted.ts"))
			{
				return TwitchVodChunkState.Muted;
			}
			else if (FileName.EndsWith("-unmuted.ts"))
			{
				return TwitchVodChunkState.Unmuted;
			}
			return TwitchVodChunkState.NotMuted;
		}

		public void SetState(TwitchVodChunkState state)
		{
			switch (state)
			{
				case TwitchVodChunkState.Muted:
					FileName = ExtractNumberFromFileName() + "-muted.ts";
					break;

				case TwitchVodChunkState.Unmuted:
					FileName = ExtractNumberFromFileName() + "-unmuted.ts";
					break;

				case TwitchVodChunkState.NotMuted:
					FileName = ExtractNumberFromFileName() + ".ts";
					break;
			}
		}

		public TwitchVodChunkState NextState()
		{
			switch (GetState())
			{
				case TwitchVodChunkState.Muted:
					SetState(TwitchVodChunkState.NotMuted);
					return TwitchVodChunkState.NotMuted;

				case TwitchVodChunkState.NotMuted:
					SetState(TwitchVodChunkState.Unmuted);
					return TwitchVodChunkState.Unmuted;

				case TwitchVodChunkState.Unmuted:
					SetState(TwitchVodChunkState.Muted);
					return TwitchVodChunkState.Muted;
			}
			return TwitchVodChunkState.NotMuted;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using static TwitchApiLib.TwitchVodChunk;

namespace TwitchApiLib
{
	public class TwitchVodMutedSegments
	{
		public List<string> SegmentListFormatted { get; }
		public TimeSpan TotalDuration { get; private set; }
		public List<List<TwitchVodChunk>> Segments { get; }

		public TwitchVodMutedSegments()
		{
			SegmentListFormatted = new List<string>();
			TotalDuration = TimeSpan.Zero;
			Segments = new List<List<TwitchVodChunk>>();
		}

		public void CalculateTotalDuration()
		{
			TotalDuration = TimeSpan.Zero;

			foreach (List<TwitchVodChunk> list in Segments)
			{
				foreach (TwitchVodChunk chunk in list)
				{
					TotalDuration = TotalDuration.Add(TimeSpan.FromSeconds(chunk.Duration));
				}
			}
		}

		public void BuildSegmentList()
		{
			SegmentListFormatted.Clear();
			foreach (List<TwitchVodChunk> list in Segments)
			{
				string t = SegmentToString(list);
				SegmentListFormatted.Add(t);
			}
		}

		private string SegmentToString(List<TwitchVodChunk> segment)
		{
			double segmentDuration = segment.Sum(item => item.Duration);
			TimeSpan start = TimeSpan.FromSeconds(segment[0].Offset);
			TimeSpan end = TimeSpan.FromSeconds(segment[0].Offset + segmentDuration);
			return $"{start:hh':'mm':'ss} - {end:hh':'mm':'ss}";
		}

		public static TwitchVodMutedSegments ParseMutedSegments(List<TwitchVodChunk> chunkList)
		{
			TwitchVodMutedSegments result = new TwitchVodMutedSegments();
			List<TwitchVodChunk> segmentList = null;
			for (int i = 0; i < chunkList.Count; ++i)
			{
				if (chunkList[i].GetState() != TwitchVodChunkState.NotMuted)
				{
					if (segmentList == null)
					{
						segmentList = new List<TwitchVodChunk>();
					}

					TwitchVodChunk chunk = new TwitchVodChunk(
						chunkList[i].FileName, chunkList[i].Offset, chunkList[i].Duration);
					segmentList.Add(chunk);
				}
				else if (segmentList != null)
				{
					result.Segments.Add(segmentList);
					segmentList = null;
				}
			}

			if (segmentList != null)
			{
				result.Segments.Add(segmentList);
			}

			return result;
		}

		public void Clear()
		{
			Segments.Clear();
			SegmentListFormatted.Clear();
			TotalDuration = TimeSpan.Zero;
		}

		public override string ToString()
		{
			string t = string.Empty;
			if (SegmentListFormatted.Count > 0)
			{
				foreach (string segment in SegmentListFormatted)
				{
					t += segment + Environment.NewLine;
				}
			}
			return t;
		}
	}
}

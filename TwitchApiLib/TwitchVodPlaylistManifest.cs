using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace TwitchApiLib
{
    public class TwitchVodPlaylistManifest
    {
        public string ManifestRaw { get; }

        public int Count => _manifestItemList != null ? _manifestItemList.Count : 0;

        public TwitchVodPlaylistManifestItem this[string formatId] { get { return FindManifestItemWithFormatId(formatId); } }
        public TwitchVodPlaylistManifestItem this[int id] { get { return _manifestItemList[id]; } }

        private List<TwitchVodPlaylistManifestItem> _manifestItemList;

        public TwitchVodPlaylistManifest(string manifestRaw)
        {
            ManifestRaw = manifestRaw;
            _manifestItemList = Parse();
        }

        private List<TwitchVodPlaylistManifestItem> Parse()
        {
            if (string.IsNullOrEmpty(ManifestRaw) || string.IsNullOrWhiteSpace(ManifestRaw))
            {
                return null;
            }

            List<string> fixedList = GetFixedList();
            if (fixedList.Count < 2) { return null; }

            _manifestItemList = new List<TwitchVodPlaylistManifestItem>();
            int max = fixedList.Count - 1;
            for (int i = 0; i < max; i += 2)
            {
                string str = fixedList[i].Substring(fixedList[i].IndexOf(':') + 1);
                Dictionary<string, string> dict = SplitStringToKeyValues(str, ",", '=');
                int bandwidth = dict.ContainsKey("BANDWIDTH") ? int.Parse(dict["BANDWIDTH"]) : 0;
                int videoWidth = 0;
                int videoHeight = 0;
                if (dict.ContainsKey("RESOLUTION"))
                {
                    string res = dict["RESOLUTION"];
                    string[] resSplitted = res.Split('x');
                    if (resSplitted.Length == 2)
                    {
                        if (!int.TryParse(resSplitted[0], out videoWidth))
                        {
                            videoWidth = 0;
                        }
                        if (!int.TryParse(resSplitted[1], out videoHeight))
                        {
                            videoHeight = 0;
                        }
                    }
                }

                string codecs = dict.ContainsKey("CODECS") ? RemoveQuotes(HttpUtility.UrlDecode(dict["CODECS"])) : null;
                string formatId = dict.ContainsKey("VIDEO") ? RemoveQuotes(dict["VIDEO"]) : null;
                int frameRate = 0;
                if (dict.ContainsKey("FRAME-RATE"))
                {
                    string rate = dict["FRAME-RATE"];
                    if (!string.IsNullOrEmpty(rate) && !string.IsNullOrWhiteSpace(rate))
                    {
                        if (!double.TryParse(rate.Replace('.', ','), out double rateDouble))
                        {
                            rateDouble = 0.0;
                        }
                        frameRate = (int)Math.Round(rateDouble);
                    }
                }
                string url = fixedList[i + 1];

                TwitchVodPlaylistManifestItem manifestItem = new TwitchVodPlaylistManifestItem(
                    videoWidth, videoHeight, bandwidth, frameRate, codecs, formatId, url);
                _manifestItemList.Add(manifestItem);
            }

            return _manifestItemList;
        }

        public void SortByBandwidth()
        {
            if (Count > 1)
            {
                _manifestItemList.Sort((x, y) =>
                {
                    if (x.Bandwidth == 0 || y.Bandwidth == 0)
                    {
                        if (x.FormatId == "chunked") { return -1; }
                        else if (y.FormatId == "chunked") { return 1; }
                        else { return 0; }
                    }

                    return x.Bandwidth > y.Bandwidth ? -1 : 1;
                });
            }
        }

        public void SortByVideoHeightOrFrameRate()
        {
            if (Count > 1)
            {
                _manifestItemList.Sort((x, y) =>
                {
                    if (x.ResolutionHeight == y.ResolutionHeight)
                    {
                        return x.FrameRate > y.FrameRate ? -1 : 1;
                    }
                    return x.ResolutionHeight > y.ResolutionHeight ? -1 : 1;
                });
            }
        }

        private List<string> GetFixedList()
        {
            string manifest = ManifestRaw;
            Regex regex = new Regex("CODECS=\"(.+?)\"");
            MatchCollection matches = regex.Matches(manifest);
            for (int i = matches.Count - 1; i >= 0; --i)
            {
                Match match = matches[i];

                int len = 7;
                string tmp = match.Value.Substring(len);
                int actualPos = match.Index + len;
                int actualLength = match.Length - len;
                string encoded = HttpUtility.UrlEncode(tmp);
                manifest = manifest.Remove(actualPos, actualLength).Insert(actualPos, encoded);
            }

            List<string> resultList = new List<string>();
            string[] strings = manifest.Split('\n');
            foreach (string str in strings)
            {
                if (str.StartsWith("#EXT-X-STREAM-INF:") || str.StartsWith("http"))
                {
                    resultList.Add(str);
                }
            }
            return resultList;
        }

        private TwitchVodPlaylistManifestItem FindManifestItemWithFormatId(string formatId)
        {
            foreach (TwitchVodPlaylistManifestItem item in _manifestItemList)
            {
                if (item.FormatId == formatId)
                {
                    return item;
                }
            }
            return null;
        }

        private static string RemoveQuotes(string str)
        {
            if (str.Length <= 1) { return str; }
            string s = str;
            if (s[0] == '\"' && s.Length > 1) { s = s.Substring(1); }
            if (s.Length > 0 && s[s.Length - 1] == '"') { s = s.Substring(0, s.Length - 1); }
            return s;
        }

        private static Dictionary<string, string> SplitStringToKeyValues(
            string inputString, string keySeparator, char valueSeparator)
        {
            if (string.IsNullOrEmpty(inputString) || string.IsNullOrWhiteSpace(inputString))
            {
                return null;
            }
            string[] keyValues = inputString.Split(new string[] { keySeparator }, StringSplitOptions.None);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < keyValues.Length; ++i)
            {
                string[] t = keyValues[i].Split(valueSeparator);
                string value = t.Length > 1 ? t[1] : string.Empty;
                dict[t[0]] = value;
            }
            return dict;
        }

        public override string ToString()
        {
            string t = string.Empty;
            for (int i = 0; i < Count; ++i)
            {
                t += this[i] + Environment.NewLine;
            }
            return t;
        }
    }
}

﻿namespace RoliSoft.TVShowTracker.Parsers.Downloads.Engines.Torrent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping BroadcasTheNet.
    /// </summary>
    [Parser("RoliSoft", "2011-02-10 8:13 AM"), TestFixture]
    public class BroadcasTheNet : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "BroadcasTheNet";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "https://broadcasthe.net/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://broadcasthe.net/favicon.ico";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires authentication.
        /// </summary>
        /// <value><c>true</c> if requires authentication; otherwise, <c>false</c>.</value>
        public override bool Private
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the names of the required cookies for the authentication.
        /// </summary>
        /// <value>The required cookies for authentication.</value>
        public override string[] RequiredCookies
        {
            get
            {
                return new[] { "keeplogged" };
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override IEnumerable<Link> Search(string query)
        {
            var html  = Utils.GetHTML(Site + "torrents.php?searchstr=" + Uri.EscapeUriString(query), cookies: Cookies);
            var links = html.DocumentNode.SelectNodes("//table[@id='torrent_table']/tr[not(position() = 1)]/td[3]");

            if (links == null)
            {
                yield break;
            }

            foreach (var node in links)
            {
                var link = new Link(this);

                var release = Regex.Match(node.InnerHtml, @"<b>Release Name</b>: ([^<$]+)");
                var quality = node.GetTextValue("a[2]/following-sibling::text()").Trim();

                if (release.Success && !release.Groups[1].Value.Trim().Contains("Not Available"))
                {
                    link.Release = release.Groups[1].Value.Trim();
                    link.Quality = ThePirateBay.ParseQuality(link.Release);
                }
                else
                {
                    link.Release = HtmlEntity.DeEntitize(node.GetTextValue("a[1]") + " " + node.GetTextValue("a[2]") + " " + quality.Replace("[", string.Empty).Replace("]", string.Empty).Replace(" / ", " "));
                }

                if (link.Quality == Qualities.Unknown)
                {
                    link.Quality = ParseQuality(quality);
                }

                link.URL     = Site + node.GetNodeAttributeValue("span/a[contains(@href, 'action=download')]", "href");
                link.Size    = node.GetTextValue("../td[5]").Trim();

                yield return link;
            }
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Qualities ParseQuality(string release)
        {
            if (IsMatch(release, @"1080(i|p)", @"WEB"))
            {
                return Qualities.WebDL1080p;
            }
            if (IsMatch(release, @"1080(i|p)", @"(Bluray|BD|HDDVD)"))
            {
                return Qualities.BluRay1080p;
            }
            if (IsMatch(release, @"1080(i|p)", @"HDTV"))
            {
                return Qualities.HDTV1080i;
            }
            if (IsMatch(release, @"720p", @"WEB"))
            {
                return Qualities.WebDL720p;
            }
            if (IsMatch(release, @"720p", @"(Bluray|BD|HDDVD)"))
            {
                return Qualities.BluRay720p;
            }
            if (IsMatch(release, @"720p", @"HDTV"))
            {
                return Qualities.HDTV720p;
            }
            if (IsMatch(release, @"(x264|h.264|MKV)"))
            {
                return Qualities.HRx264;
            }
            if (IsMatch(release, @"(HDTV|DSR|DVDRip)"))
            {
                return Qualities.HDTVXviD;
            }
            if (IsMatch(release, @"TVRip"))
            {
                return Qualities.TVRip;
            }

            return Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified input is matches all the specified regexes.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="regexes">The regexes.</param>
        /// <returns>
        /// 	<c>true</c> if the specified input matches all the specified regexes; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string input, params string[] regexes)
        {
            return regexes.All(regex => Regex.IsMatch(input, regex, RegexOptions.IgnoreCase));
        }
    }
}

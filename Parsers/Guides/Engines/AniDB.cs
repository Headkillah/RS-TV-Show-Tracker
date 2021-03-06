﻿namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for the AniDB HTTP API.
    /// </summary>
    [TestFixture]
    public class AniDB : Guide
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "AniDB";
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
                return "http://anidb.net/";
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
                return "pack://application:,,,/RSTVShowTracker;component/Images/anidb.png";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-07-19 8:12 PM");
            }
        }

        /// <summary>
        /// Gets the list of supported languages.
        /// </summary>
        /// <value>The list of supported languages.</value>
        public override string[] SupportedLanguages
        {
            get
            {
                return Languages.List.Keys.ToArray();
            }
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>ID.</returns>
        public override IEnumerable<ShowID> GetID(string name, string language = "en")
        {
            var list = Utils.GetXML("http://anisearch.outrance.pl/?task=search&query=" + Utils.EncodeURL(name));

            foreach (var show in list.Descendants("anime"))
            {
                var id = new ShowID(this);
                
                try
                {
                    id.Title = show.Descendants("title").First(t => t.Attribute("lang").Value == "en").Value;
                    id.Language = "en";
                }
                catch
                {
                    try
                    {
                        id.Title = show.Descendants("title").First(t => t.Attribute("lang").Value == language).Value;
                        id.Language = language;
                    }
                    catch
                    {
                        id.Title = show.GetValue("title");
                        id.Language = "en";
                    }
                }

                id.ID  = show.Attribute("aid").Value;
                id.URL = Site + "perl-bin/animedb.pl?show=anime&aid=" + id.ID;

                yield return id;
            }
        }

        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <param name="language">The preferred language of the data.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id, string language = "en")
        {
            var info = Utils.GetXML("http://api.anidb.net:9001/httpapi?request=anime&client=rstvshowtracker&clientver=2&protover=1&aid=" + id);
            var show = new TVShow();

            try { show.Title = info.Descendants("title").First(t => t.Attributes().First().Value == language).Value; }
            catch
            {
                try   { show.Title = info.Descendants("title").First(t => t.Attributes().First().Value == "en").Value; }
                catch { show.Title = info.GetValue("title"); }
            }
            
            show.Source      = GetType().Name;
            show.SourceID    = id;
            show.Description = info.GetValue("description");
            show.Genre       = info.Descendants("category").Aggregate(string.Empty, (current, g) => current + (g.GetValue("name") + ", ")).TrimEnd(", ".ToCharArray());
            show.Airing      = string.IsNullOrWhiteSpace(info.GetValue("enddate"));
            show.Runtime     = info.GetValue("length").ToInteger();
            show.TimeZone    = "Tokyo Standard Time";
            show.Language    = language;
            show.URL         = Site + "perl-bin/animedb.pl?show=anime&aid=" + id;
            show.Episodes    = new List<Episode>();

            show.Cover = info.GetValue("picture");
            if (!string.IsNullOrWhiteSpace(show.Cover))
            {
                show.Cover = "http://img7.anidb.net/pics/anime/" + show.Cover;
            }

            foreach (var node in info.Descendants("episode"))
            {
                try { node.GetValue("epno").ToInteger(); }
                catch
                {
                    continue;
                }

                var ep = new Episode();

                ep.Season = 1;
                ep.Number = node.GetValue("epno").ToInteger();
                ep.URL    = Site + "perl-bin/animedb.pl?show=ep&eid=" + node.Attribute("id").Value;

                try { ep.Title = node.Descendants("title").First(t => t.Attributes().First().Value == language).Value; }
                catch
                {
                    try   { ep.Title = node.Descendants("title").First(t => t.Attributes().First().Value == "en").Value; }
                    catch { ep.Title = node.GetValue("title"); }
                }

                DateTime dt;
                ep.Airdate = DateTime.TryParse(node.GetValue("airdate"), out dt)
                           ? dt
                           : Utils.UnixEpoch;

                show.Episodes.Add(ep);
            }

            show.Episodes = show.Episodes.OrderBy(e => e.Number).ToList();

            if (show.Episodes.Count != 0)
            {
                show.AirDay = show.Episodes.Last().Airdate.DayOfWeek.ToString();
            }

            return show;
        }
    }
}

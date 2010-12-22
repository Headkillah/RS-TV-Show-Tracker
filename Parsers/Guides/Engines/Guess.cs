﻿namespace RoliSoft.TVShowTracker.Parsers.Guides.Engines
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RoliSoft.TVShowTracker.Helpers;

    /// <summary>
    /// Provides support for guessing episode list using download links.
    /// </summary>
    public class Guess : Guide
    {
        /// <summary>
        /// Extracts the data available in the database.
        /// </summary>
        /// <param name="id">The ID of the show.</param>
        /// <returns>TV show data.</returns>
        public override TVShow GetData(string id)
        {
            // create an average TV show
            var show = new TVShow
                {
                    Title    = id,
                    Runtime  = 30,
                    AirTime  = "20:00",
                    Airing   = true, // otherwise it won't update
                    Episodes = new List<TVShow.Episode>()
                };

            // search for links
            var search = new DownloadSearch();
            var links  = search.Search(id);

            // get the highest value for season and episode
            var snr = 0;
            var enr = 0;

            foreach (var link in links)
            {
                var ep = ShowNames.ExtractEpisode(link.Release);

                if (ep != null)
                {
                    if (ep.Season > snr)
                    {
                        snr = ep.Season;
                    }
                    if (ep.Episode > enr)
                    {
                        enr = ep.Episode;
                    }
                }
            }

            if (snr == 0 || enr == 0)
            {
                return show;
            }

            // create the episode listing
            for (var s = 1; s <= snr; s++)
            {
                for (var e = 1; e <= enr; e++)
                {
                    show.Episodes.Add(new TVShow.Episode
                    {
                        Season  = s,
                        Number  = e,
                        Title   = "Season " + s + ", Episode " + e,
                        Airdate = new DateTime(DateTime.Now.Year - (snr - s), (int)Math.Ceiling(e / 10d), int.Parse((e / 10d).ToString("0.0").Split('.').Last()) + 1, 0, 0, 0, 0) // I take the guesswork waaay too far...
                    });
                }
            }

            return show;
        }

        /// <summary>
        /// Gets the ID of a TV show in the database.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ID.</returns>
        public override string GetID(string name)
        {
            return name;
        }
    }
}
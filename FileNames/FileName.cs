﻿namespace RoliSoft.TVShowTracker.FileNames
{
    using System.IO;

    using RoliSoft.TVShowTracker.ShowNames;

    /// <summary>
    /// Represents a TV show video file.
    /// </summary>
    public class ShowFile
    {
        /// <summary>
        /// Gets or sets the original name of the file.
        /// </summary>
        /// <value>The file name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the original extension of the file.
        /// </summary>
        /// <value>The extension.</value>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the quality of the file.
        /// </summary>
        /// <value>The quality.</value>
        public string Quality { get; set; }

        /// <summary>
        /// Gets or sets the name of the show.
        /// </summary>
        /// <value>The show.</value>
        public string Show { get; set; }

        /// <summary>
        /// Gets or sets the season of the episode.
        /// </summary>
        /// <value>The season.</value>
        public int Season { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        /// <value>The episode.</value>
        public int Episode { get; set; }

        /// <summary>
        /// Gets or sets the second episode number.
        /// </summary>
        /// <value>The second episode.</value>
        public int? SecondEpisode { get; set; }

        /// <summary>
        /// Gets or sets the title of the episode.
        /// </summary>
        /// <value>The episode title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        public ShowFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        /// <param name="location">The location of the file.</param>
        public ShowFile(string location)
        {
            Name      = Path.GetFileName(location);
            Extension = Path.GetExtension(Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowFile"/> class.
        /// </summary>
        /// <param name="name">The name of the original file.</param>
        /// <param name="show">The name of the show.</param>
        /// <param name="ep">The parsed season and episode.</param>
        /// <param name="title">The title of the episode.</param>
        /// <param name="quality">The quality of the file.</param>
        public ShowFile(string name, string show, ShowEpisode ep, string title, string quality)
        {
            Name          = name;
            Extension     = Path.GetExtension(Name);
            Show          = show;
            Season        = ep.Season;
            Episode       = ep.Episode;
            SecondEpisode = ep.SecondEpisode;
            Title         = title;
            Quality       = quality;
        }
    }
}
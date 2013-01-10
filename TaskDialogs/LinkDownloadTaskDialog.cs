﻿namespace RoliSoft.TVShowTracker.TaskDialogs
{
    using System;
    using System.IO;
    using System.Threading;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    using TaskDialogInterop;

    using RoliSoft.TVShowTracker.Downloaders;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    /// <summary>
    /// Provides a <c>TaskDialog</c> frontend to the <c>DownloadLinksPage</c> links.
    /// </summary>
    public class LinkDownloadTaskDialog
    {
        private IDownloader _dl;
        private volatile bool _active;
        private string _tdstr, _tdtit;
        private int _tdpos;
        
        /// <summary>
        /// Downloads the specified link.
        /// </summary>
        /// <param name="link">The link.</param>
        /// <param name="token">The token.</param>
        public void Download(Link link, string token)
        {
            if (link.FileURL.StartsWith("magnet:"))
            {
                DownloadFileCompleted(null, new EventArgs<string, string, string>(link.FileURL, null, token));
                return;
            }

            _active = true;
            _tdtit = link.Release;
            _tdstr = "Sending request to " + new Uri(link.FileURL).DnsSafeHost.Replace("www.", string.Empty) + "...";
            var showmbp = false;
            var mthd = new Thread(() => TaskDialog.Show(new TaskDialogOptions
                {
                    Title                   = "Downloading...",
                    MainInstruction         = _tdtit,
                    Content                 = _tdstr,
                    CustomButtons           = new[] { "Cancel" },
                    ShowMarqueeProgressBar  = true,
                    ShowProgressBar         = true,
                    EnableCallbackTimer     = true,
                    AllowDialogCancellation = true,
                    Callback                = (dialog, args, data) =>
                        {
                            if (!showmbp && _tdpos == 0)
                            {
                                dialog.SetProgressBarMarquee(true, 0);

                                showmbp = true;
                            }

                            if (_tdpos > 0 && showmbp)
                            {
                                dialog.SetMarqueeProgressBar(false);
                                dialog.SetProgressBarState(VistaProgressBarState.Normal);
                                dialog.SetProgressBarPosition(_tdpos);

                                showmbp = false;
                            }

                            if (_tdpos > 0)
                            {
                                dialog.SetProgressBarPosition(_tdpos);
                            }

                            dialog.SetContent(_tdstr);

                            if (args.ButtonId != 0)
                            {
                                if (_active)
                                {
                                    try { _dl.CancelAsync(); } catch { }
                                }

                                return false;
                            }

                            if (!_active)
                            {
                                dialog.ClickButton(500);
                                return false;
                            }

                            return true;
                        }
                }));
            mthd.SetApartmentState(ApartmentState.STA);
            mthd.Start();

            _dl                          = link.Source.Downloader;
            _dl.DownloadFileCompleted   += DownloadFileCompleted;
            _dl.DownloadProgressChanged += (s, a) =>
                {
                    _tdstr = "Downloading file... ({0}%)".FormatWith(a.Data);
                    _tdpos = a.Data;
                };

            _dl.Download(link, Path.Combine(Path.GetTempPath(), Utils.CreateSlug(link.Release + " " + link.Source.Name + " " + Utils.Rand.Next().ToString("x"), false) + (link.Source.Type == Types.Torrent ? ".torrent" : link.Source.Type == Types.Usenet ? ".nzb" : string.Empty)), !string.IsNullOrWhiteSpace(token) ? token : "DownloadFile");

            Utils.Win7Taskbar(state: TaskbarProgressBarState.Indeterminate);
        }

        /// <summary>
        /// Handles the DownloadFileCompleted event of the HTTPDownloader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DownloadFileCompleted(object sender, EventArgs<string, string, string> e)
        {
            _active = false;

            Utils.Win7Taskbar(state: TaskbarProgressBarState.NoProgress);

            if (e.First == null && e.Second == null)
            {
                TaskDialog.Show(new TaskDialogOptions
                    {
                        MainIcon                = VistaTaskDialogIcon.Error,
                        Title                   = "Download error",
                        MainInstruction         = _tdtit,
                        Content                 = "There was an error while downloading the requested file." + Environment.NewLine + "Try downloading another file from the list.",
                        AllowDialogCancellation = true,
                        CustomButtons           = new[] { "OK" }
                    });

                return;
            }

            switch (e.Third)
            {
                case "DownloadFile":
                    var sfd = new SaveFileDialog
                        {
                            CheckPathExists = true,
                            FileName        = e.Second
                        };

                    if (sfd.ShowDialog().Value)
                    {
                        if (File.Exists(sfd.FileName))
                        {
                            File.Delete(sfd.FileName);
                        }

                        File.Move(e.First, sfd.FileName);
                    }
                    else
                    {
                        File.Delete(e.First);
                    }
                    break;

                case "SendToAssociated":
                    Utils.Run(e.First);
                    break;

                case "SendToTorrent":
                    Utils.Run(Settings.Get("Torrent Downloader"), e.First);
                    break;
            }
        }
    }
}

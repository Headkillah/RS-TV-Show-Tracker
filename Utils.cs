﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media.Imaging;

    using HtmlAgilityPack;

    using Microsoft.Win32;
    using Microsoft.WindowsAPICodePack.Taskbar;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>
        /// Gets the Unix epoch date. (1970-01-01 00:00:00)
        /// </summary>
        /// <value>The Unix epoch.</value>
        public static DateTime UnixEpoch
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the operating system is Windows 7 or newer.
        /// </summary>
        /// <value><c>true</c> if the OS is Windows 7 or newer; otherwise, <c>false</c>.</value>
        public static bool Is7
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                     ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1) ||
                       Environment.OSVersion.Version.Major >= 6);
            }
        }

        /// <summary>
        /// Gets the name of the operating system.
        /// </summary>
        /// <value>The OS.</value>
        public static string OS
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32S:
                        return "Windows 3.1";

                    case PlatformID.Win32Windows:
                        switch (Environment.OSVersion.Version.Minor)
                        {
                            case 0:
                                return "Windows 95";

                            case 10:
                                return Environment.OSVersion.Version.Revision.ToString() == "2222A"
                                       ? "Windows 98 Second Edition"
                                       : "Windows 98";

                            case 90:
                                return "Windows ME";
                        }
                        break;

                    case PlatformID.Win32NT:
                        switch (Environment.OSVersion.Version.Major)
                        {
                            case 3:
                                return "Windows NT 3.51";

                            case 4:
                                return "Windows NT 4.0";

                            case 5:
                                switch (Environment.OSVersion.Version.Minor)
                                {
                                    case 0:
                                        return "Windows 2000";

                                    case 1:
                                        return "Windows XP";

                                    case 2:
                                        return "Windows 2003";
                                }
                                break;

                            case 6:
                                switch (Environment.OSVersion.Version.Minor)
                                {
                                    case 0:
                                        return "Windows Vista";

                                    case 1:
                                        return "Windows 7";
                                }
                                break;
                        }
                        break;

                    case PlatformID.WinCE:
                        return "Windows CE";

                    case PlatformID.Unix:
                        return "Unix";
                }

                return "Unknown OS";
            }
        }

        /// <summary>
        /// Appends a unit to a number and makes it plural if the number is not 1.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <param name="unit">The unit.</param>
        /// <returns>Formatted number.</returns>
        public static string FormatNumber(int number, string unit)
        {
            return number + " " + unit + (number != 1 ? "s" : string.Empty);
        }

        /// <summary>
        /// Runs the specified process.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="arguments">The arguments.</param>
        public static void Run(string process, string arguments = null)
        {
            try { Process.Start(process, arguments); } catch { }
        }

        /// <summary>
        /// Runs the specified process, waits until it finishes and returns the console content.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="elevate">if set to <c>true</c> the process will be elevated if the invoker is not under admin.</param>
        /// <returns>Console output.</returns>
        public static string RunAndRead(string process, string arguments = null, bool elevate = false)
        {
            var sb = new StringBuilder();
            var p  = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo           =
                        {
                            FileName               = process,
                            Arguments              = arguments ?? string.Empty,
                            UseShellExecute        = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            CreateNoWindow         = true
                        }
                };

            if (elevate)
            {
                if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    p.StartInfo.Verb = "runas";
                }
            }

            p.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sb.AppendLine(e.Data);
                    }
                };
            p.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sb.AppendLine(e.Data);
                    }
                };

            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            }
            catch (Win32Exception) { }

            return sb.ToString();
        }

        /// <summary>
        /// Downloads the specified URL and parses it with HtmlAgilityPack.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <returns>Remote page's parsed content.</returns>
        public static HtmlDocument GetHTML(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, Dictionary<string, string> headers = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(
                GetURL(url, postData, cookies, encoding, autoDetectEncoding, userAgent, headers)
            );

            return doc;
        }

        /// <summary>
        /// Downloads the specified URL into a string.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The data to POST.</param>
        /// <param name="cookies">The cookies.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="autoDetectEncoding">if set to <c>true</c> it will automatically detect the encoding. Not guaranteed to work.</param>
        /// <param name="userAgent">The user agent to send.</param>
        /// <param name="headers">The additional headers to send.</param>
        /// <returns>Remote page's content.</returns>
        public static string GetURL(string url, string postData = null, string cookies = null, Encoding encoding = null, bool autoDetectEncoding = false, string userAgent = null, Dictionary<string, string> headers = null)
        {
            ServicePointManager.Expect100Continue = false;

            var req       = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout   = 10000;
            req.UserAgent = userAgent ?? "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.7.39 Version/11.00";

            req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            if (!string.IsNullOrWhiteSpace(postData))
            {
                req.Method                    = "POST";
                req.ContentType               = "application/x-www-form-urlencoded";
                req.AllowWriteStreamBuffering = true;
            }

            if (!string.IsNullOrWhiteSpace(cookies))
            {
                req.CookieContainer = new CookieContainer();

                foreach (var kv in Regex.Replace(cookies.TrimEnd(';'), @";\s*", ";")
                                   .Split(';')
                                   .Where(cookie => cookie != null)
                                   .Select(cookie => cookie.Split('=')))
                {
                    req.CookieContainer.Add(new Cookie(kv[0], kv[1], "/", new Uri(url).Host));
                }
            }

            if (headers != null && headers.Count != 0)
            {
                foreach (var header in headers)
                {
                    req.Headers[header.Key] = header.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(postData))
            {
                using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                {
                    sw.Write(postData);
                    sw.Flush();
                }
            }

            var resp = (HttpWebResponse)req.GetResponse();
            var rstr = resp.GetResponseStream();

            if (resp.ContentEncoding.ToUpper().Contains("GZIP"))
            {
                rstr = new GZipStream(rstr, CompressionMode.Decompress);
            }
            else if (resp.ContentEncoding.ToUpper().Contains("DEFLATE"))
            {
                rstr = new DeflateStream(rstr, CompressionMode.Decompress);
            }

            if (!autoDetectEncoding)
            {
                using (var sr = new StreamReader(rstr, encoding ?? Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
            else
            {
                var ms = new MemoryStream();
                byte[] bs;

                int read;
                do
                {
                    bs = new byte[8192];
                    read = rstr.Read(bs, 0, bs.Length);
                    ms.Write(bs, 0, read);
                } while (read > 0);

                bs = ms.ToArray();

                var rgx = Regex.Match(Encoding.ASCII.GetString(bs), @"charset=([^""]+)", RegexOptions.IgnoreCase);
                var eenc = "utf-8";

                if (rgx.Success)
                {
                    eenc = rgx.Groups[1].Value;

                    if (eenc == "iso-8859-1") // .NET won't recognize iso-8859-1
                    {
                        eenc = "windows-1252";
                    }
                }

                return Encoding.GetEncoding(eenc).GetString(bs);
            }
        }

        /// <summary>
        /// Modify the specified URL to go through Coral CDN.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static string Coralify(string url)
        {
            return Regex.Replace(url, @"(/{2}[^/]+)/", @"$1.nyud.net/");
        }

        /// <summary>
        /// Gets the unique user identifier or generates one if absent.
        /// </summary>
        /// <returns>Unique ID.</returns>
        public static string GetUUID()
        {
            if (string.IsNullOrWhiteSpace(Signature.FullPath))
            {
                return string.Empty;
            }

            var uid = Database.Setting("uid");

            if (string.IsNullOrWhiteSpace(uid))
            {
                uid = Guid.NewGuid().ToString();
                Database.Setting("uid", uid);
            }

            return uid;
        }

        /// <summary>
        /// Gets the full path to a random file.
        /// </summary>
        /// <param name="extension">The extension of the file.</param>
        /// <returns>Full path to random file.</returns>
        public static string GetRandomFileName(string extension = null)
        {
            return Path.GetTempPath() + Path.PathSeparator + Path.GetRandomFileName() + (extension != null ? "." + extension : string.Empty);
        } 
        
        /// <summary>
        /// Gets the size of the file in human-readable format.
        /// </summary>
        /// <param name="bytes">The size.</param>
        /// <returns>Transformed file size.</returns>
        public static string GetFileSize(long bytes)
        {
            var size = "0 bytes";

            if (bytes >= 1073741824.0)
            {
                size = String.Format("{0:0.00}", bytes / 1073741824.0) + " GB";
            }
            else if (bytes >= 1048576.0)
            {
                size = String.Format("{0:0.00}", bytes / 1048576.0) + " MB";
            }
            else if (bytes >= 1024.0)
            {
                size = String.Format("{0:0.00}", bytes / 1024.0) + " kB";
            }
            else if (bytes > 0 && bytes < 1024.0)
            {
                size = bytes + " bytes";
            }

            return size;
        }

        /// <summary>
        /// Replaces UTF-8 characters with \uXXXX format.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>Pure ASCII text.</returns>
        public static string EscapeUTF8(string text)
        {
            return Regex.Replace(text, @"[^\u0000-\u007F]", m => string.Format(@"\u{0:x4}", (int)m.Value[0]));
        }

        /// <summary>
        /// Sets the application's progress bar state and/or progress on the Windows 7 taskbar.
        /// </summary>
        /// <param name="progress">The progress of the progress bar from 1 to 100.</param>
        /// <param name="state">The state of the progress bar behind the icon.</param>
        public static void Win7Taskbar(int? progress = null, TaskbarProgressBarState? state = null)
        {
            MainWindow.Active.Dispatcher.Invoke((Action)(() =>
                {
                    if (!MainWindow.Active.IsVisible)
                    {
                        return;
                    }

                    if (state.HasValue)
                    {
                        TaskbarManager.Instance.SetProgressState(state.Value, MainWindow.Active);
                    }

                    if (progress.HasValue)
                    {
                        TaskbarManager.Instance.SetProgressValue(progress.Value, 100, MainWindow.Active);
                    }
                }));
        }

        /// <summary>
        /// Gets the default application for the specified extension.
        /// </summary>
        /// <param name="extension">The extension with a leading dot.</param>
        /// <returns>The path of the associated application.</returns>
        public static string GetApplicationForExtension(string extension)
        {
            // get prog id

            var extkey = Registry.ClassesRoot.OpenSubKey(extension);

            if (extkey == null)
            {
                return string.Empty;
            }

            var appid = extkey.GetValue(null) as string;

            if (appid == null)
            {
                return string.Empty;
            }

            extkey.Close();

            // get application

            var appkey = Registry.ClassesRoot.OpenSubKey(appid + @"\shell\open\command");

            if (appkey == null)
            {
                return string.Empty;
            }

            var cmd = appkey.GetValue(null) as string;

            if (cmd == null)
            {
                return string.Empty;
            }

            appkey.Close();

            if (cmd.IndexOf("%1") != -1)
            {
                cmd = Regex.Replace(cmd, @"\s*""?\s*%[0-9]\s*""?\s*", string.Empty);
            }

            return cmd.Trim(" \"'".ToCharArray());
        }

        /// <summary>
        /// Creates an array of handles to large or small icons extracted from the specified executable file, DLL, or icon file.
        /// </summary>
        /// <param name="lpszFile">The name of an executable file, DLL, or icon file from which icons will be extracted.</param>
        /// <param name="nIconIndex">The zero-based index of the first icon to extract. For example, if this value is zero, the function extracts the first icon in the specified file.</param>
        /// <param name="phiconLarge">An array of icon handles that receives handles to the large icons extracted from the file. If this parameter is NULL, no large icons are extracted from the file.</param>
        /// <param name="phiconSmall">An array of icon handles that receives handles to the small icons extracted from the file. If this parameter is NULL, no small icons are extracted from the file.</param>
        /// <param name="nIcons">The number of icons to be extracted from the file.</param>
        /// <returns>If the nIconIndex parameter is -1, the phiconLarge parameter is NULL, and the phiconSmall parameter is NULL, then the return value is the number of icons contained in the specified file. Otherwise, the return value is the number of icons successfully extracted from the file.</returns>
        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        public static extern int ExtractIconExW(string lpszFile, int nIconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);

        /// <summary>
        /// Destroys an icon and frees any memory the icon occupied.
        /// </summary>
        /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll")]
        public static extern int DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Extracts the icon for a specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>WPF-compatible small icon.</returns>
        public static BitmapSource ExtractIcon(string path)
        {
            try
            {
                var largeIcon = IntPtr.Zero;
                var smallIcon = IntPtr.Zero;

                ExtractIconExW(path, 0, ref largeIcon, ref smallIcon, 1);
                DestroyIcon(largeIcon);

                return Imaging.CreateBitmapSourceFromHBitmap(Icon.FromHandle(smallIcon).ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the name and small icon of the specified executable.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Tuple containing the name and icon.</returns>
        public static Tuple<string, BitmapSource> GetExecutableInfo(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var name = FileVersionInfo.GetVersionInfo(path).ProductName;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = new FileInfo(path).Name.ToUppercaseFirst().Replace(".exe", string.Empty);
            }

            var icon = ExtractIcon(path);

            return new Tuple<string, BitmapSource>(name, icon);
        }

        /// <summary>
        /// Gets a regular expression which matches illegal characters in file names.
        /// </summary>
        /// <value>Regex for illegal characters.</value>
        public static Regex InvalidFileNameChars = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");

        /// <summary>
        /// Removes illegal characters from a file name.
        /// </summary>
        /// <param name="file">The file name.</param>
        /// <returns>Sanitized file name.</returns>
        public static string SanitizeFileName(string file)
        {
            return InvalidFileNameChars.Replace(file, string.Empty);
        }
    }
}
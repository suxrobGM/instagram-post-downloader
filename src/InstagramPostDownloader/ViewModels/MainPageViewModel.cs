using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Prism.Commands;
using Prism.Mvvm;

namespace InstagramPostDownloader.ViewModels
{
    public class MainPageViewModel : BindableBase
    {
        private string _postUrl;
        private string _status;
        private string _innerBody;
        private bool _isDownloadingFile;
        private bool _foundFileToDownload;
        private readonly HtmlParser _parser;
        private readonly WebClient _webClient;

        public string PostUrl
        {
            get => _postUrl;
            set
            {
                if (Connectivity.NetworkAccess == NetworkAccess.None)
                {
                    Status = "Status: No internet connection";
                    return;
                }

                SetProperty(ref _postUrl, value);                
                DownloadCommand.RaiseCanExecuteChanged();
            }
        }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public bool IsDownloadingFile { get => _isDownloadingFile; set => SetProperty(ref _isDownloadingFile, value); }
        public DelegateCommand DownloadCommand { get; }
        public DelegateCommand EditorOnFocusedCommand { get; }
        public DelegateCommand<WebView> WebViewOnNavigatedCommand { get; }

        public MainPageViewModel()
        {
            _parser = new HtmlParser();
            _webClient = new WebClient();
            Status = "Status: ";

            DownloadCommand = new DelegateCommand(async () =>
            {
                string extDir = App.AndroidExternalDirectory;
                string localFilesDir = Path.Combine(extDir, "InstagramPostFiles");

                if (!Directory.Exists(localFilesDir))
                    Directory.CreateDirectory(localFilesDir);

                var doc = _parser.ParseDocument(_innerBody);
                var imgNodes = doc.QuerySelectorAll("img");
                var videoNodes = doc.QuerySelectorAll("video");

                await Task.Run(() =>
                {
                    Status = "Downloading file...";
                    IsDownloadingFile = true;
                    DownloadCommand.RaiseCanExecuteChanged();

                    if (imgNodes != null && imgNodes.Count() >= 2)
                    {
                        _foundFileToDownload = true;
                        for (int i = 1; i <= imgNodes.Length - 1; i++)
                        {
                            var postImage = (imgNodes[i] as IHtmlImageElement);
                            DownloadMediaElement(postImage.Source, localFilesDir);
                        }
                    }
                    if (videoNodes != null && imgNodes.Count() > 0)
                    {
                        _foundFileToDownload = true;
                        for (int i = 0; i < videoNodes.Length; i++)
                        {
                            var postVideo = (videoNodes[i] as IHtmlVideoElement);
                            DownloadMediaElement(postVideo.Source, localFilesDir);
                        }
                    }
                    if (!_foundFileToDownload)
                    {
                        Status = "Did not found any files in this link for downloading";
                    }

                    IsDownloadingFile = false;
                    DownloadCommand.RaiseCanExecuteChanged();
                });

            }, CanExecuteDownloadButton);

            EditorOnFocusedCommand = new DelegateCommand(async () =>
            {               
                string clipText = await Clipboard.GetTextAsync();
                
                if (IsValidInstagramPostUrl(clipText))
                {
                    PostUrl = clipText;
                }
            });

            WebViewOnNavigatedCommand = new DelegateCommand<WebView>(async webView =>
            {
                _innerBody = await webView.EvaluateJavaScriptAsync("document.body.innerHTML");
                _innerBody = Regex.Unescape(_innerBody);
            });
        }

        private bool IsValidInstagramPostUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri instaPostUrl) &&
                    instaPostUrl.Host.Contains("instagram.com") &&
                    instaPostUrl.AbsolutePath.StartsWith("/p/");
        }
        private bool CanExecuteDownloadButton()
        {
            return !string.IsNullOrEmpty(PostUrl) && IsValidInstagramPostUrl(PostUrl) && !IsDownloadingFile;
        }
        private void DownloadMediaElement(string elementSource, string localFilesDir)
        {
            var postUrl = new Uri(elementSource);
            var mediaName = postUrl.Segments.Last();
            var mediaFilePath = Path.Combine(localFilesDir, mediaName);

            _webClient.DownloadFile(postUrl, mediaFilePath);
            Status = $"Downloaded {mediaName}";
        }
    }
}

using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace InstagramPostDownloader.ViewModels
{
    public class MainPageViewModel : BindableBase
    {
        private string _postUrl;
        private string _status;
        private string _innerBody;
        private bool _isDownloadingFile;
        private readonly HtmlParser _parser;
        private readonly WebClient _webClient;

        public string PostUrl
        {
            get => _postUrl;
            set
            {
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
                string postFilesDir = Path.Combine(extDir, "InstagramPostFiles");

                if (!Directory.Exists(postFilesDir))
                    Directory.CreateDirectory(postFilesDir);

                var doc = _parser.ParseDocument(_innerBody);
                var imgNodes = doc.QuerySelectorAll("img");

                if (imgNodes != null && imgNodes.Count() >= 2)
                {
                    await Task.Run(() =>
                    {
                        Status = "Downloading file...";
                        IsDownloadingFile = true;
                        DownloadCommand.RaiseCanExecuteChanged();

                        for (int i = 1; i <= imgNodes.Length - 1; i++)
                        {
                            var postImage = (imgNodes[i] as IHtmlImageElement);
                            var postUrl = new Uri(postImage.Source);
                            var mediaName = postUrl.Segments.Last();
                            var mediaFilePath = Path.Combine(postFilesDir, mediaName);
                           
                            _webClient.DownloadFile(postUrl, mediaFilePath);                           
                            Status = $"Downloaded {mediaName}";
                        }

                        IsDownloadingFile = false;
                        DownloadCommand.RaiseCanExecuteChanged();
                    });                   
                }   
                
            }, CanExecuteDownloadButton);

            EditorOnFocusedCommand = new DelegateCommand(async () =>
            {
                string clipText = await Clipboard.GetTextAsync();
                
                if (IsValidInstagramUrl(clipText))
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

        private bool IsValidInstagramUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri instaPostUrl) &&
                    instaPostUrl.Host.Contains("instagram.com") &&
                    instaPostUrl.AbsolutePath.StartsWith("/p/");
        }
        private bool CanExecuteDownloadButton()
        {
            return !string.IsNullOrEmpty(PostUrl) && IsValidInstagramUrl(PostUrl) && !IsDownloadingFile;
        }
    }
}

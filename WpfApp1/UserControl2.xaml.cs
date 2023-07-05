using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// UserControl2.xaml 的交互逻辑
    /// </summary>
    public partial class UserControl2 : UserControl
    {
        private CancellationTokenSource _cancellationTokenSource;
        private PauseTokenSource _pauseTokenSource;

        public UserControl2()
        {
            InitializeComponent();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _pauseTokenSource = new PauseTokenSource();
            try
            {
                await DownloadFileAsync(UrlTextBox.Text, SavePathTextBox.Text, _cancellationTokenSource.Token, _pauseTokenSource.PauseToken);
                StatusTextBlock.Text = "下载成功!";
            }
            catch (OperationCanceledException)
            {
                StatusTextBlock.Text = "下载暂停";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "下载失败!";
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pauseTokenSource != null)
            {
                _pauseTokenSource.IsPaused = true;
            }
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pauseTokenSource != null)
            {
                _pauseTokenSource.IsPaused = false;
            }
        }

        private async Task DownloadFileAsync(string url, string savePath, CancellationToken cancellationToken, PauseToken pauseToken)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                if (File.Exists(savePath))
                {
                    FileInfo fileInfo = new FileInfo(savePath);
                    requestMessage.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(fileInfo.Length, null);
                }

                using (HttpResponseMessage response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"下载失败，状态码为：{(int)response.StatusCode}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        long totalBytes = response.Content.Headers.ContentLength ?? 0;
                        DownloadProgressBar.Maximum = totalBytes;

                        using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                        using (FileStream fileStream = File.Open(savePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                            {
                                await pauseToken.WaitWhilePausedAsync();

                                cancellationToken.ThrowIfCancellationRequested();

                                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                                DownloadProgressBar.Value += bytesRead;
                            }
                        }
                    }
                }
            }
        }
    }
}

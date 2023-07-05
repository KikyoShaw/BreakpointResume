using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }

        private CancellationTokenSource _cancellationTokenSource;
        private PauseTokenSource _pauseTokenSource;

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

        private async Task DownloadFileAsync(string url, string savePath, CancellationToken cancellationToken, PauseToken pauseToken)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (File.Exists(savePath))
            {
                FileInfo fileInfo = new FileInfo(savePath);
                request.AddRange(fileInfo.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    long totalBytes = response.ContentLength;
                    DownloadProgressBar.Maximum = totalBytes;

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

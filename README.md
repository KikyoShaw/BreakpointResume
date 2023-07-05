# BreakpointResume

breakpoint resume 断点续传测试实例

## 说明

#### 关键步骤
  1. 创建一个 HTTP 请求来获取文件的总大小

  2. 确定需要下载的本地文件的大小

  3. 如果本地文件的大小小于总大小，则从对应位置下载剩余部分 

  4. 使用 HTTP 请求 Range 头部字段来请求剩余部分

  5. 创建FileStream并将其位置设置为本地文件的当前大小

  6. 将从网络下载的数据附加到本地文件 

  7. 监听下载进度并在需要时显示


#### 详细讲解

获取文件大小：
首先，我们需要知道文件的总大小。在这个示例中，我们使用了 HttpRequestMessage 创建一个 HTTP HEAD 请求。通过创建一个 HttpRequestMessage 实例，请求方法设置为 HttpMethod.Head，并提供文件的 URL。使用 HttpClient.SendAsync 方法发送请求并获取响应（HttpResponseMessage）。从响应的 Content.Headers.ContentRange.Length 属性中获取文件的总大小。

确定本地文件大小：
我们需要检查已经下载到本地的文件大小。如果文件存在，我们会使用 FileInfo 获取本地文件的长度；如果文件不存在，我们会将本地文件的大小设置为0。

请求剩余部分：
在这里，我们利用 HttpRequestMessage 类创建一个 HTTP GET 请求。我们设置 request.Headers.Range 的值为本地文件大小，表示请求从已下载的部分之后的数据开始。这样，服务器就会发送从这个字节位置开始的剩余文件内容。

下载并保存数据：
使用 httpClient.SendAsync 方法发送请求，并选择 HttpCompletionOption.ResponseHeadersRead 模式。这意味着当响应头读取完成时，我们就可以接收服务器返回的数据。

我们使用 response.Content.ReadAsStreamAsync 方法返回一个可用于读取文件响应内容的流。创建一个 FileStream 打开（或创建）本地文件，将写入位置设置为已接收到的数据的字节，以便在文件末尾追加新下载的数据。

使用 while 循环从服务器的响应流读取数据，然后将数据写入本地文件。在每次迭代中，使用 DateTime 类记录已下载的字节数和当前耗费的时间。

显示下载进度：
当数据附加到文件时，我们可以根据已下载数据和总文件大小计算下载进度。在这个示例中，我们每隔一秒更新一次下载进度并将其输出到控制台。可以根据需要调整更新频率。

#### 核心代码
 ```
  	long totalSize;
    using (HttpClient httpClient = new HttpClient())
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url);
        HttpResponseMessage response = await httpClient.SendAsync(request);
        totalSize = response.Content.Headers.ContentRange.Length.GetValueOrDefault();
    }

    long localFileSize = 0;
    if (File.Exists(destinationPath))
    {
        localFileSize = new FileInfo(destinationPath).Length;
    }

    using (FileStream fileStream = new FileStream(destinationPath, FileMode.Append, FileAccess.Write, FileShare.None))
    using (HttpClient httpClient = new HttpClient())
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(localFileSize, null);

        HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        using (Stream responseStream = await response.Content.ReadAsStreamAsync())
        {
            byte[] buffer = new byte[4096];
            int bytesRead;
            DateTime progressReportTime = DateTime.Now;

            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                localFileSize += bytesRead;

                if ((DateTime.Now - progressReportTime).TotalSeconds >= 1)
                {
                    Console.WriteLine($"下载进度: {(double)localFileSize / totalSize:F3}");
                    progressReportTime = DateTime.Now;
                }
            }
        }
    }
  ```


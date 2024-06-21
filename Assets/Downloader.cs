using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class  DownloadInfo
{
    /// <summary>
    /// 已经下载的文件名
    /// </summary>
    public List<string> DownloadedFileNames=new List<string>();
}
public class Downloader
{
    private string url = null; // 需要下载的文件的地址
    private string savePath = null; // 保存的路径
    private UnityWebRequest request = null; // Unity中用来与Web服务器进行通信的类
    private DownloadHandler downloadHandler = null; // 我们自己实现的下载处理类
    private ErrorEventHandler onError = null; // 出错回调
    private CompletedEventHandler onCompleted = null; // 完成回调
    private ProgressEventHandler onProgress = null; // 进度回调


    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="url">文件网络地址</param>
    /// <param name="savePath">文件本地保存地址</param>
    /// <param name="onCompleted">完成回调</param>
    /// <param name="onProgress">下载时回调</param>
    /// <param name="onError">报错回调</param>
    public Downloader(string url, string savePath, CompletedEventHandler onCompleted, ProgressEventHandler onProgress,
    ErrorEventHandler onError)
    {
        this.url = url;
        this.savePath = savePath;
        this.onCompleted = onCompleted;
        this.onProgress = onProgress;
        this.onError = onError;
    }

    /// <summary>
    /// 开始下载
    /// </summary>
    /// <param name="timeout">请求时间上限</param>
    public void Start(int timeout = 10)
    {
        request = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(savePath))
        {
            request.timeout = timeout;
            request.disposeDownloadHandlerOnDispose = true;
            downloadHandler = new DownloadHandler(savePath, onCompleted, onProgress, onError);
            // 这里是设置http的请求头
            // range表示请求资源的部分内容（不包括响应头的大小），单位是byte
            request.SetRequestHeader("range", $"bytes={downloadHandler.CurrLength}-");





            request.downloadHandler = downloadHandler;
        }
        request.SendWebRequest();
    }

    /// <summary>
    /// 下载器的释放
    /// </summary>
    public void Dispose()
    {
        onError = null;
        onCompleted = null;
        onProgress = null;
        if (request != null)
        {
            // 如果下载没有完成，就中止
            if (!request.isDone)
                request.Abort();
            request.Dispose();
            request = null;
        }
    }
}

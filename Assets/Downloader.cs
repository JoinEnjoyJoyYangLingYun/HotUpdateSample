using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class  DownloadInfo
{
    /// <summary>
    /// �Ѿ����ص��ļ���
    /// </summary>
    public List<string> DownloadedFileNames=new List<string>();
}
public class Downloader
{
    private string url = null; // ��Ҫ���ص��ļ��ĵ�ַ
    private string savePath = null; // �����·��
    private UnityWebRequest request = null; // Unity��������Web����������ͨ�ŵ���
    private DownloadHandler downloadHandler = null; // �����Լ�ʵ�ֵ����ش�����
    private ErrorEventHandler onError = null; // ����ص�
    private CompletedEventHandler onCompleted = null; // ��ɻص�
    private ProgressEventHandler onProgress = null; // ���Ȼص�


    /// <summary>
    /// ���캯��
    /// </summary>
    /// <param name="url">�ļ������ַ</param>
    /// <param name="savePath">�ļ����ر����ַ</param>
    /// <param name="onCompleted">��ɻص�</param>
    /// <param name="onProgress">����ʱ�ص�</param>
    /// <param name="onError">����ص�</param>
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
    /// ��ʼ����
    /// </summary>
    /// <param name="timeout">����ʱ������</param>
    public void Start(int timeout = 10)
    {
        request = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(savePath))
        {
            request.timeout = timeout;
            request.disposeDownloadHandlerOnDispose = true;
            downloadHandler = new DownloadHandler(savePath, onCompleted, onProgress, onError);
            // ����������http������ͷ
            // range��ʾ������Դ�Ĳ������ݣ���������Ӧͷ�Ĵ�С������λ��byte
            request.SetRequestHeader("range", $"bytes={downloadHandler.CurrLength}-");





            request.downloadHandler = downloadHandler;
        }
        request.SendWebRequest();
    }

    /// <summary>
    /// ���������ͷ�
    /// </summary>
    public void Dispose()
    {
        onError = null;
        onCompleted = null;
        onProgress = null;
        if (request != null)
        {
            // �������û����ɣ�����ֹ
            if (!request.isDone)
                request.Abort();
            request.Dispose();
            request = null;
        }
    }
}

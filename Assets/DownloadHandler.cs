using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
public enum ErrorCode
{
    DownloadContentEmpty,    // ��Ҫ���ص��ļ�����Ϊ��
    TempFileMissing,            // ��ʱ�ļ���ʧ
}

/// <summary>
/// ���ش���ص�
/// </summary>
/// <param name="errorCode">������</param>
/// <param name="message">������Ϣ</param>
public delegate void ErrorEventHandler(ErrorCode errorCode, string message);

/// <summary>
/// ������ɻص�
/// </summary>
/// <param name="message">��ɵ���Ϣ</param>
public delegate void CompletedEventHandler(string fileName,string message);

/// <summary>
/// ���ؽ��Ȼص�
/// </summary>
/// <param name="prg">��ǰ����</param>
/// <param name="currLength">��ǰ������ɵĳ���</param>
/// <param name="totalLength">�ļ��ܳ���</param>
public delegate void ProgressEventHandler(float prg, long currLength, long totalLength);
public class DownloadHandler : DownloadHandlerScript
{
    private string savePath = null; // ���浽��·��
    private string tempPath = null; // ������ʱ�ļ�·��
    private long currLength = 0; // ��ǰ�Ѿ����ص����ݳ���
    private long totalLength = 0; // �ļ������ݳ���
    private long contentLength = 0; // ������Ҫ���ص����ݳ���
    private FileStream fileStream = null; // �ļ��������������յ�������д���ļ�
    private ErrorEventHandler onError = null; // ����ص�
    private CompletedEventHandler onCompleted = null; // ��ɻص�
    private ProgressEventHandler onProgress = null; // ���Ȼص�
    public long CurrLength
    {
        get { return currLength; }
    }

    public long TotalLength
    {
        get { return totalLength; }
    }

    /// <summary>
    /// ���캯��
    /// </summary>
    /// <param name="savePath">���غ��ļ��ı����ַ</param>
    /// <param name="onCompleted">������ɵĻص�</param>
    /// <param name="onProgress">��������ʱ�Ļص�</param>
    /// <param name="onError"></param>
    public DownloadHandler(string savePath, CompletedEventHandler onCompleted, ProgressEventHandler onProgress,
        ErrorEventHandler onError) : base(new byte[1024 * 1024])
    {



        this.savePath = savePath.Replace("\\", "/");
        this.onCompleted = onCompleted;
        this.onProgress = onProgress;
        this.onError = onError;
        this.tempPath = savePath + ".temp";

        //�ҵ���Ӧ�ļ�·���µ���ʱ�ļ�,ʹ���ļ����ķ�ʽ����
        fileStream = new FileStream(tempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        //����ǰ���ȸ���Ϊ��ʱ�ļ���д����ֽڳ���
        currLength = fileStream.Length;
        fileStream.Position = currLength;
    }

    /// <summary>
    /// ���յ� Content-Length ��ͷ���õĻص���
    /// </summary>
    /// <param name="contentLength">���ļ���ĳ���ֽڿ�ʼ,���ļ����һ���ֽڵĳ���</param>
    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        this.contentLength = (long)contentLength;
        totalLength = this.contentLength + currLength;
    }
    /// <summary>
    /// ��Զ�̷������յ�����ʱ���õĻص���
    /// </summary>
    /// <param name="data"></param>
    /// <param name="dataLength"></param>
    /// <returns></returns>
    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        // ������ص����ݳ���С�ڵ���0,�ͽ�������
        if (contentLength <= 0 || data == null || data.Length <= 0)
        {
            return false;
        }

        fileStream.Write(data, 0, dataLength);
        currLength += dataLength;
        onProgress?.Invoke(currLength * 1.0f / totalLength, currLength, totalLength);
        return true;
    }

    /// <summary>
    /// �ڴ�Զ�̷����������������ݺ���õĻص�
    /// </summary>
    protected override void CompleteContent()
    {
        // ��������������ݺ����ȹر��ļ���
        FileStreamClose();
        // ����������ϲ����ڸ��ļ����������ص����ݳ��Ȼ�Ϊ0
        // ������Ҫ���⴦���������
        if (contentLength <= 0)
        {
            onError(ErrorCode.DownloadContentEmpty, "�������ݳ���Ϊ0");
            return;
        }
        // ���������ɺ���ʱ�ļ����������ɾ���ˣ�Ҳ�׳�������ʾ
        if (!File.Exists(tempPath))
        {
            onError(ErrorCode.TempFileMissing, "������ʱ�����ļ���ʧ");
            return;
        }
        // ������ص��ļ��Ѿ����ڣ���ɾ��ԭ�ļ�
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        // ͨ�������ϵ�У��󣬾ͽ���ʱ�ļ��ƶ���Ŀ��·�������سɹ�
        File.Move(tempPath, savePath);
        FileInfo fileInfo = new FileInfo(savePath);
        onCompleted(fileInfo.Name, "�����ļ����");
    }

    public override void Dispose()
    {
        base.Dispose();
        FileStreamClose();
    }
    
    /// <summary>
    /// �ر��ļ���
    /// </summary>
    public void FileStreamClose()
    {
        if (fileStream == null) return;
        fileStream.Close();
        fileStream.Dispose();
        fileStream = null;
        Debug.Log("�ļ����ر�");
    }
}

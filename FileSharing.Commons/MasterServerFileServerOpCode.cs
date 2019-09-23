namespace FileSharing.Commons
{
    public enum MasterServerFileServerOpCode : byte
    {
        Hello,
        KeepAlive,
        RequestFileList,
        ReturnFileList,        
        RequestDownloadEndPoint,
        ReturnDownloadEndPoint,        
        RequestShutDown
    }
}

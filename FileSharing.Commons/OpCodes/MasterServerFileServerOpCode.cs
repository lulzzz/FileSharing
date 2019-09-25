namespace FileSharing.Commons.OpCodes
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

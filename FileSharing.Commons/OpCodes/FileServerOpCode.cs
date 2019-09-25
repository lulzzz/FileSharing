namespace FileSharing.Commons.OpCodes
{
    public enum FileServerOpCode : byte
    {
        Ack,
        Bye,
        RequestFileInfo,
        ReturnFileInfo,
        RequestBlock,
        ReturnBlock,
    }
}

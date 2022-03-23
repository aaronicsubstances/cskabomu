namespace Kabomu.Common.Abstractions
{
    public interface IQpcSendOptions
    {
        int TimeoutMillis { get; }
        ICancellationHandle CancellationHandle { get; }
        bool IgnoreDuplicateProtection { get; }
        bool AcknowledgeReceiptBeforeRemoteProcessing { get; }
    }
}
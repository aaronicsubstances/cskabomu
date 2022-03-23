namespace Kabomu.Common.Abstractions
{
    public interface IQpcOptions
    {
        int TimeoutMillis { get; }
        ICancellationHandle CancellationHandle { get; }
        bool IgnoreDuplicateProtection { get; }
        bool AcknowledgeReceiptBeforeRemoteProcessing { get; }
        bool DuplicateProtectionIgnored { get; }
    }
}
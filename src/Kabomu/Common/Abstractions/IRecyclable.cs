namespace Kabomu.Common.Abstractions
{
    public interface IRecyclable
    {
        int RecyclingFlags { get; set; }
        IRecyclingFactory RecyclingFactory { get; }
    }
}
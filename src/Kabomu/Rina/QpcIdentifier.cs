namespace Kabomu.Rina
{
    internal class QpcIdentifier
    {
        public int RequestId { get; set; }
        public string NetworkAddressName { get; set; }
        public int NetworkAddressId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is QpcIdentifier identifier &&
                    RequestId == identifier.RequestId &&
                    NetworkAddressName == identifier.NetworkAddressName &&
                    NetworkAddressId == identifier.NetworkAddressId;
        }

        public override int GetHashCode()
        {
            // Don't include network address in hash code computation
            // in order to gain some efficiency by leveraging fact
            // that request id alone almost always uniquely identify a QPC.
            return RequestId;
        }
    }
}
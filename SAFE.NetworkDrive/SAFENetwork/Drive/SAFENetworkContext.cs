
namespace SAFE.NetworkDrive.Gateways.AsyncEvents
{
    internal class SAFENetworkContext
    {
        public EventTransactor Writer { get; }
        public DriveReader Reader { get; }
        public SequenceNr SequenceNr { get; }

        public SAFENetworkContext(EventTransactor writer, DriveReader reader, SequenceNr sequenceNr)
        {
            Writer = writer;
            Reader = reader;
            SequenceNr = sequenceNr;
        }
    }
}
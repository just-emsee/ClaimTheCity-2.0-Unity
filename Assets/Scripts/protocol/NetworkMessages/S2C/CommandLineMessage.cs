namespace shared {
    public class CommandLineMessage : ISerializable {
        public string output;
        public CommandLineMessage() { }
        public CommandLineMessage(string output) {
            this.output = output;
        }
        public void Deserialize(Packet pPacket) {
            output = pPacket.ReadString();
        }
        public void Serialize(Packet pPacket) {
            pPacket.Write(output);
        }
    }
}
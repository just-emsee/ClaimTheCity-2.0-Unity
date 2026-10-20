namespace shared{
    public class ConsoleCommandMessage : ISerializable {
        public string command;

        public ConsoleCommandMessage() {}

        public ConsoleCommandMessage(string command) {
            this.command = command;
        }

        public void Deserialize(Packet pPacket) {
            command = pPacket.ReadString();
        }
        public void Serialize(Packet pPacket) {
            pPacket.Write(command);
        }
    }

}

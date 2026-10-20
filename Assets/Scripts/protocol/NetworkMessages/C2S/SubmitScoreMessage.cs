namespace shared {
    public class SubmitScoreMessage : ISerializable {
        public string teamPass;
        public string taskName;
        public double score;

        public SubmitScoreMessage() { }

        public SubmitScoreMessage(string teamPass, string taskName, double score) {
            this.teamPass = teamPass;
            this.taskName = taskName;
            this.score = score;
        }


        public void Serialize(Packet pPacket) {
            pPacket.Write(teamPass);
            pPacket.Write(taskName);
            pPacket.Write(score);
        }

        public void Deserialize(Packet pPacket) {
            teamPass = pPacket.ReadString();
            taskName = pPacket.ReadString();
            score = pPacket.ReadDouble();
        }
    }
}
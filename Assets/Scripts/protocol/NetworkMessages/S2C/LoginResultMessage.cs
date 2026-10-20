using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace shared {
    public class LoginResultMessage : ISerializable{
        public bool success { get; private set; }

        public bool asAdmin { get; private set; }
        public GameTeam team {  get; private set; }
        public string password { get; private set; }

        public LoginResultMessage() {
            success = false;
            asAdmin = false;
            team = new GameTeam("invalid");
            password = "invalid";
        }
        public LoginResultMessage(GameTeam team, string password, bool asAdmin = false) {
            success = true;
            this.team = team;
            this.password = password;
        }

        public LoginResultMessage(bool asAdmin, string password) {
            success = true;
            this.asAdmin = asAdmin;
            team = new("Admin");
            this.password = password;
        }


        public void Serialize(Packet pPacket) {
            pPacket.Write(success);
            pPacket.Write(asAdmin);
            pPacket.Write(team);
            pPacket.Write(password);
        }

        public void Deserialize(Packet pPacket) {
            success=pPacket.ReadBool();
            asAdmin = pPacket.ReadBool();
            team = (GameTeam)pPacket.ReadObject();
            password = pPacket.ReadString();
        }
    }
}

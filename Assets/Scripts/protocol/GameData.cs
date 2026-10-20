using shared;
using System;
using System.Collections.Generic;

public class GameData : ISerializable {
    public bool updated;

    // <passWord, Team>
    readonly Dictionary<string, GameTeam> teams = new Dictionary<string, GameTeam>();
    readonly Dictionary<string, GameTask> tasks = new Dictionary<string, GameTask>();

    public void addScore(string taskName, string teamPassWord, double score) {
        if (taskExists(taskName) && teamExists(teamPassWord))
            tasks[taskName].setScore(score, teams[teamPassWord], teamPassWord);
        updated = true;
    }

    public bool taskExists(string name) {
        return tasks.ContainsKey(name);
    }

    public bool teamExists(string passWord) {
        return teams.ContainsKey(passWord);
    }

    public void CreateTask(string name) {
        if (tasks.ContainsKey(name)) throw new Exception("Name already in use");
        tasks.Add(name, new GameTask());
        updated = true;
    }

    public void RemoveTask(string name) {
        if (!tasks.ContainsKey(name)) throw new Exception("Task does not exist");
        tasks.Remove(name);
        updated = true;
    }

    public void CreateTeam(string name, string pass) {
        if (teams.ContainsKey(pass)) throw new Exception("Password already in use");
        teams.Add(pass, new GameTeam(name));
        updated = true;
    }

    public void RemoveTeam(string pass) {
        if (!teams.ContainsKey(pass)) throw new Exception("Team with this password does not exist");
        foreach (GameTask task in tasks.Values) {
            task.TeamRemoved(teams[pass]);
        }
        teams.Remove(pass);
        updated = true;
    }

    public void SetTeamColor(string pass, string r, string g, string b) {
        if (!teams.ContainsKey(pass)) throw new Exception("Team with this password does not exist");
        try {
            teams[pass].setColor(int.Parse(r), int.Parse(g), int.Parse(b));
            updated = true;
        } catch {
            throw new Exception("color values are non numbers");
        }
    }

    public void Serialize(Packet pPacket) {
        pPacket.Write(teams.Count);
        pPacket.Write(tasks.Count);
        foreach (KeyValuePair<string, GameTeam> pair in teams) {
            pPacket.Write(pair.Key);
            pPacket.Write(pair.Value);
        }
        foreach (KeyValuePair<string, GameTask> pair in tasks) {
            pPacket.Write(pair.Key);
            pPacket.Write(pair.Value);
        }
    }

    public void Deserialize(Packet pPacket) {
        int teamCount = pPacket.ReadInt();
        int taskCount = pPacket.ReadInt();
        teams.Clear();
        tasks.Clear();
        while (teamCount > 0) {
            teams.Add(pPacket.ReadString(), (GameTeam)pPacket.ReadObject());
            teamCount--;
        }
        while (taskCount > 0) {
            tasks.Add(pPacket.ReadString(), (GameTask)pPacket.ReadObject());
            taskCount--;
        }
    }

    public string GetTeamLog() {
        string toReturn = "";
        foreach (KeyValuePair<string, GameTeam> pair in teams) {
            toReturn += $" - {pair.Value.name}, [{pair.Key}]\n";
        }
        toReturn = toReturn.TrimEnd('\n');
        return toReturn;
    }

    public string GetTaskLog() {
        string toReturn = "";
        foreach (KeyValuePair<string, GameTask> pair in tasks) {
            toReturn += $" - {pair.Key}\n";
        }
        toReturn = toReturn.TrimEnd('\n');
        return toReturn;
    }

    public GameTeam GetTeamData(string pass) {
        return teams[pass];
    }

    public int TeamAmount() {
        return teams.Count;
    }

    public int TaskAmount() {
        return tasks.Count;
    }

    public Dictionary<string, GameTask> GetTasks() {
        return tasks;
    }
}

public class GameTeam : ISerializable {

    public string name { get; private set; }
    public int R { get; private set; }
    public int G { get; private set; }
    public int B { get; private set; }
    public GameTeam() { }
    public GameTeam(string name) { this.name = name; }

    public void setColor(int r, int g, int b) {
        R = r;
        G = g;
        B = b;
    }

    public void Serialize(Packet pPacket) {
        pPacket.Write(name);
        pPacket.Write(R);
        pPacket.Write(G);
        pPacket.Write(B);
    }

    public void Deserialize(Packet pPacket) {
        name = pPacket.ReadString();
        R = pPacket.ReadInt();
        G = pPacket.ReadInt();
        B = pPacket.ReadInt();
    }


}

public class GameTask : ISerializable {
    private readonly List<Scoring> scores = new();
    private Scoring bestScore;
    public string description = "";
    public bool HasTeamScore(GameTeam team) {
        return scores.Exists(s => s.team == team);
    }

    public bool HasTeamScore(string pass) {
        return scores.Exists(s => s.teamPass == pass);
    }

    public double getScore(GameTeam team) {
        if (!HasTeamScore(team)) return 0;
        double score = 0;
        foreach (Scoring scoring in scores) {
            if (scoring.team == team && scoring.score > score) {
                score = scoring.score;
            }
        }
        return score;
    }

    public void setScore(double score, GameTeam team, string pass) {
        scores.Add(new Scoring(score, team, pass));
    }

    public void Serialize(Packet pPacket) {
        pPacket.Write(scores.Count);
        foreach (Scoring scoring in scores) {
            pPacket.Write(scoring);
        }
        pPacket.Write(description);
    }

    public void Deserialize(Packet pPacket) {
        int scoreCount = pPacket.ReadInt();
        scores.Clear();
        while (scoreCount > 0) {
            scores.Add((Scoring)pPacket.ReadObject());
            scoreCount--;
        }
        description = pPacket.ReadString();
    }

    public int R() {
        if (bestScore == null) return 0;
        return bestScore.team.R;
    }
    public int G() {
        if (bestScore == null) return 0;
        return bestScore.team.G;
    }
    public int B() {
        if (bestScore == null) return 0;
        return bestScore.team.B;
    }

    public void RecalculateBestScore() {
        bestScore = null;
        foreach (Scoring score in scores) {
            if (bestScore == null) {
                bestScore = score;
                continue;
            }
            if (score.score > bestScore.score) {
                bestScore = score;
            }
        }
    }

    public Scoring GetBestScore() {
        return bestScore;
    }

    public void TeamRemoved(GameTeam team) {
        for (int i = scores.Count - 1; i >= 0; i--) {
            Scoring score = scores[i];
            if (score.team == team) {
                scores.Remove(score);
            }
        }

        if (bestScore != null && bestScore.team == team) {
            RecalculateBestScore();
        }
    }

    public class Scoring : ISerializable {
        public double score;
        public GameTeam team;
        public string teamPass;
        public string timestamp;

        public Scoring() { }

        public Scoring(double score, GameTeam team, string pass) {
            this.score = score;
            this.team = team;
            this.teamPass = pass;
            this.timestamp = DateTime.UtcNow.AddHours(2).ToString("o");
        }


        public void Serialize(Packet pPacket) {
            pPacket.Write(score);
            pPacket.Write(team);
            pPacket.Write(teamPass);
            pPacket.Write(timestamp);
        }
        public void Deserialize(Packet pPacket) {
            score = pPacket.ReadDouble();
            team = (GameTeam)pPacket.ReadObject();
            teamPass = pPacket.ReadString();
            timestamp = pPacket.ReadString();
        }
    }

}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BattleAnalyzer
{
    public class EventData // This class contains data about an event such an attack or so, they can result in death which is important to track who caused the kill
    {
        public void clear()
        {
            event_name = "";
            user = "";
            target = "";
            player_user = 0;
            player_target = 0;
    }

        public string event_name = "";
        public string user = "";
        public int player_user = 0;
        public string target = "";
        public int player_target = 0;

        public override string ToString()
        {
            return $"P{player_user}'s {user} used {event_name} on P{player_target}'s {target}";
        }
    }
    public class BattleData
    {
        TeamData[] player_data = new TeamData[2]; // Both players
        public BattleData()
        {
            for (int i = 0; i < player_data.Length; i++)
            {
                player_data[i] = new TeamData();
                player_data[i].TeamNumber = i+1;
            }
        }
        public TeamData getTeam(string identifier) // Returns a team given team string (p1, p2, p1a, p2a, etc)
        {
            if (identifier[0] != 'p' ||  (identifier[1] != '1' &&  identifier[1] != '2'))
            {
                throw new Exception($"INVALID PLAYER STRING FOUND IN PARSING: {identifier}!");
            }
            return getTeam(identifier[1] - '0');
        }
        public TeamData getTeam(int team_number)
        {
            return player_data[team_number-1];
        }
        public TeamData getTeamFromName(string name)
        {
            foreach (TeamData team in player_data)
            {
                if(team.Name == name)
                {
                    return team;
                }
            }
            throw new Exception("Requested nonexistant team name!");
        }
    }
    public class TeamData
    {
        public string Name = "";
        public bool Winner = false;
        public int TeamNumber = 0; // Team 1 or 2
        public Dictionary<string, PokemonData> PokemonInTeam = new Dictionary<string, PokemonData>();
        public Dictionary<string, string> DamagingFieldEffectAndLastUser = new Dictionary<string, string>();
        public override string ToString()
        {
            return $"{Name}";
        }
    }

    public class PokemonData
    {
        public PokemonData(string name)
        {
            Name = name;
        }

        public string Name = "";
        public float NumberOfTurns = 0.0f;
        public int NumberOfKills = 0;
        public int NumberOfDeaths = 0;
        public Dictionary<string, string> DamagingEventsAndUser = new Dictionary<string, string>();

        public override string ToString()
        {
            return $"{Name} {NumberOfTurns:0.0}T {NumberOfKills}K {NumberOfDeaths}D";
        }
    }
}

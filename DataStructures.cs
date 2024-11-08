using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BattleAnalyzer
{
    public class AttackData // This class contains data about an event such an attack or so, they can result in death which is important to track who caused the kill
    {
        public void clear()
        {
            attack_name = "";
            user = "";
            player_user = 0;
    }

        public string attack_name = "";
        public string user = "";
        public int player_user = 0;

        public override string ToString()
        {
            return $"P{player_user}'s {user} used {attack_name}";
        }
    }

    public class HazardSetData
    {
        public void clear()
        {
            hazard_name = "";
            user = "";
            cause = "";
            player_user = 0;
        }
        
        public string hazard_name = "";
        public string user = "";
        public string cause = "";
        public int player_user = 0;

        public override string ToString()
        {
            return $"P{player_user}'s {user}'s {cause} caused {hazard_name}";
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
        static bool first_urshifu = true; // Horrible patch because urshifu is an asshole
        public void SetNickname(string mon, string nickname)
        {
            if(!PokemonInTeam.ContainsKey(mon)) // Not found?! then it means that it's a mon with -* (unknown form??)
            {
                bool found_unknown = false;
                foreach(string pokemon_prev_name in PokemonInTeam.Keys)
                {
                    if (!PokemonInTeam[pokemon_prev_name].DiscoveredName) // Found a mon with an undiscovered nickname
                    {
                        if (mon.Split('-')[0] == pokemon_prev_name.Split('-')[0]) // If same species, got the mon
                        {
                            found_unknown = true;
                            PokemonInTeam[pokemon_prev_name].DiscoveredName = true;
                            ChangeMonForm(pokemon_prev_name, mon); // Change to correct species
                            break;
                        }
                    }
                }
                if (!found_unknown) throw new Exception("Mon not in team!");
            }
            PokemonInTeam[mon].Nickname = nickname;
        }
        public string GetMonByNickname(string nickname)
        {
            foreach(PokemonData mon in PokemonInTeam.Values)
            {
                if(mon.Nickname == nickname)
                {
                    return mon.Name;
                }
            }
            throw new Exception("Fetching a nickname that doesn't exist!");
        }
        public string ChangeMonForm(string nickname, string form) // Changes the form name (e.g. mega). Returns old name of what changed
        {
            PokemonData new_mon = new PokemonData(form);
            new_mon.Nickname = nickname;
            string old_mon = GetMonByNickname(nickname); // Get the old entry
            PokemonData old_mon_data = PokemonInTeam[old_mon];
            new_mon.NumberOfTurns = old_mon_data.NumberOfTurns; // Copy values
            new_mon.NumberOfDeaths = old_mon_data.NumberOfDeaths;
            new_mon.NumberOfKills = old_mon_data.NumberOfKills;
            new_mon.DiscoveredName = old_mon_data.DiscoveredName;
            // Replace
            PokemonInTeam.Remove(old_mon);
            PokemonInTeam.Add(form, new_mon);
            return old_mon;
        }
        public bool HasMon(string mon)
        {
            return PokemonInTeam.ContainsKey(mon);
        }
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
            Nickname = name; // By default, the same, if a nickname appears I override
        }

        public string Name = "";
        public string Nickname = "";
        public float NumberOfTurns = 0.0f;
        public int NumberOfKills = 0;
        public int NumberOfDeaths = 0;
        public Dictionary<string, string> DamagingEventsAndUser = new Dictionary<string, string>();
        public bool DiscoveredName = true;

        public override string ToString()
        {
            return $"{Name} {NumberOfTurns:0.0}T {NumberOfKills}K {NumberOfDeaths}D ({Nickname})";
        }
    }
}

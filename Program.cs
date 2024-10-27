using System.Threading.Tasks;

namespace BattleAnalyzer
{
    internal class Program
    {
        const bool DEBUG = true;
        static void Main(string[] args)
        {
            BattleData battle_data = new BattleData(); // info about current battle
            EventData current_event = new EventData(); // Contains info about current event like an attack

            // Start Battle, open file
            string file_name;
            if (args.Length == 0) // File not included, need to ask for it
            {
                PrintUtilities.printString("Please input file name of battle to analyze\n", ConsoleColor.White, ConsoleColor.Black);
                file_name = "test.html"; // Console.ReadLine()!;
            }
            else
            {
                file_name = args[0];
            }
            string[] all_lines = File.ReadAllLines(file_name); // open battle file

            TeamData current_team; // Which team i'm parsing in current line (AUX var)
            string current_poke; // Which poke i'm parsing (AUX var)

            foreach (string line in all_lines)
            {
                if (line == "" || line[0] != '|') // Battle info begins with | and no spaces, only care about them
                {
                    continue;
                }
                if(DEBUG)
                {
                    PrintUtilities.printString(line+"\n", ConsoleColor.Magenta, ConsoleColor.Black);
                }
                string[] battle_data_lines = line.Split('|'); // Get each element
                if (battle_data_lines[1].Length == 0 || battle_data_lines[1][0] != '-') // Defines new event, lines with - are part of previous event
                {
                    current_event.clear();
                }
                switch (battle_data_lines[1]) // Check the second value (command, always?) and do different stuff for each
                {
                    case "player": // Gives names of player, and which player is it (p1 or p2)
                        current_team = battle_data.getTeam(battle_data_lines[2]);
                        current_team.Name = battle_data_lines[3]; // Add name to player
                        PrintUtilities.printString($"TEAM {battle_data_lines[2]}: {current_team.Name}\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "poke": // Defines which poke in which team
                        current_team = battle_data.getTeam(battle_data_lines[2]);
                        string[] pokemon_to_add = battle_data_lines[3].Split(','); // Separate mon from gender
                        current_team.PokemonInTeam.Add(pokemon_to_add[0], new PokemonData(pokemon_to_add[0]));
                        break;
                    case "switch": // Pokemon has been switched in, the older one in the turn is marked as old and new one is the main
                        string[] switch_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(switch_data[0]);
                        current_poke = switch_data[1].Trim(' '); // Remove spaces, now i got the mon
                        PrintUtilities.printString($"P{current_team}'s {current_poke} switch in\n", ConsoleColor.White, ConsoleColor.Black);
                        // TODO SWITCH LOGIC!
                        break;
                    case "turn": // VERY IMPORTANT, turn begins, mons start in turn full and (except first turn) the previous turn is loaded
                        // TODO: SWITCH AND TURN LOGIC
                        PrintUtilities.printString($"Turn {int.Parse(battle_data_lines[2])}\n", ConsoleColor.Yellow, ConsoleColor.Black);
                        break;
                    case "move": // A mon used a move, maaaybe damaging, we'll see
                        // Move is an event
                        current_event.event_name = battle_data_lines[3]; // Name of move
                        string[] move_data = battle_data_lines[2].Split(':');
                        current_event.player_user = move_data[0][1] - '0'; // Number of player obtained in dirty way
                        current_event.user = move_data[1].Trim(' '); // Remove spaces, now i got the mon
                        // Repeat for target
                        move_data = battle_data_lines[4].Split(':');
                        current_event.player_target = move_data[0][1] - '0';
                        current_event.target = move_data[1].Trim(' ');
                        PrintUtilities.printString("\t-"+current_event+"\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "-damage": // Part of the event, mon received damage and possibly fainted
                        // TODO: What happens with future sight/doom desire?
                        if (battle_data_lines[3].Contains("fnt")) // Target of attack received lethal damage!
                        {
                            string[] fainted_mon_data = battle_data_lines[2].Split(':');
                            int dead_owner = fainted_mon_data[0][1] - '0';
                            current_poke = fainted_mon_data[1].Trim(' '); // Remove spaces, now i got the mon
                            if(current_poke == current_event.target && dead_owner == current_event.player_target) // The dead mon was the target of the attack, so it was killed!
                            {
                                current_team = battle_data.getTeam(current_event.player_user); // Find player and mon that killed
                                current_poke = current_event.user;
                                current_team.PokemonInTeam[current_poke].NumberOfKills++; // Assign kill
                                PrintUtilities.printString($"\t\t-{current_event.user} killed {current_event.target} with {current_event.event_name} ({current_team.PokemonInTeam[current_poke].NumberOfKills} TOTAL KILLS)\n", ConsoleColor.Red, ConsoleColor.Black);
                            }
                            else
                            {//Wtf
                                throw new Exception("Not sure what happened here, a mon died from attack without being targeted");
                            }
                        }
                        break;
                    case "-ability": // A mon activated an ability and so it just did something (e.g. terrain on switch in)
                        string[] ability_data = battle_data_lines[2].Split(':');
                        int ability_owner = ability_data[0][1] - '0';
                        current_poke = ability_data[1].Trim(' ');
                        PrintUtilities.printString($"\t\t-{current_poke} activated ability {battle_data_lines[3]}\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "-heal": // A mon activated an ability and so it just did something (e.g. terrain on switch in)
                        string[] heal_data = battle_data_lines[2].Split(':');
                        int heal_owner = heal_data[0][1] - '0';
                        current_poke = heal_data[1].Trim(' ');
                        PrintUtilities.printString($"\t\t-{current_poke} healed {battle_data_lines[4]}\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "faint": // mon fainted either from event (attack) or recoil (or status?)
                        string[] faint_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(faint_data[0]); // Fainted mon and its owner
                        current_poke = faint_data[1].Trim(' ');
                        current_team.PokemonInTeam[current_poke].NumberOfDeaths++;
                        PrintUtilities.printString($"\t-{current_poke} died ({current_team.PokemonInTeam[current_poke].NumberOfDeaths} TOTAL DEATHS)\n\n", ConsoleColor.Red, ConsoleColor.Black);
                        break;
                    case "win": // A player won, let's set that flag up
                        string winner = battle_data_lines[2];
                        current_team = battle_data.getTeamFromName(winner);
                        current_team.Winner = true;
                        break;
                    default: // Many commands (like chat or join) are not analised
                        break;
                }
            }
            // At this point, file has been parsed and winner has been decided. Now I need to populate the data.
            /*
            Data has the following format (CSV)
            NAME,W/L,+/-,TT
            RESULT,<WIN_OR_LOSE>,<DIFF>,<TURN#>
            POKEMON,K,D,Turns
            <mon>,<mon_kills>,<mon_deaths>,<mon_turns>
             */
            using (StreamWriter outputtext = new StreamWriter("./output.txt"))
            {
                int diff;
                TeamData winner_team;
                // Check winner and calculate diff
                current_team = battle_data.getTeam(1);
                winner_team = (current_team.Winner) ? current_team : battle_data.getTeam(2);
                diff = winner_team.PokemonInTeam.Count;
                foreach(PokemonData pokemon in winner_team.PokemonInTeam.Values)
                {
                    if(pokemon.NumberOfDeaths >0)
                    {
                        diff--;
                    }
                }
                // Save for player 1
                PrintUtilities.ExportMonData(outputtext, current_team, diff);
                // Empty line
                outputtext.WriteLine();
                // Save for player 2
                current_team = battle_data.getTeam(2);
                PrintUtilities.ExportMonData(outputtext, current_team, diff);
            }
        }
    }
}

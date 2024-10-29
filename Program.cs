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
            TurnCounter turn_state_machine = new TurnCounter(battle_data); // Holds info about ongoing turn and keeps the pace (and logs)

            // Start Battle, open file
            string file_name;
            string file_path;
            if (args.Length == 0) // File not included, need to ask for it
            {
                PrintUtilities.printString("Please input file name of battle to analyze\n", ConsoleColor.White, ConsoleColor.Black);
                file_name = "test.html"; // Console.ReadLine()!;
            }
            else
            {
                file_name = args[0];
            }
            PrintUtilities.printString(Path.GetFullPath(file_name), ConsoleColor.Cyan, ConsoleColor.Black);
            file_path = Path.GetDirectoryName(Path.GetFullPath(file_name));
            string[] all_lines = File.ReadAllLines(file_name); // open battle file

            TeamData current_team; // Which team i'm parsing in current line (AUX var)
            string current_poke; // Which poke i'm parsing (AUX var)
            string current_poke_nickname; // The nickname of the switched-in mon

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
                        current_poke_nickname = switch_data[1].Trim(' '); // Remove spaces, now i got the mon
                        current_poke = battle_data_lines[3].Split(',')[0];
                        current_team.SetNickname(current_poke, current_poke_nickname);
                        turn_state_machine.SwitchIn(current_team.TeamNumber, current_poke);
                        PrintUtilities.printString($"{current_team}'s {current_poke} switch in\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "turn": // VERY IMPORTANT, turn begins, mons start in turn full and (except first turn) the previous turn is loaded
                        turn_state_machine.StartTurn(int.Parse(battle_data_lines[2]));
                        break;
                    case "move": // A mon used a move, maaaybe damaging, we'll see
                        // Move is an event
                        current_event.event_name = battle_data_lines[3]; // Name of move
                        string[] move_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(move_data[0]);
                        current_event.player_user = current_team.TeamNumber;
                        current_poke_nickname = move_data[1].Trim(' '); // Remove spaces, now i got the mon
                        current_event.user = current_team.GetMonByNickname(current_poke_nickname);
                        // Repeat for target
                        move_data = battle_data_lines[4].Split(':');
                        current_team = battle_data.getTeam(move_data[0]);
                        current_event.player_target = current_team.TeamNumber;
                        current_poke_nickname = move_data[1].Trim(' ');
                        current_event.target = current_team.GetMonByNickname(current_poke_nickname);
                        PrintUtilities.printString("\t-"+current_event+"\n", ConsoleColor.White, ConsoleColor.Black);
                        turn_state_machine.Move(); // Notify of move
                        break;
                    case "-damage": // Part of the event, mon received damage and possibly fainted
                        // TODO: What happens with future sight/doom desire?
                        if (battle_data_lines[3].Contains("fnt")) // Target of attack received lethal damage!
                        {
                            string[] fainted_mon_data = battle_data_lines[2].Split(':');
                            current_team = battle_data.getTeam(fainted_mon_data[0]);
                            int dead_owner = current_team.TeamNumber;
                            current_poke_nickname = fainted_mon_data[1].Trim(' '); // Remove spaces, now i got the mon
                            current_poke = current_team.GetMonByNickname(current_poke_nickname);
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
                        current_team = battle_data.getTeam(ability_data[0]);
                        int ability_owner = current_team.TeamNumber;
                        current_poke_nickname = ability_data[1].Trim(' ');
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        PrintUtilities.printString($"\t\t-{current_poke} activated ability {battle_data_lines[3]}\n", ConsoleColor.White, ConsoleColor.Black);
                        turn_state_machine.Ability();
                        break;
                    case "-heal": // A mon activated an ability and so it just did something (e.g. terrain on switch in)
                        string[] heal_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(heal_data[0]);
                        int heal_owner = current_team.TeamNumber;
                        current_poke_nickname = heal_data[1].Trim(' ');
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        PrintUtilities.printString($"\t\t-{current_poke} healed {battle_data_lines[4]}\n", ConsoleColor.White, ConsoleColor.Black);
                        turn_state_machine.Heal();
                        break;
                    case "faint": // mon fainted either from event (attack) or recoil (or status?)
                        string[] faint_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(faint_data[0]); // Fainted mon and its owner
                        current_poke_nickname = faint_data[1].Trim(' ');
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        current_team.PokemonInTeam[current_poke].NumberOfDeaths++;
                        PrintUtilities.printString($"\t-{current_poke} died ({current_team.PokemonInTeam[current_poke].NumberOfDeaths} TOTAL DEATHS)\n\n", ConsoleColor.Red, ConsoleColor.Black);
                        turn_state_machine.Faint(current_team.TeamNumber, current_poke);
                        break;
                    case "win": // A player won, let's set that flag up
                        string winner = battle_data_lines[2];
                        current_team = battle_data.getTeamFromName(winner);
                        current_team.Winner = true;
                        turn_state_machine.Win();
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
            using (StreamWriter outputtext = new StreamWriter(file_path+"\\output.txt"))
            {
                int diff;
                TeamData winner_team;
                // Check winner and calculate diff
                current_team = battle_data.getTeam(1);
                winner_team = (current_team.Winner) ? current_team : battle_data.getTeam(2);
                diff = winner_team.PokemonInTeam.Count;
                foreach(PokemonData pokemon in winner_team.PokemonInTeam.Values)
                {
                    if(pokemon.NumberOfDeaths > 0)
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

            Console.WriteLine("\n----- PRESS ENTER TO FINISH -----");
            Console.ReadLine();
        }
    }
}

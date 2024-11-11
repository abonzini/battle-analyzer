using System.Threading.Tasks;

namespace BattleAnalyzer
{
    internal class Program
    {
        const bool DEBUG = false;
        static void Main(string[] args)
        {
            BattleData battle_data = new BattleData(); // info about current battle
            AttackData current_attack = new AttackData(); // Contains info about current event like an attack
            HazardSetData current_hazard_setting = new HazardSetData(); // Contains info about potential hazard setting
            TurnCounter turn_state_machine = new TurnCounter(battle_data); // Holds info about ongoing turn and keeps the pace (and logs)

            // Start Battle, open file
            string file_name;
            string file_path;
            if (args.Length == 0) // File not included, need to ask for it
            {
                PrintUtilities.printString("Please input file name of battle to analyze\n", ConsoleColor.White, ConsoleColor.Black);
                file_name = Console.ReadLine()!;
            }
            else
            {
                file_name = args[0];
            }
            PrintUtilities.printString(Path.GetFullPath(file_name)+'\n', ConsoleColor.Cyan, ConsoleColor.Black);
            file_path = Path.GetDirectoryName(Path.GetFullPath(file_name));
            string[] all_lines = File.ReadAllLines(file_name); // open battle file

            TeamData current_team; // Which team i'm parsing in current line (AUX var)
            string current_poke; // Which poke i'm parsing (AUX var)
            string current_poke_nickname; // The nickname of the switched-in mon

            string last_line = "";
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
                    current_attack.clear();
                    current_hazard_setting.clear();
                    last_line = battle_data_lines[1]; // Registers what was the last command happened (attack, switch?)
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
                        PokemonData mon_to_add = new PokemonData(pokemon_to_add[0]);
                        if (pokemon_to_add[0].Contains("-*")) // Unknown forms pre-battle
                        {
                            mon_to_add.DiscoveredName = false;
                        }
                        current_team.PokemonInTeam.Add(mon_to_add.Name, mon_to_add);
                        break;
                    case "drag":
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
                        // Move is an event, I record who used it
                        current_attack.attack_name = battle_data_lines[3]; // Name of move
                        string[] move_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(move_data[0]);
                        current_attack.player_user = current_team.TeamNumber;
                        current_poke_nickname = move_data[1].Trim(' '); // Remove spaces, now i got the mon
                        current_attack.user = current_team.GetMonByNickname(current_poke_nickname);
                        // Notify all parts
                        PrintUtilities.printString($"\t-{battle_data.getTeam(current_attack.player_user)}'s {current_attack.user} used {current_attack.attack_name}\n", ConsoleColor.White, ConsoleColor.Black);
                        turn_state_machine.Move(); // Notify of move
                        // If move causd hazard...
                        switch(current_attack.attack_name)
                        {
                            case "Toxic Spikes":
                                current_hazard_setting.hazard_name = current_attack.attack_name;
                                current_hazard_setting.user = current_attack.user;
                                current_hazard_setting.cause = current_attack.attack_name;
                                current_hazard_setting.player_user = current_attack.player_user;
                                break;
                            case "Stealth Rock":
                            case "Stone Axe":
                                current_hazard_setting.hazard_name = "Stealth Rock";
                                current_hazard_setting.user = current_attack.user;
                                current_hazard_setting.cause = current_attack.attack_name;
                                current_hazard_setting.player_user = current_attack.player_user;
                                break;
                            case "Spikes":
                            case "Ceaseless Edge":
                                current_hazard_setting.hazard_name = "Spikes";
                                current_hazard_setting.user = current_attack.user;
                                current_hazard_setting.cause = current_attack.attack_name;
                                current_hazard_setting.player_user = current_attack.player_user;
                                break;
                            default:
                                break;
                        }
                        break;
                    case "replace": // For zoroark things
                        // Very similar to change form but only current in-turn record is changed (all we can do)
                        string[] replace_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(replace_data[0]);
                        current_poke_nickname = replace_data[1].Trim(' '); // Remove spaces, now i got the mon
                        current_poke = battle_data_lines[3].Split(',')[0];
                        string prev_mon_illusion = turn_state_machine.GetCurrentMon(current_team.TeamNumber); // This mon needs to be replaced in turn counter
                        turn_state_machine.ChangeMonName(current_team.TeamNumber, prev_mon_illusion, current_poke);
                        PrintUtilities.printString($"{prev_mon_illusion} was {current_poke}!\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "detailschange":
                        //|detailschange|p2a: Mega-Swampass|Swampert-Mega
                        string[] form_change_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(form_change_data[0]);
                        current_poke_nickname = form_change_data[1].Trim(' '); // Remove spaces, now i got the mon
                        current_poke = battle_data_lines[3].Split(',')[0];
                        // Got all info Í need, update
                        string old_form_name = current_team.ChangeMonForm(current_poke_nickname, current_poke); // Notify all elements of change!
                        turn_state_machine.ChangeMonName(current_team.TeamNumber, old_form_name, current_poke);
                        PrintUtilities.printString($"{old_form_name} became {current_poke}\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "-activate":
                        // Activation can cause hazard (toxic debris???)
                        string[] activate_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(activate_data[0]);
                        current_poke_nickname = activate_data[1].Trim(' '); // Remove spaces, now i got the mon
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        string activate_effect = battle_data_lines[3].Split(':').Last().Trim(' '); // Get name of effect

                        switch (activate_effect)
                        {
                            case "Toxic Debris": // May create a hazard, causing tspike on opp field
                                current_hazard_setting.user = current_poke;
                                current_hazard_setting.cause = "Toxic Debris";
                                current_hazard_setting.hazard_name = "Toxic Spikes";
                                current_hazard_setting.player_user = current_team.TeamNumber;
                                break;
                            case "confusion": // Oh crap this may've been caused by an attack?! in that case current poke is the victim and depends on attack
                                switch (current_attack.attack_name) // Todo shadow puppeteer
                                {
                                    case "Hurricane": // Attacks that cause cnf
                                    case "Swagger":
                                    case "Confuse Ray":
                                        if(current_attack.player_user !=  current_team.TeamNumber) // Can only caused by opp, not myself
                                        {
                                            current_team.PokemonInTeam[current_poke].DamagingEventsAndUser["confusion"] = current_attack.user; // Someone used attack
                                        }
                                        break;
                                    default: // These won't be considered then 
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case "-sidestart":
                        // Starting of entry hazard mostly from a move
                        string[] side_start_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(side_start_data[0]);
                        // Team can't self inflict hazard, check if hazard available then!
                        if(current_team.TeamNumber != current_hazard_setting.player_user && current_hazard_setting.hazard_name != "")
                        {
                            current_team.DamagingFieldEffectAndLastUser[current_hazard_setting.hazard_name] = current_hazard_setting.user; // Now there's a field effect that may grant kills
                            PrintUtilities.printString($"\t\t-{current_hazard_setting} on {current_team.Name}'s side\n", ConsoleColor.White, ConsoleColor.Black);
                        }
                        break;
                    case "-weather":
                        string weather = battle_data_lines[2];
                        if(weather != "Hail" && weather != "Sandstorm") // No need to register non-damaging weathers
                        {
                            break;
                        }
                        if(battle_data_lines.Length > 3 && battle_data_lines[3].Contains("upkeep")) // Upkeep means no new weather
                        {
                            break;
                        }
                        string weather_setter = "";
                        if (battle_data_lines.Length > 4 && battle_data_lines[4].Contains("[of]")) // Means a mon set it with ability
                        {
                            string[] weather_start_data;
                            weather_setter = battle_data_lines[4].Replace("[of] ", ""); // Remove this
                            weather_start_data = weather_setter.Split(':');
                            current_team = battle_data.getTeam(weather_start_data[0]);
                            weather_setter = weather_start_data[1].Trim(' ');
                            weather_setter = current_team.GetMonByNickname(weather_setter);
                        }
                        else // Weather set manually
                        {
                            weather_setter = current_attack.user; // Set by corresponding mon then
                        }
                        for (int i = 1; i <= 2; i++) // Add only on the side that can actually get killed by weather (can't self kill)
                        {
                            current_team = battle_data.getTeam(i);
                            if (!current_team.HasMon(weather_setter))
                            {
                                current_team.DamagingFieldEffectAndLastUser[weather] = weather_setter;
                            }
                        }
                        PrintUtilities.printString($"\t\t-{weather_setter} set {weather}\n", ConsoleColor.White, ConsoleColor.Black);
                        break;
                    case "-status":
                        // mon got status
                        string[] status_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(status_data[0]);
                        current_poke_nickname = status_data[1].Trim(' ');
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        string status = (battle_data_lines[3]=="tox") ? "psn" : battle_data_lines[3]; // get status bot tox=psn...
                        
                        if(battle_data_lines.Length > 4 && battle_data_lines[4].Contains("item"))
                        {
                            // Cant deal with this right now... Just ignore the status
                        }
                        else if(last_line == "switch" && status == "psn") // Status on switch means hazards, tspikes specifically
                        {
                            if(current_team.DamagingFieldEffectAndLastUser.ContainsKey("Toxic Spikes"))
                            {
                                current_team.PokemonInTeam[current_poke].DamagingEventsAndUser[status] = current_team.DamagingFieldEffectAndLastUser["Toxic Spikes"];
                                // Add tspikes setter to causer
                                PrintUtilities.printString($"\t\t-{current_team.PokemonInTeam[current_poke].Name} got {status} by TSpikes set by {current_team.DamagingFieldEffectAndLastUser["Toxic Spikes"]}\n", ConsoleColor.White, ConsoleColor.Black);
                            }
                        }
                        else // Status caused by an attack
                        {
                            if(!current_team.HasMon(current_attack.user)) // BUT ONLY IF NOT SELF INFLICTED
                            {
                                current_team.PokemonInTeam[current_poke].DamagingEventsAndUser[status] = current_attack.user;
                                PrintUtilities.printString($"\t\t-{current_team.PokemonInTeam[current_poke].Name} got {status} by {current_attack.user}\n", ConsoleColor.White, ConsoleColor.Black);
                            }
                        }
                        break;
                    case "-start":
                        // mon got status, salt cure? what
                        string[] start_data = battle_data_lines[2].Split(':');
                        current_team = battle_data.getTeam(start_data[0]);
                        current_poke_nickname = start_data[1].Trim(' ');
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        string start_effect = "";

                        if (battle_data_lines[3].Contains("move: ")) // move caused this
                        {
                            start_effect = battle_data_lines[3].Replace("move: ","");
                        }
                        else
                        {
                            // Just ignore these for now
                        }

                        switch (start_effect)
                        {
                            case "Future Sight":
                            case "Doom Desire":
                                // These attacks set a "Hazard" on the opp's field, similar to sidestart
                                current_team = battle_data.getOppositeTeam(current_team.TeamNumber); // Set hazard on the opponent's
                                current_team.DamagingFieldEffectAndLastUser[start_effect] = current_poke; // Now there's a field effect that may grant kills
                                PrintUtilities.printString($"\t\t-{current_poke} scheduled a {start_effect} against {current_team.Name}\n", ConsoleColor.White, ConsoleColor.Black);
                                break;
                            case "Leech Seed":
                                // This is caused by an attack hopefully and so we track the user
                                current_team.PokemonInTeam[current_poke].DamagingEventsAndUser[start_effect] = current_attack.user;
                                break;
                            default:
                                break;
                        }
                        break;
                    case "-end":
                        // A status (???) ended, future sight ended
                        string[] end_data = battle_data_lines[2].Split(':');
                        // Who did it end for
                        current_team = battle_data.getTeam(end_data[0]);
                        current_poke_nickname = end_data[1].Trim(' ');
                        current_poke = current_team.GetMonByNickname(current_poke_nickname);
                        string end_effect = "";

                        if (battle_data_lines[3].Contains("move: ")) // move caused this
                        {
                            end_effect = battle_data_lines[3].Replace("move: ", "");
                        }
                        else
                        {
                            // Just ignore these for now
                        }

                        switch (end_effect)
                        {
                            case "Future Sight":
                            case "Doom Desire":
                                // If these finished, it means the move ended as an attack, will populate attack data and someone will check if someone faint
                                // Important to track who caused the move in the first place
                                current_attack.attack_name = end_effect;
                                current_attack.user = current_team.DamagingFieldEffectAndLastUser[end_effect]; // Get caster
                                current_attack.player_user = battle_data.getOppositeTeam(current_team.TeamNumber).TeamNumber;
                                PrintUtilities.printString($"\t-{current_attack.user}'s {end_effect} happened\n", ConsoleColor.White, ConsoleColor.Black);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "-damage": // Part of the event, mon received damage and possibly fainted
                        // We only care about fainting
                        if (battle_data_lines[3].Contains("fnt")) // Target of attack received lethal damage!
                        {
                            // First, we check who died
                            string[] fainted_mon_data = battle_data_lines[2].Split(':');
                            current_team = battle_data.getTeam(fainted_mon_data[0]);
                            int dead_owner = current_team.TeamNumber;
                            current_poke_nickname = fainted_mon_data[1].Trim(' '); // Remove spaces, now i got the mon
                            string dead_poke = current_poke = current_team.GetMonByNickname(current_poke_nickname);
                            
                            // Next, we get the source/killer
                            AttackData damage_source;
                            if (battle_data_lines.Length > 4 && battle_data_lines[4].Contains("[from]")) // Means something fishy happened, damage from somewhere
                            {
                                damage_source = new AttackData();
                                damage_source.attack_name = battle_data_lines[4].Replace("[from] ", "");
                                damage_source.attack_name = damage_source.attack_name.Split(":").Last().Trim(' '); //If there's ability: or item:, remove it too

                                switch (damage_source.attack_name) // Track damage effects and their source...
                                {
                                    case "Leech Seed":
                                        // Need to find who used it
                                        damage_source.user = current_team.PokemonInTeam[dead_poke].DamagingEventsAndUser[damage_source.attack_name];
                                        // And obviously will be the other player!
                                        damage_source.player_user = battle_data.getOppositeTeam(current_team.TeamNumber).TeamNumber;
                                        break;
                                    case "confusion": // Simple case but I do need to check if it wasn't self inflicted
                                    case "psn": // Have to track status kill too! These work in the same way
                                    case "brn":
                                        if (current_team.PokemonInTeam[dead_poke].DamagingEventsAndUser.ContainsKey(damage_source.attack_name))
                                        {
                                            damage_source.user = current_team.PokemonInTeam[dead_poke].DamagingEventsAndUser[damage_source.attack_name];
                                            damage_source.player_user = battle_data.getOppositeTeam(current_team.TeamNumber).TeamNumber;
                                        }
                                        break;
                                    case "Rocky Helmet":
                                    case "Rough Skin":
                                    case "Iron Barbs":
                                    case "Aftermath":
                                        if (battle_data_lines.Length > 5 && battle_data_lines[5].Contains("[of]")) // The game gives us data of who did this
                                        {
                                            battle_data_lines[5] = battle_data_lines[5].Replace("[of] ", ""); // Remove of
                                            current_team = battle_data.getTeam(battle_data_lines[5].Split(':')[0]);
                                            damage_source.player_user = current_team.TeamNumber; // Get owner of killer
                                            damage_source.user = current_team.GetMonByNickname(battle_data_lines[5].Split(':')[1].Trim(' ')); // Get mon that killed with thing
                                        }
                                        break;
                                    default:
                                        PrintUtilities.printString($"\t\t-{dead_poke} died of mysterious circumstances: {damage_source.attack_name}\n", ConsoleColor.Red, ConsoleColor.Black);
                                        break;
                                }
                            }
                            else
                            {
                                // Otherwise good ol direct damage
                                damage_source = current_attack; // Will be only in some cases!
                            }

                            // I discovered who fainted and deduced who did it, now I need to apply kill
                            if(dead_owner != damage_source.player_user)
                            {
                                current_team = battle_data.getTeam(damage_source.player_user); // Find player and mon that killed
                                current_poke = damage_source.user;
                                current_team.PokemonInTeam[current_poke].NumberOfKills++; // Assign kill
                                PrintUtilities.printString($"\t\t-{damage_source.user} killed {dead_poke} with {damage_source.attack_name} ({current_team.PokemonInTeam[current_poke].NumberOfKills} TOTAL KILLS)\n", ConsoleColor.Red, ConsoleColor.Black);
                            }
                        }
                        break;
                    case "-ability": // A mon activated an ability and so it just did something (e.g. terrain on switch in)
                        turn_state_machine.Ability();
                        break;
                    case "-heal": // A mon activated an ability and so it just did something (e.g. terrain on switch in)
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
                PrintUtilities.ExportMonData(outputtext, current_team, diff, turn_state_machine);
                // Empty line
                outputtext.WriteLine();
                // Save for player 2
                current_team = battle_data.getTeam(2);
                PrintUtilities.ExportMonData(outputtext, current_team, diff, turn_state_machine);
            }

            Console.WriteLine("\n----- PRESS ENTER TO FINISH -----");
            Console.ReadLine();
        }
    }
}

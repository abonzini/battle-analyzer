using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BattleAnalyzer
{

    public class TurnCounter // Counts turns in a battle, has a weird logic
    {
        enum TurnState // Keeps a simple track of the moment in the turn, which can get wild if many things happen
        {
            TURN_0,
            TURN_START,
            IN_TURN,
            GAME_END
        }
        public enum EventTypes
        {
            TURN_START,
            SWITCH_IN,
            MOVE,
            FAINT,
            ABILITY,
            HEAL,
            WIN
        }
        public class InternalData // Some things (faint & switch) require player/mon data
        {
            public int number = 0;
            public string text = "";
            public override string ToString()
            {
                return $"{number}:{text}";
            }
        }

        public class MonData
        {
            public MonData(string mon_name)
            {
                name = mon_name;
            }

            public string name = "";
            public bool dead = false;
            public bool acted = false;
            public override string ToString()
            {
                return $"{name}{((acted)?",acted":"")}{((dead) ? ",dead" : "")}";
            }
        }

        TeamData[] teams = new TeamData[2]; // Info about the two teams
        MonData[] current_mons = new MonData[2]; // Contains tuple of mon (and turn info, active this turn and/or dead)
        Dictionary<string, MonData>[] all_mons_this_turn = new Dictionary<string, MonData>[2]; // Historical mon data for this turn
        int turn_number;
        TurnState state;
        BattleData battle_data;
        InternalData internal_data = new InternalData(); // For extra info to pass to state machine

        public TurnCounter(BattleData data) // Keeps internal picture of mons and teams
        {
            state = TurnState.TURN_0;
            battle_data = data;
        }

        void ExecuteStateMachine(EventTypes turn_event)
        {
            bool finished_state_operation = false;
            do
            {
                int player; // AUX VARS
                string mon_name;
                switch (state)
                {
                    case TurnState.TURN_0:
                        if (turn_event == EventTypes.SWITCH_IN) // On switch in, adds the corresponding mon
                        {
                            current_mons[internal_data.number] = new MonData(internal_data.text); // Add the new pokemon to the corresponding player (inactive and not dead)
                            finished_state_operation = true; // Finished query
                        }
                        else if (turn_event == EventTypes.TURN_START) // When turn starts, everything breaks if mons are not existing yet
                        {
                            foreach (MonData mon in current_mons)
                            {
                                if (mon.name == "")
                                {
                                    throw new Exception("Game started but not all mons have beem loaded!");
                                }
                            }
                            state = TurnState.TURN_START;
                            finished_state_operation = false; // Need to do another round as the state changed
                        }
                        else
                        {
                            throw new Exception("Event happened and game hasn't even started!");
                        }
                        break;
                    case TurnState.TURN_START: // In this case, the currently active mons are insta-set as active, lists are cleaned as a new start began
                        for(int i = 0; i < all_mons_this_turn.Length; i++)
                        {
                            all_mons_this_turn[i] = new Dictionary<string, MonData>(); // Empty dict
                        }
                        foreach (MonData mon in current_mons)
                        {
                            if(mon.name == "") // This probably would never happen
                            {
                                throw new Exception("Turn started but not all mons have beem loaded!");
                            }
                            mon.acted = false; // This is Chug's logic, if insta switch, mon hasn't acted yet, switch in will do all the cool stuff (except pursuited i guess)
                        }
                        //Print
                        PrintUtilities.printString($"Turn {turn_number}: {current_mons[0].name} vs {current_mons[1].name}\n", ConsoleColor.Yellow, ConsoleColor.Black);
                        state = TurnState.IN_TURN;
                        finished_state_operation = true; // Finished query
                        break;
                    case TurnState.IN_TURN:
                        switch(turn_event)
                        {
                            case EventTypes.TURN_START:
                                // Ok, fun's over, time to do the tally...
                                for (player = 0; player < current_mons.Length; player++) // Aggregate mons for each player
                                {
                                    if (all_mons_this_turn[player].ContainsKey(current_mons[player].name)) // Mon was already there? Means it left and came back (triple switch)
                                    {
                                        all_mons_this_turn[player][current_mons[player].name].acted |= current_mons[player].acted; // It would've aready acted, weird if it wasn't but hey...
                                    }
                                    else // More likely, mon is added in historic total
                                    {
                                        all_mons_this_turn[player].Add(current_mons[player].name, current_mons[player]); // Good
                                    }
                                    int denominator = 0;
                                    // Now I count how many active mons this player had
                                    foreach (MonData mon in all_mons_this_turn[player].Values)
                                    {
                                        if (mon.acted)
                                        {
                                            denominator++;
                                        }
                                    }
                                    // Finally I add this "split" turn into final mons
                                    TeamData team = battle_data.getTeam(player + 1); // Get team
                                    if(denominator == 0) // Double switch situation, no mon has acted and none have abilities...
                                    {
                                        // In this case the main mon gets all i guess
                                        team.PokemonInTeam[current_mons[player].name].NumberOfTurns += 1.0f;
                                        PrintUtilities.printString($"{current_mons[player].name} now has {team.PokemonInTeam[current_mons[player].name].NumberOfTurns} turns.\n", ConsoleColor.DarkYellow, ConsoleColor.Black);
                                    }
                                    else // Glory is divided by mons who did stuff
                                    {
                                        foreach (MonData mon in all_mons_this_turn[player].Values)
                                        {
                                            if (mon.acted)
                                            {
                                                team.PokemonInTeam[mon.name].NumberOfTurns += (float)1 / denominator; // Add portion of turn
                                            }
                                            PrintUtilities.printString($"{mon.name} now has {team.PokemonInTeam[mon.name].NumberOfTurns} turns.\n", ConsoleColor.DarkYellow, ConsoleColor.Black);
                                        }
                                    }
                                }

                                finished_state_operation = false; // Need to do another round as the state changed
                                state = TurnState.TURN_START;
                                break;
                            case EventTypes.SWITCH_IN: // A switch in means a mon has been active and also is replaced
                                player = internal_data.number; // Player
                                mon_name = internal_data.text; // Mon that came in
                                // First I check the current mon, add to pile
                                if (all_mons_this_turn[player].ContainsKey(current_mons[internal_data.number].name)) // Mon was already there? Means it left and came back (triple switch)
                                {
                                    all_mons_this_turn[player][current_mons[player].name].acted |= current_mons[player].acted;
                                    all_mons_this_turn[player][current_mons[player].name].dead |= current_mons[player].dead;
                                }
                                else // More likely, mon is added in historic total
                                {
                                    all_mons_this_turn[player].Add(current_mons[player].name, current_mons[player]); // Good
                                }
                                if (!current_mons[player].dead)
                                {
                                    PrintUtilities.printString($"{battle_data.getTeam(player+1)}'s {current_mons[player].name} switched out\n", ConsoleColor.White, ConsoleColor.Black);
                                }
                                // Now the new mon
                                if (all_mons_this_turn[player].ContainsKey(mon_name)) // These are weird situations but...
                                {
                                    current_mons[player] = all_mons_this_turn[player][mon_name]; // Get from pile
                                }
                                else // More likely, mon comes in for first time in turn
                                {
                                    current_mons[player] = new MonData(mon_name);
                                }

                                finished_state_operation = true; // Switch in complete
                                break;
                            case EventTypes.FAINT:
                                player = internal_data.number; // Player
                                mon_name = internal_data.text; // Mon that died
                                if (current_mons[player].name == mon_name) // This would be really strange if not
                                {
                                    current_mons[player].dead = true;
                                }
                                if (all_mons_this_turn[player].ContainsKey(mon_name)) // But this would be even weirder (maybe pursuit? idk)
                                {
                                    all_mons_this_turn[player][mon_name].dead = true;
                                }
                                finished_state_operation = true; // Death complete
                                break;
                            case EventTypes.MOVE: // These are easy, no matter what, all witnesses just become active
                            case EventTypes.ABILITY:
                            case EventTypes.HEAL:
                                foreach(MonData mon in current_mons) // All acted
                                {
                                    mon.acted = true;
                                }
                                finished_state_operation = true; // Complete
                                break;
                            case EventTypes.WIN:
                                // Copy pasted because I'm really lazy
                                // Ok, fun's over, time to do the tally...
                                for (player = 0; player < current_mons.Length; player++) // Aggregate mons for each player
                                {
                                    if (all_mons_this_turn[player].ContainsKey(current_mons[player].name)) // Mon was already there? Means it left and came back (triple switch)
                                    {
                                        all_mons_this_turn[player][current_mons[player].name].acted |= current_mons[player].acted; // It would've aready acted, weird if it wasn't but hey...
                                    }
                                    else // More likely, mon is added in historic total
                                    {
                                        all_mons_this_turn[player].Add(current_mons[player].name, current_mons[player]); // Good
                                    }
                                    int denominator = 0;
                                    // Now I count how many active mons this player had
                                    foreach (MonData mon in all_mons_this_turn[player].Values)
                                    {
                                        if (mon.acted)
                                        {
                                            denominator++;
                                        }
                                    }
                                    // Finally I add this "split" turn into final mons
                                    TeamData team = battle_data.getTeam(player + 1); // Get team
                                    foreach (MonData mon in all_mons_this_turn[player].Values)
                                    {
                                        team.PokemonInTeam[mon.name].NumberOfTurns += 1 / denominator; // Add portion of turn
                                        PrintUtilities.printString($"{mon.name} now has {team.PokemonInTeam[mon.name].NumberOfTurns} turns.\n", ConsoleColor.Yellow, ConsoleColor.Black);
                                    }
                                }

                                finished_state_operation = true; // Game ended
                                state = TurnState.GAME_END;
                                break;
                            default:
                                throw new Exception("Turn state machine has a weird event occurring");
                        }
                        break;
                    default:
                        break; // Nothing happens after reached end of fight
                }
            } while (!finished_state_operation); // Can switch from a state to another in one step
        }

        public void StartTurn(int turn) // Need turn number
        {
            turn_number = turn;
            ExecuteStateMachine(EventTypes.TURN_START);
        }

        public void SwitchIn(int player, string mon) // Need to identify which mon switched in
        {
            internal_data.number = player-1;
            internal_data.text = mon;
            ExecuteStateMachine(EventTypes.SWITCH_IN);
        }

        public void Move() // When a move happens, all live moins will increase the turn counter (no matter the target)
        {
            ExecuteStateMachine(EventTypes.MOVE);
        }

        public void Faint(int player, string mon) // Need to identify which mon died
        {
            internal_data.number = player-1;
            internal_data.text = mon;
            ExecuteStateMachine(EventTypes.FAINT);
        }

        public void Ability() // Mons present are active
        {
            ExecuteStateMachine(EventTypes.ABILITY);
        }

        public void Heal()
        {
            ExecuteStateMachine(EventTypes.HEAL);
        }

        public void Win()
        {
            ExecuteStateMachine(EventTypes.WIN);
        }

        public void ChangeMonName(int player, string old_name, string new_name) // For mega for example. Will need to update all turns in here
        {
            player--;
            if (current_mons[player].name == old_name) // Usually the case as megas are the current mon. IDK if there's form change out of main mon
            {
                MonData new_mon = new MonData(new_name); // Replace old with new
                new_mon.acted = current_mons[player].acted;
                new_mon.dead = current_mons[player].dead;
                current_mons[player] = new_mon;
            }
            if (all_mons_this_turn[player].ContainsKey(old_name)) // Old name also here for some reason
            {
                MonData old_mon = all_mons_this_turn[player][old_name];
                MonData new_mon = new MonData(new_name);
                new_mon.acted = old_mon.acted; // Create a mon with same data but different name
                new_mon.dead = old_mon.dead;
                all_mons_this_turn[player].Add(new_name, new_mon); // Replace it in list
                all_mons_this_turn[player].Remove(old_name);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleAnalyzer
{
    static public class PrintUtilities
    {
        public static void printString(string str, ConsoleColor fg_color, ConsoleColor bg_color)
        {
            Console.ForegroundColor = fg_color;
            Console.BackgroundColor = bg_color;
            Console.Write(str);
            Console.ResetColor();
        }
        public static string ExportMonData(TeamData current_team, int diff, TurnCounter turns)
        {
            string str = "";
            str += $"{current_team.Name},W/L,+/-,TT\n";
            str += $"RESULT,{((current_team.Winner) ? 'W' : 'L')},{((current_team.Winner) ? diff : -diff)},{turns.turn_number}\n";
            str += "POKEMON,K,D,Turns\n";
            foreach (PokemonData pokemon in current_team.PokemonInTeam.Values)
            {
                str += $"{pokemon.Name},{pokemon.NumberOfKills},{pokemon.NumberOfDeaths},{pokemon.NumberOfTurns:0.0}\n";
            }
            return str;
        }
    }
}

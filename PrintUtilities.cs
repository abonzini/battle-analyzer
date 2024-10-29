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
        public static void ExportMonData(StreamWriter outputtext, TeamData current_team, int diff)
        {
            outputtext.WriteLine($"{current_team.Name},W/L,+/-,TT");
            outputtext.WriteLine($"RESULT,{((current_team.Winner) ? 'W' : 'L')},{((current_team.Winner) ? diff : -diff)},0"); // TODO TURN NUMBERS
            outputtext.WriteLine("POKEMON,K,D,Turns");
            foreach (PokemonData pokemon in current_team.PokemonInTeam.Values)
            {
                outputtext.WriteLine($"{pokemon.Name},{pokemon.NumberOfKills},{pokemon.NumberOfDeaths},{pokemon.NumberOfTurns:0.0}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreConsole
{
    class MoveType
    {
        public static readonly string[] Types = { "Normal", "Fighting", "Flying", "Poison", "Ground", "Rock", "Bug", "Ghost", "Steel", "Fire", "Water", "Grass", "Electric", "Psychic", "Ice", "Dragon", "Dark", "Fairy" };

       internal int Index;
       public  string Type;
       internal int TypeIndex;

        public static MoveType ReadCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            MoveType moveType = new MoveType
            {
                Index = Convert.ToInt32(values[0]),
                TypeIndex = Convert.ToInt32(values[1])
            };
            moveType.Type = Types[moveType.TypeIndex];
            return moveType;
        }
    }
}

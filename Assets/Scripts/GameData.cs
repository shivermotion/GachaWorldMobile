using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    public int coinCount;
    // Using our SerializableDictionary to store toy collection counts:
    public SerializableDictionary<int, int> toyCollection = new SerializableDictionary<int, int>();

    // Override ToString() to provide a readable summary of the data.
    public override string ToString()
    {
        string output = $"Coin Count: {coinCount}\nCollected Toys:\n";

        if (toyCollection == null || toyCollection.dictionary.Count == 0)
        {
            output += "  None";
        }
        else
        {
            foreach (var pair in toyCollection.dictionary)
            {
                output += $"  Toy ID {pair.Key}: x{pair.Value}\n";
            }
        }

        return output;
    }
}

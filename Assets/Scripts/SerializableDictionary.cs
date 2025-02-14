using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    // The dictionary you’ll use in code
    public Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    // These lists will hold the keys and values for serialization
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    // Before serialization, copy dictionary data into the lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var pair in dictionary)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // After deserialization, rebuild the dictionary from the lists
    public void OnAfterDeserialize()
    {
        dictionary = new Dictionary<TKey, TValue>();
        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            dictionary[keys[i]] = values[i];
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PriorityData
{
    public List<int> order;

    public PriorityData()
    {
        order = new List<int>
        {
            (int)PriorityCriteria.Favorited,
            (int)PriorityCriteria.NotFavorited,
            (int)PriorityCriteria.Unlocked,
            (int)PriorityCriteria.Locked,
            (int)PriorityCriteria.Oddity,
            (int)PriorityCriteria.Exotic,
            (int)PriorityCriteria.Epic,
            (int)PriorityCriteria.Rare,
            (int)PriorityCriteria.Standard,
            (int)PriorityCriteria.Turbocharged,
            (int)PriorityCriteria.NotTurbocharged,
            (int)PriorityCriteria.RecentlyUsed,
            (int)PriorityCriteria.RecentlyAcquired,
            (int)PriorityCriteria.InstanceName
        };
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static PriorityData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new PriorityData();
        return JsonUtility.FromJson<PriorityData>(json);
    }
}

public enum PriorityCriteria
{
    Favorited,
    NotFavorited,
    Unlocked,
    Locked,
    RecentlyUsed,
    RecentlyAcquired,
    InstanceName,
    Oddity,
    Exotic,
    Epic,
    Rare,
    Standard,
    Turbocharged,
    Trashed,
    NotTurbocharged,
    NotTrashed
}
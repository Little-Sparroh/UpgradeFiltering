using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PriorityData
{
    public List<int> order;

    public PriorityData()
    {
        order = new List<int>();
        if (order.Count == 0)
        {
            order.Add((int)PriorityCriteria.Favorited);
            order.Add((int)PriorityCriteria.NotFavorited);
            order.Add((int)PriorityCriteria.Unlocked);
            order.Add((int)PriorityCriteria.Locked);
            order.Add((int)PriorityCriteria.Trashed);
            order.Add((int)PriorityCriteria.NotTrashed);
            order.Add((int)PriorityCriteria.Oddity);
            order.Add((int)PriorityCriteria.Exotic);
            order.Add((int)PriorityCriteria.Epic);
            order.Add((int)PriorityCriteria.Rare);
            order.Add((int)PriorityCriteria.Standard);
            order.Add((int)PriorityCriteria.InstanceName);
            order.Add((int)PriorityCriteria.Turbocharged);
            order.Add((int)PriorityCriteria.NotTurbocharged);
            order.Add((int)PriorityCriteria.RecentlyUsed);
            order.Add((int)PriorityCriteria.RecentlyAcquired);
        }
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

using UnityEngine;

public static class SortHandling
{

    public static void AddPriorityGUI()
    {
        var existing = Object.FindObjectOfType<PriorityGUI>();
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }
        var go = new GameObject("PriorityGUI");
        go.AddComponent<PriorityGUI>();
    }
}

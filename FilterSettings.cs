using System.Collections.Generic;

public enum FavoriteFilter
{
    ShowAll,
    ShowOnlyFavorited,
    HideFavorited
}

public struct FilterSettings
{
    public List<Rarity> HiddenRarities;
    public bool FilterStats;
    public List<string> StatIncludeList;
    public FavoriteFilter FavoriteSetting;
}
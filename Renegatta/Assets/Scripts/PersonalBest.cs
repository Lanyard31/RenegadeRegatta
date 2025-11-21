using UnityEngine;

public static class PersonalBest
{
    private const string Key = "PersonalBestTime";

    public static bool HasBest()
    {
        return PlayerPrefs.HasKey(Key);
    }

    public static float GetBest()
    {
        return PlayerPrefs.GetFloat(Key, float.MaxValue);
    }

    public static bool TrySetBest(float newTime, out float previous)
    {
        previous = GetBest();
        if (newTime < previous)
        {
            PlayerPrefs.SetFloat(Key, newTime);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }
}

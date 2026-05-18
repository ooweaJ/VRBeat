public static class AccuracyCalc
{
    public static float GetAccuracy(int hits, int misses)
    {
        int total = hits + misses;
        return total == 0 ? 1f : (float)hits / total;
    }

    public static string GetRank(float accuracy)
    {
        if (accuracy >= 1.00f) return "SS";
        if (accuracy >= 0.95f) return "S";
        if (accuracy >= 0.90f) return "A";
        if (accuracy >= 0.80f) return "B";
        if (accuracy >= 0.70f) return "C";
        return "D";
    }
}

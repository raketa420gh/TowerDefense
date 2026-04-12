public static class StarCalculator
{
    public static int Calculate(int currentHp, int maxHp)
    {
        if (currentHp <= 0) return 0;
        if (currentHp >= maxHp) return 3;
        return currentHp * 2 >= maxHp ? 2 : 1;
    }
}

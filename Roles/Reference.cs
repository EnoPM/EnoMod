namespace EnoMod.Roles;

public static class Reference
{
    public static Sheriff Sheriff;
    public static Jester Jester;

    public static void Load()
    {
        Sheriff = new Sheriff();
        Jester = new Jester();
    }
}

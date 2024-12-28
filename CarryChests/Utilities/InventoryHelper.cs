namespace LeFauxMods.CarryChest.Utilities;

internal static class InventoryHelper
{
    public static string GetTemporaryId()
    {
        var id = Constants.Prefix + RandomString();
        while (Game1.player.team.globalInventories.ContainsKey(id)
               || Game1.player.team.globalInventoryMutexes.ContainsKey(id))
        {
            id = Constants.Prefix + RandomString();
        }

        return id;
    }

    private static string RandomString(int length = 16)
    {
        var stringChars = new char[length];

        for (var i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = Constants.AlphaNumeric[Game1.random.Next(Constants.AlphaNumeric.Length)];
        }

        return new string(stringChars);
    }
}

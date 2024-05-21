using System.Text;

namespace StereoMix.Security;

public static class IdGenerator
{
    private static readonly Random _random = new();

    public static string GenerateRoomId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public static string GenerateShortRoomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var stringBuilder = new StringBuilder(6);
        for (var i = 0; i < 6; i++)
        {
            // Skip the first 3 random numbers
            for (var j = 0; j < 3; j++)
            {
                _ = _random.Next(chars.Length);
            }

            stringBuilder.Append(chars[_random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }
}

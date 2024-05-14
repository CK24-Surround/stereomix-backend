using System.Text;

namespace StereoMix.Security;

public static class IdGenerator
{
    private static readonly Random _random = new();

    public static string GenerateRoomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var stringBuilder = new StringBuilder(6);
        for (var i = 0; i < 6; i++)
        {
            stringBuilder.Append(chars[_random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }
}

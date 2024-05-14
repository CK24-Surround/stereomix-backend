using Google.Cloud.Firestore;
using Google.Protobuf.Collections;
using StereoMix.Room;

namespace StereoMix.Lobby.Model;

[FirestoreData]
public class RoomConnectionInfo
{
    [FirestoreProperty] public required string Host { get; set; }
    [FirestoreProperty] public required int Port { get; set; }
}

[FirestoreData]
public class RoomPlayer
{
    [FirestoreProperty] public required string UserName { get; set; }
}

public class ConnectionConverter : IFirestoreConverter<Connection>
{
    public object ToFirestore(Connection value)
    {
        return new Dictionary<string, object> { ["host"] = value.Host, ["port"] = value.Port };
    }

    public Connection FromFirestore(object value)
    {
        if (value is not Dictionary<string, object> dictionary)
        {
            throw new ArgumentException("Invalid document");
        }

        return new Connection { Host = (string)dictionary["host"], Port = (int)dictionary["port"] };
    }
}

public class RoomConverter : IFirestoreConverter<Room.Room>
{
    public object ToFirestore(Room.Room value)
    {
        return new Dictionary<string, object>
        {
            ["room_id"] = value.RoomId,
            ["password_encrypted"] = value.PasswordEncrypted,
            ["state"] = value.State,
            ["owner_id"] = value.OwnerId,
            ["config"] = value.Config,
            ["players"] = value.Players,
            ["connection"] = value.Connection
        };
    }

    public Room.Room FromFirestore(object value)
    {
        if (value is not Dictionary<string, object> dictionary)
        {
            throw new ArgumentException("Invalid document");
        }

        var room = new Room.Room
        {
            RoomId = (string)dictionary["room_id"],
            PasswordEncrypted = (string)dictionary["password_encrypted"],
            State = (RoomState)dictionary["state"],
            OwnerId = (string)dictionary["owner_id"],
            Config = (RoomConfig)dictionary["config"],
            Connection = (Connection)dictionary["connection"]
        };
        var players = dictionary["players"] as RepeatedField<Player>;
        room.Players.AddRange(players);

        return room;
    }
}

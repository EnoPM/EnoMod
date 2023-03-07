namespace EnoMod.Kernel;

public class PlayerState
{
    public enum DeathReason
    {
        Kill,
        Vote,
        Suicide,
        Disconnected,
    }

    public PlainShipRoom? LastRoom;

    private SerializablePlayerState _state;

    public byte PlayerId
    {
        get
        {
            return _state.PlayerId;
        }
    }

    public bool IsDead
    {
        get
        {
            return _state.DeathState != null;
        }
    }

    public bool HasTasks
    {
        get
        {
            return _state.TaskState.HasTasks;
        }
    }

    public PlayerState(byte playerId)
    {
        var player = PlayerCache.GetPlayerById(playerId);
        if (player == null)
            throw new EnoModException($"Unable to find player by id {playerId} in PlayerState constructor");

        _state = new SerializablePlayerState
        {
            PlayerId = playerId,
            DeathState = null,
            TaskState = new SerializableTaskState
            {
                HasTasks = true,
            },
        };
    }
}

public class SerializablePlayerState
{
    public byte PlayerId { get; set; }
    public SerializableDeathState? DeathState { get; set; }
    public SerializableTaskState TaskState { get; set; } = new();
}

public class SerializableDeathState
{
    public PlayerState.DeathReason DeathReason { get; set; }
    public byte KillerId { get; set; }
}

public class SerializableTaskState
{
    public bool HasTasks { get; set; }
}

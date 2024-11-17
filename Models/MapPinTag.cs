namespace FlagsRally.Models;

public class MapPinTag
{
    const int ARRIVAL_LOCATION_PIN_ID = 0;
    public int Id{ get; init; }
    public int BoardId { get; init; } = ARRIVAL_LOCATION_PIN_ID;
    public int PinId => BoardId;

    public MapPinTag(int id)
    {
        Id = id;
    }

    public MapPinTag(int id, int boardId)
    {
        Id = id;
        BoardId = boardId;
    }
}

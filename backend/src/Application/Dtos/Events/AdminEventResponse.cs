namespace Application.Dtos.Events;

public class AdminEventResponse : EventResponse
{
    public AdminOwnerResponse? Owner { get; set; }
}

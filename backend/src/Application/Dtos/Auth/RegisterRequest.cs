namespace Application.Dtos.Auth;
public class RegisterRequest
{
    public string Email {get; set;} = default!;
    public string Password {get; set;} = default!;
    public string FirstName {get; set;} = default!;
    public string LastName {get; set;} = default!; 
    public string Role {get; set;} = default!;
}
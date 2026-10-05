using Domain.Entities;

namespace Application.GuestManagement;

internal static class RegistrationAccess
{
    public static void RequireOwner(Event eventDetails, string plannerId)
    {
        if (string.IsNullOrWhiteSpace(plannerId) || eventDetails.OwnerId.ToString() != plannerId)
            throw new RegistrationException(404, "not_found", "Registration resource not found.");
    }

    public static async Task<RegistrationSubmission> GetPublicRegistrationAsync(
        IGuestRegistrationRepository repository,
        IRegistrationTokenGenerator tokens,
        string reference,
        string secret,
        CancellationToken ct)
    {
        RegistrationValidator.ValidatePublicCredential(reference);
        RegistrationValidator.ValidatePublicCredential(secret);
        var registration = await repository.FindPublicRegistrationAsync(reference, ct);
        if (registration is null || !tokens.Matches(secret, registration.StatusSecretHash))
            throw new RegistrationException(404, "not_found", "Registration resource not found.");
        return registration;
    }
}

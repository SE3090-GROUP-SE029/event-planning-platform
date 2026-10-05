using Application.GuestManagement;
using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.ExternalServices;

public sealed class RegistrationSecretProtector : IRegistrationSecretProtector
{
    private readonly IDataProtector protector;

    public RegistrationSecretProtector(IDataProtectionProvider provider)
    {
        protector = provider.CreateProtector("PlanIt.GuestRegistration.StatusSecret.v1");
    }

    public string Protect(string secret) => protector.Protect(secret);

    public string Unprotect(string protectedSecret) => protector.Unprotect(protectedSecret);
}

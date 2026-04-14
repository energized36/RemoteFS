namespace FileExplorer.Services;

public class CredentialService
{
    private readonly string _username;
    private readonly string _password;

    public CredentialService(IConfiguration configuration)
    {
        _username = configuration.GetValue<string>("AdminCredentials:Username") 
                    ?? throw new InvalidOperationException("AdminCredentials:Username is not configured"); 
        _password = configuration.GetValue<string>("AdminCredentials:Password") 
                    ?? throw new InvalidOperationException("AdminCredentials:Password is not configured"); 
    }
}

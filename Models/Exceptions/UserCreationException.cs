using Microsoft.AspNetCore.Identity;

// Custom exception for user creation errors
public class UserCreationException : Exception
{
    public IdentityResult IdentityResult { get; }

    public UserCreationException(IdentityResult identityResult) : base("User creation failed")
    {
        IdentityResult = identityResult;
    }
}
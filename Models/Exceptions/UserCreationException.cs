using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using NewVivaApi.Models;

// Custom exception for user creation errors
public class UserCreationException : Exception
{
    public UserCreationException(RegisterBindingModel bindingModel, IdentityResult result)
        : base("An error occurred while attempting to create a user")
    {
        RegisterBindingModel = bindingModel;
        IdentityResult = result;
    }

    public IdentityResult IdentityResult { get; }
    public RegisterBindingModel RegisterBindingModel { get; }
}

// Note: To easily detect inner exceptions coming from external sources
//       it can help to use a regex to test the error message, you can
//       create other fragments of the ExceptionRegexes class in any 
//       file where a new regex needs to be defined

public static partial class ExceptionRegexes
{
    [GeneratedRegex(@"((E|e)mail){1,}.*is already taken", RegexOptions.Compiled)]
    public static partial Regex UserCreationDuplicateEmailRegex();
}
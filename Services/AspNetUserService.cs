using NewVivaApi.Data;
using NewVivaApi.Models;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Authentication.Models;

namespace NewVivaApi.Services;
public class AspNetUserService
{

    // private readonly ILogger<UserService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IdentityDbContext _identityDbContext;
    public AspNetUserService(
        // ILogger<UserService> logger,
        AppDbContext dbContext,
        IdentityDbContext identityDbContext
        )
    {
        // _logger = logger;
        _dbContext = dbContext;
        _identityDbContext = identityDbContext;

    }

    public async Task<ApplicationUser?> FindUserByUserName(string username) {
        return await _identityDbContext.Users.FirstOrDefaultAsync( user => user.UserName == username );
    }

}
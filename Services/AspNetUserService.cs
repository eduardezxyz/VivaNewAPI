using NewVivaApi.Data;
using NewVivaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace NewVivaApi.Services;
public class AspNetUserService
{

    // private readonly ILogger<UserService> _logger;
    private readonly AppDbContext _dbContext;
    public AspNetUserService(
        // ILogger<UserService> logger,
        AppDbContext dbContext
        )
    {
        // _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<AspNetUser?> FindUserByUserName(string username) {
        return await _dbContext.AspNetUsers.FirstOrDefaultAsync<AspNetUser>( user => user.UserName == username );
    }

}
using Microsoft.EntityFrameworkCore;
using PermitPro.Core.Data;
using PermitPro.Core.Data.DTO;
using PermitPro.Core.Entities;
using PermitPro.Core.Interfaces;

namespace PermitPro.Core.Services;

public class UserService : IUserService
{
	private readonly ApplicationDbContext _dbContext;
	private readonly ICurrentUserService _currentUserService;

	public UserService(
		ApplicationDbContext dbContext
		, ICurrentUserService currentUserService)
	{
		_dbContext = dbContext;
		_currentUserService = currentUserService;
	}


	public Task CreateAsync()
	{
		throw new NotImplementedException();
	}


	public async Task CreateAsync(UserInfo entity)
	{
		_dbContext.Users.Add(entity);
		await _dbContext.SaveChangesAsync();
	}


	public Task UpdateAsync()
	{
		throw new NotImplementedException();
	}


	public async Task UpdateAsync(UserInfo entity)
	{
		_dbContext.Users.Update(entity);
		await _dbContext.SaveChangesAsync();
	}


	public async Task DeleteAsync(UserInfo entity)
	{
		//_dbContext.Users.Remove(entity);
		await _dbContext.SoftDeleteAsync(entity, Guid.Parse(_currentUserService.GetCurrentUser().Id));
		await _dbContext.SaveChangesAsync();
	}


	public UserInfo? GetById(Guid entityId)
	{
		return _dbContext.Users.FirstOrDefault(x => x.Id == entityId.ToString());
	}


	public IEnumerable<UserInfo> GetAll()
	{
		var users = _dbContext.Users
			.Include(e => e.UserRoles)
			.Include(e => e.UserCompany)
			.AsEnumerable();

		return users;
	}


	public IQueryable<UserData> GetAllUsers()
	{
		return _dbContext.Users
			.Include(e => e.UserRoles)
			.Include(e => e.UserCompany)
			.Select(p => new UserData
			{
				Id = p.Id,
				Name = p.UserName!,
				Email = p.Email!,
				FirstName = p.FirstName!,
				LastName = p.LastName!,
				FullName = $"{p.FirstName!.Trim()} {p.LastName!.Trim()}",
				Roles = string.Join(", ", p.UserRoles.Select(r => r.Role!.Name)),
				IsActive = p.IsActive,
				Company = new CompanyData
				{
					Id = p.UserCompany!.Id,
					Name = p.UserCompany.Name,
					Description = p.UserCompany!.Description!
				}
			});
	}

}

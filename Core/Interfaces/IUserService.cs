using PermitPro.Core.Data.DTO;
using PermitPro.Core.Entities;

namespace PermitPro.Core.Interfaces;

public interface IUserService : IEntityOperation<UserInfo>
{
	IQueryable<UserData> GetAllUsers();
}

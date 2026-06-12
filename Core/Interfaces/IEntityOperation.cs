namespace PermitPro.Core.Interfaces;

public interface IEntityOperation<T>
{
	Task CreateAsync();

	Task CreateAsync(T entity);

	Task UpdateAsync();

	Task UpdateAsync(T entity);

	Task DeleteAsync(T entity);

	T? GetById(Guid entityId);

	IEnumerable<T> GetAll();
}

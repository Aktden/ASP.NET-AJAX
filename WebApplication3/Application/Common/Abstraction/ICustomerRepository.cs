using Shop.Domain;

namespace Shop.Application.Common.Abstractions;

public interface ICustomerRepository
{
	Task<Customer?> GetByEmailAsync(string email, CancellationToken ct);
	void Add(Customer customer);
}
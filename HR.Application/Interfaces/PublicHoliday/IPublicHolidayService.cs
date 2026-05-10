using internalEmployee.Auth.Contracts;
using internalEmployee.Data.Entities;
using PublicHolidayEntity = internalEmployee.Data.Entities.PublicHoliday;
using PublicHolidayExceptionEntity = internalEmployee.Data.Entities.PublicHolidayException;

namespace internalEmployee.Services.PublicHoliday;

public interface IPublicHolidayService
{
    Task<List<PublicHolidayResponse>> GetAllAsync(int? year, CancellationToken ct);
    Task<PublicHolidayResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<PublicHolidayEntity> CreateAsync(CreatePublicHolidayRequest request, CancellationToken ct);
    Task<PublicHolidayEntity> UpdateAsync(int id, UpdatePublicHolidayRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task<PublicHolidayExceptionEntity> AddExceptionAsync(int holidayId, CreatePublicHolidayExceptionRequest request, CancellationToken ct);
    Task<List<PublicHolidayExceptionResponse>> GetExceptionsAsync(int holidayId, CancellationToken ct);
    Task DeleteExceptionAsync(int exceptionId, CancellationToken ct);
}

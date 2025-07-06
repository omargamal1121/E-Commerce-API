using E_Commers.DtoModels.OrderDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.Services;

namespace E_Commers.Interfaces
{
	public interface IOrderServices
	{
		Task<Result<OrderDto>> GetOrderByIdAsync(int orderId, string userId);
		Task<Result<OrderDto>> GetOrderByNumberAsync(string orderNumber, string userId);
		Task<Result<List<OrderDto>>> GetCustomerOrdersAsync(string userId, int page = 1, int pageSize = 10);
		Task<Result<List<OrderDto>>> GetOrdersByStatusAsync(OrderStatus status, string userRole, int page = 1, int pageSize = 10);
		Task<Result<OrderDto>> CreateOrderFromCartAsync(string userId, CreateOrderDto orderDto);
		Task<Result<OrderDto>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto statusDto, string userRole);
		Task<Result<string>> CancelOrderAsync(int orderId, CancelOrderDto cancelDto, string userId);
		Task<Result<string>> ShipOrderAsync(int orderId, string userRole);
		Task<Result<string>> DeliverOrderAsync(int orderId, string userRole);
		Task<Result<int?>> GetOrderCountByCustomerAsync(string userId);
		Task<Result<decimal>> GetTotalRevenueByCustomerAsync(string userId);
		Task<Result<decimal>> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, string userRole);
		Task<Result<List<OrderDto>>> GetOrdersWithPaginationAsync(int page, int pageSize, OrderStatus? status, string userRole);
		Task<Result<int?>> GetTotalOrderCountAsync(OrderStatus? status, string userRole);
	}
} 
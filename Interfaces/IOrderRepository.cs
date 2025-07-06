using E_Commers.Enums;
using E_Commers.Models;

namespace E_Commers.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetOrdersByCustomerAsync(string customerId);
        Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status);
        Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status, string? notes = null);
        Task<bool> CancelOrderAsync(int orderId, string cancellationReason);
        Task<bool> ShipOrderAsync(int orderId);
        Task<bool> DeliverOrderAsync(int orderId);
        Task<string> GenerateOrderNumberAsync();
        Task<int> GetOrderCountByCustomerAsync(string customerId);
        Task<decimal> GetTotalRevenueByCustomerAsync(string customerId);
        Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Order>> GetOrdersWithPaginationAsync(int page, int pageSize, OrderStatus? status = null);
        Task<int> GetTotalOrderCountAsync(OrderStatus? status = null);
    }
} 
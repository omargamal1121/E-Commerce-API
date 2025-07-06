using E_Commers.Context;
using E_Commers.Enums;
using E_Commers.Interfaces;
using E_Commers.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commers.Repository
{
    public class OrderRepository : MainRepository<Order>, IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(AppDbContext context, ILogger<OrderRepository> logger) 
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            _logger.LogInformation($"Getting order by ID: {orderId}");
            
            return await _context.Orders
                .Where(o => o.Id == orderId && o.DeletedAt == null)
                .Include(o => o.Customer)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Discount)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Images.Where(img => img.DeletedAt == null))
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentMethod)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            _logger.LogInformation($"Getting order by number: {orderNumber}");
            
            return await _context.Orders
                .Where(o => o.OrderNumber == orderNumber && o.DeletedAt == null)
                .Include(o => o.Customer)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.ProductVariants.Where(v => v.DeletedAt == null))
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Discount)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Images.Where(img => img.DeletedAt == null))
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.ProductVariant)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentMethod)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerId)
        {
            _logger.LogInformation($"Getting orders for customer: {customerId}");
            
            return await _context.Orders
                .Where(o => o.CustomerId == customerId && o.DeletedAt == null)
                .Include(o => o.Customer)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentMethod)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status)
        {
            _logger.LogInformation($"Getting orders by status: {status}");
            
            return await _context.Orders
                .Where(o => o.Status == status && o.DeletedAt == null)
                .Include(o => o.Customer)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentMethod)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation($"Getting orders from {startDate} to {endDate}");
            
            return await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.DeletedAt == null)
                .Include(o => o.Customer)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentMethod)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status, string? notes = null)
        {
            _logger.LogInformation($"Updating order {orderId} status to {status}");
            
            try
            {
                var order = await _context.Orders
                    .Where(o => o.Id == orderId && o.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return false;
                }

                order.Status = status;
                order.ModifiedAt = DateTime.UtcNow;

                // Update specific timestamps based on status
                switch (status)
                {
                    case OrderStatus.Shipped:
                        order.ShippedAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Delivered:
                        order.DeliveredAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAt = DateTime.UtcNow;
                        break;
                }

                if (!string.IsNullOrWhiteSpace(notes))
                {
                    order.Notes = notes;
                }

                _context.Orders.Update(order);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string cancellationReason)
        {
            _logger.LogInformation($"Cancelling order {orderId}");
            
            try
            {
                var order = await _context.Orders
                    .Where(o => o.Id == orderId && o.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogWarning($"Order {orderId} not found");
                    return false;
                }

                if (!order.CanBeCancelled)
                {
                    _logger.LogWarning($"Order {orderId} cannot be cancelled in current status: {order.Status}");
                    return false;
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                order.ModifiedAt = DateTime.UtcNow;
                order.Notes = cancellationReason;

                _context.Orders.Update(order);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cancelling order: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ShipOrderAsync(int orderId)
        {
            return await UpdateOrderStatusAsync(orderId, OrderStatus.Shipped);
        }

        public async Task<bool> DeliverOrderAsync(int orderId)
        {
            return await UpdateOrderStatusAsync(orderId, OrderStatus.Delivered);
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random();
            var randomPart = random.Next(1000, 9999).ToString();
            var orderNumber = $"ORD-{timestamp}-{randomPart}";

            // Ensure uniqueness
            while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
            {
                randomPart = random.Next(1000, 9999).ToString();
                orderNumber = $"ORD-{timestamp}-{randomPart}";
            }

            return orderNumber;
        }

        public async Task<int> GetOrderCountByCustomerAsync(string customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId && o.DeletedAt == null)
                .CountAsync();
        }

        public async Task<decimal> GetTotalRevenueByCustomerAsync(string customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerId == customerId && o.DeletedAt == null && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);
        }

        public async Task<decimal> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.DeletedAt == null && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.Total);
        }

        public async Task<List<Order>> GetOrdersWithPaginationAsync(int page, int pageSize, OrderStatus? status = null)
        {
            var query = _context.Orders
                .Where(o => o.DeletedAt == null)
                .Include(o => o.Customer)
                .Include(o => o.Items.Where(i => i.DeletedAt == null))
                .ThenInclude(i => i.Product)
                .Include(o => o.Payment)
                .ThenInclude(p => p.PaymentMethod);

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalOrderCountAsync(OrderStatus? status = null)
        {
            var query = _context.Orders.Where(o => o.DeletedAt == null);

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }

            return await query.CountAsync();
        }
    }
} 
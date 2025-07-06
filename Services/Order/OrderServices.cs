using AutoMapper;
using E_Commers.DtoModels.OrderDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.ErrorHnadling;
using E_Commers.Interfaces;
using E_Commers.Models;
using E_Commers.Services.AdminOpreationServices;
using E_Commers.Services.Cache;
using E_Commers.UOW;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace E_Commers.Services.Order
{
    public class OrderServices : IOrderServices
    {
        private readonly ILogger<OrderServices> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderRepository _orderRepository;
        private readonly ICartServices _cartServices;
        private readonly IAdminOpreationServices _adminOperationServices;
        private readonly ICacheManager _cacheManager;
        private const string CACHE_TAG_ORDER = "order";

        public OrderServices(
            ILogger<OrderServices> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IOrderRepository orderRepository,
            ICartServices cartServices,
            IAdminOpreationServices adminOperationServices,
            ICacheManager cacheManager)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _orderRepository = orderRepository;
            _cartServices = cartServices;
            _adminOperationServices = adminOperationServices;
            _cacheManager = cacheManager;
        }

        private void NotifyAdminOfError(string message, string? stackTrace = null)
        {
            BackgroundJob.Enqueue<IErrorNotificationService>(_ => _.SendErrorNotificationAsync(message, stackTrace));
        }

        public async Task<Result<OrderDto>> GetOrderByIdAsync(int orderId, string userId)
        {
            _logger.LogInformation($"Getting order by ID: {orderId} for user: {userId}");

            var cacheKey = $"{CACHE_TAG_ORDER}_id_{orderId}_user_{userId}";
            var cached = await _cacheManager.GetAsync<OrderDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation($"Cache hit for order {orderId}");
                return Result<OrderDto>.Ok(cached, "Order retrieved from cache", 200);
            }

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    return Result<OrderDto>.Fail("Order not found", 404);
                }

                // Check if user has access to this order
                if (order.CustomerId != userId)
                {
                    return Result<OrderDto>.Fail("Access denied", 403);
                }

                var orderDto = _mapper.Map<OrderDto>(order);
                await _cacheManager.SetAsync(cacheKey, orderDto, tags: new[] { CACHE_TAG_ORDER });

                return Result<OrderDto>.Ok(orderDto, "Order retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order {orderId}: {ex.Message}");
                NotifyAdminOfError($"Error getting order {orderId}: {ex.Message}", ex.StackTrace);
                return Result<OrderDto>.Fail("An error occurred while retrieving order", 500);
            }
        }

        public async Task<Result<OrderDto>> GetOrderByNumberAsync(string orderNumber, string userId)
        {
            _logger.LogInformation($"Getting order by number: {orderNumber} for user: {userId}");

            try
            {
                var order = await _orderRepository.GetOrderByNumberAsync(orderNumber);
                if (order == null)
                {
                    return Result<OrderDto>.Fail("Order not found", 404);
                }

                // Check if user has access to this order
                if (order.CustomerId != userId)
                {
                    return Result<OrderDto>.Fail("Access denied", 403);
                }

                var orderDto = _mapper.Map<OrderDto>(order);
                return Result<OrderDto>.Ok(orderDto, "Order retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order by number {orderNumber}: {ex.Message}");
                NotifyAdminOfError($"Error getting order by number {orderNumber}: {ex.Message}", ex.StackTrace);
                return Result<OrderDto>.Fail("An error occurred while retrieving order", 500);
            }
        }

        public async Task<Result<List<OrderDto>>> GetCustomerOrdersAsync(string userId, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Getting orders for customer: {userId}, page: {page}");

            try
            {
                var orders = await _orderRepository.GetOrdersWithPaginationAsync(page, pageSize);
                var customerOrders = orders.Where(o => o.CustomerId == userId).ToList();

                var orderDtos = _mapper.Map<List<OrderDto>>(customerOrders);
                return Result<List<OrderDto>>.Ok(orderDtos, "Customer orders retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting customer orders for {userId}: {ex.Message}");
                NotifyAdminOfError($"Error getting customer orders for {userId}: {ex.Message}", ex.StackTrace);
                return Result<List<OrderDto>>.Fail("An error occurred while retrieving customer orders", 500);
            }
        }

        public async Task<Result<List<OrderDto>>> GetOrdersByStatusAsync(OrderStatus status, string userRole, int page = 1, int pageSize = 10)
        {
            _logger.LogInformation($"Getting orders by status: {status} for role: {userRole}");

            if (userRole != "Admin")
            {
                return Result<List<OrderDto>>.Fail("Unauthorized access", 403);
            }

            try
            {
                var orders = await _orderRepository.GetOrdersWithPaginationAsync(page, pageSize, status);
                var orderDtos = _mapper.Map<List<OrderDto>>(orders);
                return Result<List<OrderDto>>.Ok(orderDtos, $"Orders with status {status} retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting orders by status {status}: {ex.Message}");
                NotifyAdminOfError($"Error getting orders by status {status}: {ex.Message}", ex.StackTrace);
                return Result<List<OrderDto>>.Fail("An error occurred while retrieving orders", 500);
            }
        }

        public async Task<Result<OrderDto>> CreateOrderFromCartAsync(string userId, CreateOrderDto orderDto)
        {
            _logger.LogInformation($"Creating order from cart for user: {userId}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Get customer cart
                var cartResult = await _cartServices.GetCartAsync(userId);
                if (!cartResult.Success)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Failed to retrieve cart", 400);
                }

                var cart = cartResult.Data;
                if (cart.IsEmpty)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Cart is empty", 400);
                }

                // Validate payment method and provider
                var paymentMethod = await _unitOfWork.Repository<PaymentMethod>().GetByIdAsync(orderDto.PaymentMethodId);
                if (paymentMethod == null)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Invalid payment method", 400);
                }

                var paymentProvider = await _unitOfWork.Repository<PaymentProvider>().GetByIdAsync(orderDto.PaymentProviderId);
                if (paymentProvider == null)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Invalid payment provider", 400);
                }

                if (!paymentProvider.IsActive)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Payment provider is not active", 400);
                }

                // Generate order number
                var orderNumber = await _orderRepository.GenerateOrderNumberAsync();

                // Create order
                var order = new Order
                {
                    CustomerId = userId,
                    OrderNumber = orderNumber,
                    Status = OrderStatus.Pending,
                    Subtotal = cart.TotalPrice,
                    TaxAmount = orderDto.TaxAmount,
                    ShippingCost = orderDto.ShippingCost,
                    DiscountAmount = orderDto.DiscountAmount,
                    Total = cart.TotalPrice + orderDto.TaxAmount + orderDto.ShippingCost - orderDto.DiscountAmount,
                    Notes = orderDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                var createdOrder = await _orderRepository.CreateAsync(order);
                if (createdOrder == null)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Failed to create order", 500);
                }

                // Create order items from cart items
                foreach (var cartItem in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = createdOrder.Id,
                        ProductId = cartItem.ProductId,
                        ProductVariantId = cartItem.Product?.Variants?.FirstOrDefault()?.Id,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        TotalPrice = cartItem.TotalPrice,
                        OrderedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Repository<OrderItem>().CreateAsync(orderItem);
                }

                // Create payment record
                var payment = new Payment
                {
                    CustomerId = userId,
                    PaymentMethodId = orderDto.PaymentMethodId,
                    PaymentProviderId = orderDto.PaymentProviderId,
                    Amount = createdOrder.Total,
                    PaymentDate = DateTime.UtcNow,
                    OrderId = createdOrder.Id,
                    Status = "Pending"
                };

                await _unitOfWork.Repository<Payment>().CreateAsync(payment);

                // Clear cart
                await _cartServices.ClearCartAsync(userId);

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Created order {orderNumber} from cart",
                    Opreations.AddOpreation,
                    userId,
                    createdOrder.Id
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_ORDER);

                // Get complete order with all details
                var completeOrder = await _orderRepository.GetOrderByIdAsync(createdOrder.Id);
                var orderDto = _mapper.Map<OrderDto>(completeOrder);

                return Result<OrderDto>.Ok(orderDto, "Order created successfully", 201);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error creating order for user {userId}: {ex.Message}");
                NotifyAdminOfError($"Error creating order for user {userId}: {ex.Message}", ex.StackTrace);
                return Result<OrderDto>.Fail("An error occurred while creating order", 500);
            }
        }

        public async Task<Result<OrderDto>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusDto statusDto, string userRole)
        {
            _logger.LogInformation($"Updating order {orderId} status to {statusDto.Status}");

            if (userRole != "Admin")
            {
                return Result<OrderDto>.Fail("Unauthorized access", 403);
            }

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var updateResult = await _orderRepository.UpdateOrderStatusAsync(orderId, statusDto.Status, statusDto.Notes);
                if (!updateResult)
                {
                    await transaction.RollbackAsync();
                    return Result<OrderDto>.Fail("Failed to update order status", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Updated order {orderId} status to {statusDto.Status}",
                    Opreations.UpdateOpreation,
                    "Admin",
                    orderId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_ORDER);

                // Get updated order
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                var orderDto = _mapper.Map<OrderDto>(order);

                return Result<OrderDto>.Ok(orderDto, "Order status updated successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error updating order status for order {orderId}: {ex.Message}");
                NotifyAdminOfError($"Error updating order status for order {orderId}: {ex.Message}", ex.StackTrace);
                return Result<OrderDto>.Fail("An error occurred while updating order status", 500);
            }
        }

        public async Task<Result<string>> CancelOrderAsync(int orderId, CancelOrderDto cancelDto, string userId)
        {
            _logger.LogInformation($"Cancelling order {orderId} by user {userId}");

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Order not found", 404);
                }

                // Check if user has access to this order
                if (order.CustomerId != userId)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Access denied", 403);
                }

                var cancelResult = await _orderRepository.CancelOrderAsync(orderId, cancelDto.CancellationReason);
                if (!cancelResult)
                {
                    await transaction.RollbackAsync();
                    return Result<string>.Fail("Failed to cancel order", 500);
                }

                // Log admin operation
                var adminLog = await _adminOperationServices.AddAdminOpreationAsync(
                    $"Cancelled order {orderId}",
                    Opreations.UpdateOpreation,
                    userId,
                    orderId
                );

                if (!adminLog.Success)
                {
                    _logger.LogWarning($"Failed to log admin operation: {adminLog.Message}");
                }

                await _unitOfWork.CommitAsync();
                await transaction.CommitAsync();

                // Clear cache
                await _cacheManager.RemoveByTagAsync(CACHE_TAG_ORDER);

                return Result<string>.Ok(null, "Order cancelled successfully", 200);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error cancelling order {orderId}: {ex.Message}");
                NotifyAdminOfError($"Error cancelling order {orderId}: {ex.Message}", ex.StackTrace);
                return Result<string>.Fail("An error occurred while cancelling order", 500);
            }
        }

        public async Task<Result<string>> ShipOrderAsync(int orderId, string userRole)
        {
            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            try
            {
                var shipResult = await _orderRepository.ShipOrderAsync(orderId);
                if (!shipResult)
                {
                    return Result<string>.Fail("Failed to ship order", 500);
                }

                await _cacheManager.RemoveByTagAsync(CACHE_TAG_ORDER);
                return Result<string>.Ok(null, "Order shipped successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error shipping order {orderId}: {ex.Message}");
                return Result<string>.Fail("An error occurred while shipping order", 500);
            }
        }

        public async Task<Result<string>> DeliverOrderAsync(int orderId, string userRole)
        {
            if (userRole != "Admin")
            {
                return Result<string>.Fail("Unauthorized access", 403);
            }

            try
            {
                var deliverResult = await _orderRepository.DeliverOrderAsync(orderId);
                if (!deliverResult)
                {
                    return Result<string>.Fail("Failed to deliver order", 500);
                }

                await _cacheManager.RemoveByTagAsync(CACHE_TAG_ORDER);
                return Result<string>.Ok(null, "Order delivered successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error delivering order {orderId}: {ex.Message}");
                return Result<string>.Fail("An error occurred while delivering order", 500);
            }
        }

        public async Task<Result<int>> GetOrderCountByCustomerAsync(string userId)
        {
            try
            {
                var count = await _orderRepository.GetOrderCountByCustomerAsync(userId);
                return Result<int>.Ok(count, "Order count retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order count for user {userId}: {ex.Message}");
                return Result<int>.Fail("An error occurred while getting order count", 500);
            }
        }

        public async Task<Result<decimal>> GetTotalRevenueByCustomerAsync(string userId)
        {
            try
            {
                var revenue = await _orderRepository.GetTotalRevenueByCustomerAsync(userId);
                return Result<decimal>.Ok(revenue, "Total revenue retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting total revenue for user {userId}: {ex.Message}");
                return Result<decimal>.Fail("An error occurred while getting total revenue", 500);
            }
        }

        public async Task<Result<decimal>> GetTotalRevenueByDateRangeAsync(DateTime startDate, DateTime endDate, string userRole)
        {
            if (userRole != "Admin")
            {
                return Result<decimal>.Fail("Unauthorized access", 403);
            }

            try
            {
                var revenue = await _orderRepository.GetTotalRevenueByDateRangeAsync(startDate, endDate);
                return Result<decimal>.Ok(revenue, "Total revenue retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting total revenue for date range: {ex.Message}");
                return Result<decimal>.Fail("An error occurred while getting total revenue", 500);
            }
        }

        public async Task<Result<List<OrderDto>>> GetOrdersWithPaginationAsync(int page, int pageSize, OrderStatus? status, string userRole)
        {
            if (userRole != "Admin")
            {
                return Result<List<OrderDto>>.Fail("Unauthorized access", 403);
            }

            try
            {
                var orders = await _orderRepository.GetOrdersWithPaginationAsync(page, pageSize, status);
                var orderDtos = _mapper.Map<List<OrderDto>>(orders);
                return Result<List<OrderDto>>.Ok(orderDtos, "Orders retrieved successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting orders with pagination: {ex.Message}");
                return Result<List<OrderDto>>.Fail("An error occurred while retrieving orders", 500);
            }
        }

        public async Task<Result<int>> GetTotalOrderCountAsync(OrderStatus? status, string userRole)
        {
            if (userRole != "Admin")
            {
                return Result<int>.Fail("Unauthorized access", 403);
            }

            try
            {
                var count = await _orderRepository.GetTotalOrderCountAsync(status);
                return Result<int>.Ok(count, "Total order count retrieved", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting total order count: {ex.Message}");
                return Result<int>.Fail("An error occurred while getting total order count", 500);
            }
        }
    }
} 
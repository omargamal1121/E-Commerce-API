using E_Commers.DtoModels.OrderDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Enums;
using E_Commers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderServices _orderServices;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderServices orderServices, ILogger<OrderController> logger)
        {
            _orderServices = orderServices;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderById(int orderId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _orderServices.GetOrderByIdAsync(orderId, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<OrderDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOrderById: {ex.Message}");
                return StatusCode(500, new ApiResponse<OrderDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the order",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get order by order number
        /// </summary>
        [HttpGet("number/{orderNumber}")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrderByNumber(string orderNumber)
        {
            try
            {
                var userId = GetUserId();
                var result = await _orderServices.GetOrderByNumberAsync(orderNumber, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<OrderDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOrderByNumber: {ex.Message}");
                return StatusCode(500, new ApiResponse<OrderDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the order",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get customer orders with pagination
        /// </summary>
        [HttpGet("customer")]
        public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetCustomerOrders(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                var result = await _orderServices.GetCustomerOrdersAsync(userId, page, pageSize);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<OrderDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<OrderDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCustomerOrders: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<OrderDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving customer orders",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Create order from cart
        /// </summary>
        [HttpPost("create-from-cart")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrderFromCart([FromBody] CreateOrderDto orderDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        StatusCode = 400
                    });
                }

                var userId = GetUserId();
                var result = await _orderServices.CreateOrderFromCartAsync(userId, orderDto);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return StatusCode(201, new ApiResponse<OrderDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 201
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateOrderFromCart: {ex.Message}");
                return StatusCode(500, new ApiResponse<OrderDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the order",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Cancel order
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        public async Task<ActionResult<ApiResponse<string>>> CancelOrder(int orderId, [FromBody] CancelOrderDto cancelDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        StatusCode = 400
                    });
                }

                var userId = GetUserId();
                var result = await _orderServices.CancelOrderAsync(orderId, cancelDto, userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<string>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = result.Message,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CancelOrder: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while cancelling the order",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get order count for customer
        /// </summary>
        [HttpGet("customer/count")]
        public async Task<ActionResult<ApiResponse<int>>> GetOrderCount()
        {
            try
            {
                var userId = GetUserId();
                var result = await _orderServices.GetOrderCountByCustomerAsync(userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<int>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOrderCount: {ex.Message}");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while getting order count",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get total revenue for customer
        /// </summary>
        [HttpGet("customer/revenue")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetCustomerRevenue()
        {
            try
            {
                var userId = GetUserId();
                var result = await _orderServices.GetTotalRevenueByCustomerAsync(userId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<decimal>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<decimal>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCustomerRevenue: {ex.Message}");
                return StatusCode(500, new ApiResponse<decimal>
                {
                    Success = false,
                    Message = "An error occurred while getting customer revenue",
                    StatusCode = 500
                });
            }
        }

        // Admin-only endpoints
        /// <summary>
        /// Get orders by status (Admin only)
        /// </summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrdersByStatus(
            OrderStatus status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _orderServices.GetOrdersByStatusAsync(status, userRole, page, pageSize);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<OrderDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<OrderDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOrdersByStatus: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<OrderDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving orders",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Update order status (Admin only)
        /// </summary>
        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(
            int orderId, 
            [FromBody] UpdateOrderStatusDto statusDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        StatusCode = 400
                    });
                }

                var userRole = GetUserRole();
                var result = await _orderServices.UpdateOrderStatusAsync(orderId, statusDto, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<OrderDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<OrderDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateOrderStatus: {ex.Message}");
                return StatusCode(500, new ApiResponse<OrderDto>
                {
                    Success = false,
                    Message = "An error occurred while updating order status",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Ship order (Admin only)
        /// </summary>
        [HttpPost("{orderId}/ship")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> ShipOrder(int orderId)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _orderServices.ShipOrderAsync(orderId, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<string>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = result.Message,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ShipOrder: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while shipping the order",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Deliver order (Admin only)
        /// </summary>
        [HttpPost("{orderId}/deliver")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> DeliverOrder(int orderId)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _orderServices.DeliverOrderAsync(orderId, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<string>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = result.Message,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in DeliverOrder: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while delivering the order",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get total revenue by date range (Admin only)
        /// </summary>
        [HttpGet("revenue")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetRevenueByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _orderServices.GetTotalRevenueByDateRangeAsync(startDate, endDate, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<decimal>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<decimal>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetRevenueByDateRange: {ex.Message}");
                return StatusCode(500, new ApiResponse<decimal>
                {
                    Success = false,
                    Message = "An error occurred while getting revenue",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get orders with pagination (Admin only)
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrdersWithPagination(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] OrderStatus? status = null)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _orderServices.GetOrdersWithPaginationAsync(page, pageSize, status, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<OrderDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<OrderDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetOrdersWithPagination: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<OrderDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving orders",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get total order count (Admin only)
        /// </summary>
        [HttpGet("admin/count")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<int>>> GetTotalOrderCount(
            [FromQuery] OrderStatus? status = null)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _orderServices.GetTotalOrderCountAsync(status, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<int>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetTotalOrderCount: {ex.Message}");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while getting order count",
                    StatusCode = 500
                });
            }
        }
    }
} 
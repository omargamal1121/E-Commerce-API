using E_Commers.DtoModels.CartDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartServices _cartServices;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartServices cartServices, ILogger<CartController> logger)
        {
            _cartServices = cartServices;
            _logger = logger;
        }

        /// <summary>
        /// Get the current user's cart
        /// </summary>
        /// <returns>Cart details with items</returns>
        [HttpGet]
        public async Task<ActionResult<Result<CartDto>>> GetCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<CartDto>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.GetCartAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCart: {ex.Message}");
                return StatusCode(500, new Result<CartDto>().Fail("An error occurred while retrieving cart", 500));
            }
        }

        /// <summary>
        /// Add an item to the cart
        /// </summary>
        /// <param name="itemDto">Item details to add</param>
        /// <returns>Updated cart</returns>
        [HttpPost("add-item")]
        public async Task<ActionResult<Result<CartDto>>> AddItemToCart([FromBody] CreateCartItemDto itemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new Result<CartDto>().Fail("Invalid input data", 400));
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<CartDto>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.AddItemToCartAsync(userId, itemDto);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in AddItemToCart: {ex.Message}");
                return StatusCode(500, new Result<CartDto>().Fail("An error occurred while adding item to cart", 500));
            }
        }

        /// <summary>
        /// Update the quantity of an item in the cart
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="itemDto">Updated item details</param>
        /// <param name="productVariantId">Product variant ID (optional)</param>
        /// <returns>Updated cart</returns>
        [HttpPut("update-item/{productId}")]
        public async Task<ActionResult<Result<CartDto>>> UpdateCartItem(
            int productId, 
            [FromBody] UpdateCartItemDto itemDto,
            [FromQuery] int? productVariantId = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new Result<CartDto>().Fail("Invalid input data", 400));
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<CartDto>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.UpdateCartItemAsync(userId, productId, itemDto, productVariantId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateCartItem: {ex.Message}");
                return StatusCode(500, new Result<CartDto>().Fail("An error occurred while updating cart item", 500));
            }
        }

        /// <summary>
        /// Remove an item from the cart
        /// </summary>
        /// <param name="itemDto">Item details to remove</param>
        /// <returns>Updated cart</returns>
        [HttpDelete("remove-item")]
        public async Task<ActionResult<Result<CartDto>>> RemoveItemFromCart([FromBody] RemoveCartItemDto itemDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new Result<CartDto>().Fail("Invalid input data", 400));
                }

                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<CartDto>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.RemoveItemFromCartAsync(userId, itemDto);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in RemoveItemFromCart: {ex.Message}");
                return StatusCode(500, new Result<CartDto>().Fail("An error occurred while removing item from cart", 500));
            }
        }

        /// <summary>
        /// Clear all items from the cart
        /// </summary>
        /// <returns>Success message</returns>
        [HttpDelete("clear")]
        public async Task<ActionResult<Result<string>>> ClearCart()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<string>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.ClearCartAsync(userId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return StatusCode(result.StatusCode, result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ClearCart: {ex.Message}");
                return StatusCode(500, new Result<string>().Fail("An error occurred while clearing cart", 500));
            }
        }

        /// <summary>
        /// Get the total number of items in the cart
        /// </summary>
        /// <returns>Item count</returns>
        [HttpGet("item-count")]
        public async Task<ActionResult<Result<int>>> GetCartItemCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<int>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.GetCartItemCountAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCartItemCount: {ex.Message}");
                return StatusCode(500, new Result<int>().Fail("An error occurred while getting cart item count", 500));
            }
        }

        /// <summary>
        /// Get the total price of all items in the cart
        /// </summary>
        /// <returns>Total price</returns>
        [HttpGet("total-price")]
        public async Task<ActionResult<Result<decimal>>> GetCartTotalPrice()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<decimal>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.GetCartTotalPriceAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCartTotalPrice: {ex.Message}");
                return StatusCode(500, new Result<decimal>().Fail("An error occurred while getting cart total price", 500));
            }
        }

        /// <summary>
        /// Check if the cart is empty
        /// </summary>
        /// <returns>True if cart is empty, false otherwise</returns>
        [HttpGet("is-empty")]
        public async Task<ActionResult<Result<bool>>> IsCartEmpty()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new Result<bool>().Fail("User not authenticated", 401));
                }

                var result = await _cartServices.IsCartEmptyAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in IsCartEmpty: {ex.Message}");
                return StatusCode(500, new Result<bool>().Fail("An error occurred while checking cart status", 500));
            }
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
} 
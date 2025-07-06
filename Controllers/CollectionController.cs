using E_Commers.DtoModels.CollectionDtos;
using E_Commers.DtoModels.Responses;
using E_Commers.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionController : ControllerBase
    {
        private readonly ICollectionServices _collectionServices;
        private readonly ILogger<CollectionController> _logger;

        public CollectionController(ICollectionServices collectionServices, ILogger<CollectionController> logger)
        {
            _collectionServices = collectionServices;
            _logger = logger;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        }

        /// <summary>
        /// Get collection by ID
        /// </summary>
        [HttpGet("{collectionId}")]
        public async Task<ActionResult<ApiResponse<CollectionDto>>> GetCollectionById(int collectionId)
        {
            try
            {
                var result = await _collectionServices.GetCollectionByIdAsync(collectionId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<CollectionDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<CollectionDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollectionById: {ex.Message}");
                return StatusCode(500, new ApiResponse<CollectionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get collection by name
        /// </summary>
        [HttpGet("name/{name}")]
        public async Task<ActionResult<ApiResponse<CollectionDto>>> GetCollectionByName(string name)
        {
            try
            {
                var result = await _collectionServices.GetCollectionByNameAsync(name);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<CollectionDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<CollectionDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollectionByName: {ex.Message}");
                return StatusCode(500, new ApiResponse<CollectionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get active collections
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> GetActiveCollections()
        {
            try
            {
                var result = await _collectionServices.GetActiveCollectionsAsync();

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<CollectionDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<CollectionDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetActiveCollections: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<CollectionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving active collections",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get collections by display order
        /// </summary>
        [HttpGet("ordered")]
        public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> GetCollectionsByDisplayOrder()
        {
            try
            {
                var result = await _collectionServices.GetCollectionsByDisplayOrderAsync();

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<CollectionDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<CollectionDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollectionsByDisplayOrder: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<CollectionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving collections",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get collections with pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> GetCollections(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var result = await _collectionServices.GetCollectionsWithPaginationAsync(page, pageSize, isActive);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<CollectionDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<CollectionDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollections: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<CollectionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving collections",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get total collection count
        /// </summary>
        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<int>>> GetCollectionCount([FromQuery] bool? isActive = null)
        {
            try
            {
                var result = await _collectionServices.GetTotalCollectionCountAsync(isActive);

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
                _logger.LogError($"Error in GetCollectionCount: {ex.Message}");
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while getting collection count",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Search collections
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> SearchCollections([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new ApiResponse<List<CollectionDto>>
                    {
                        Success = false,
                        Message = "Search term is required",
                        StatusCode = 400
                    });
                }

                var result = await _collectionServices.SearchCollectionsAsync(searchTerm);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<CollectionDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<CollectionDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SearchCollections: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<CollectionDto>>
                {
                    Success = false,
                    Message = "An error occurred while searching collections",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get collections by product
        /// </summary>
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> GetCollectionsByProduct(int productId)
        {
            try
            {
                var result = await _collectionServices.GetCollectionsByProductAsync(productId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<CollectionDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<CollectionDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollectionsByProduct: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<CollectionDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving collections",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get collection summary
        /// </summary>
        [HttpGet("{collectionId}/summary")]
        public async Task<ActionResult<ApiResponse<CollectionSummaryDto>>> GetCollectionSummary(int collectionId)
        {
            try
            {
                var result = await _collectionServices.GetCollectionSummaryAsync(collectionId);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<CollectionSummaryDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<CollectionSummaryDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollectionSummary: {ex.Message}");
                return StatusCode(500, new ApiResponse<CollectionSummaryDto>
                {
                    Success = false,
                    Message = "An error occurred while getting collection summary",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Get collection summaries
        /// </summary>
        [HttpGet("summaries")]
        public async Task<ActionResult<ApiResponse<List<CollectionSummaryDto>>>> GetCollectionSummaries(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var result = await _collectionServices.GetCollectionSummariesAsync(page, pageSize, isActive);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<List<CollectionSummaryDto>>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<List<CollectionSummaryDto>>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetCollectionSummaries: {ex.Message}");
                return StatusCode(500, new ApiResponse<List<CollectionSummaryDto>>
                {
                    Success = false,
                    Message = "An error occurred while getting collection summaries",
                    StatusCode = 500
                });
            }
        }

        // Admin-only endpoints
        /// <summary>
        /// Create collection (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CollectionDto>>> CreateCollection([FromBody] CreateCollectionDto collectionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CollectionDto>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        StatusCode = 400
                    });
                }

                var userRole = GetUserRole();
                var result = await _collectionServices.CreateCollectionAsync(collectionDto, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<CollectionDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return StatusCode(201, new ApiResponse<CollectionDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 201
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateCollection: {ex.Message}");
                return StatusCode(500, new ApiResponse<CollectionDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Update collection (Admin only)
        /// </summary>
        [HttpPut("{collectionId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CollectionDto>>> UpdateCollection(
            int collectionId,
            [FromBody] UpdateCollectionDto collectionDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CollectionDto>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        StatusCode = 400
                    });
                }

                var userRole = GetUserRole();
                var result = await _collectionServices.UpdateCollectionAsync(collectionId, collectionDto, userRole);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new ApiResponse<CollectionDto>
                    {
                        Success = false,
                        Message = result.Message,
                        StatusCode = result.StatusCode
                    });
                }

                return Ok(new ApiResponse<CollectionDto>
                {
                    Success = true,
                    Message = result.Message,
                    Data = result.Data,
                    StatusCode = 200
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in UpdateCollection: {ex.Message}");
                return StatusCode(500, new ApiResponse<CollectionDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Delete collection (Admin only)
        /// </summary>
        [HttpDelete("{collectionId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteCollection(int collectionId)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _collectionServices.DeleteCollectionAsync(collectionId, userRole);

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
                _logger.LogError($"Error in DeleteCollection: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while deleting the collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Add products to collection (Admin only)
        /// </summary>
        [HttpPost("{collectionId}/products")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> AddProductsToCollection(
            int collectionId,
            [FromBody] AddProductsToCollectionDto productsDto)
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

                var userRole = GetUserRole();
                var result = await _collectionServices.AddProductsToCollectionAsync(collectionId, productsDto, userRole);

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
                _logger.LogError($"Error in AddProductsToCollection: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while adding products to collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Remove products from collection (Admin only)
        /// </summary>
        [HttpDelete("{collectionId}/products")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> RemoveProductsFromCollection(
            int collectionId,
            [FromBody] RemoveProductsFromCollectionDto productsDto)
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

                var userRole = GetUserRole();
                var result = await _collectionServices.RemoveProductsFromCollectionAsync(collectionId, productsDto, userRole);

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
                _logger.LogError($"Error in RemoveProductsFromCollection: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while removing products from collection",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Update collection status (Admin only)
        /// </summary>
        [HttpPut("{collectionId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateCollectionStatus(
            int collectionId,
            [FromBody] bool isActive)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _collectionServices.UpdateCollectionStatusAsync(collectionId, isActive, userRole);

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
                _logger.LogError($"Error in UpdateCollectionStatus: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while updating collection status",
                    StatusCode = 500
                });
            }
        }

        /// <summary>
        /// Update collection display order (Admin only)
        /// </summary>
        [HttpPut("{collectionId}/display-order")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateCollectionDisplayOrder(
            int collectionId,
            [FromBody] int displayOrder)
        {
            try
            {
                var userRole = GetUserRole();
                var result = await _collectionServices.UpdateCollectionDisplayOrderAsync(collectionId, displayOrder, userRole);

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
                _logger.LogError($"Error in UpdateCollectionDisplayOrder: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "An error occurred while updating collection display order",
                    StatusCode = 500
                });
            }
        }
    }
} 
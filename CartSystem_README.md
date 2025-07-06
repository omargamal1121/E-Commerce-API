# Cart System Implementation

## Overview
A complete shopping cart system for the E-Commerce application with full CRUD operations, caching, admin logging, and proper error handling.

## Architecture

### Models
- **Cart**: Main cart entity with user relationship and calculated properties
- **CartItem**: Individual items in the cart with product and variant support

### DTOs
- **CartDto**: Complete cart representation with items and totals
- **CartItemDto**: Individual cart item with pricing information
- **CreateCartItemDto**: For adding items to cart
- **UpdateCartItemDto**: For updating item quantities
- **RemoveCartItemDto**: For removing items from cart

### Repository Layer
- **ICartRepository**: Interface defining cart data access methods
- **CartRepository**: Implementation with Entity Framework and proper includes

### Service Layer
- **ICartServices**: Interface defining cart business logic
- **CartServices**: Implementation with transaction management, caching, and admin logging

### Controller Layer
- **CartController**: RESTful API endpoints with proper authorization

## Features

### ✅ Core Functionality
- **Get Cart**: Retrieve user's cart with all items and calculated totals
- **Add Item**: Add products/variants to cart with quantity validation
- **Update Item**: Modify item quantities with stock validation
- **Remove Item**: Remove specific items from cart
- **Clear Cart**: Remove all items from cart
- **Cart Info**: Get item count, total price, and empty status

### ✅ Business Logic
- **Stock Validation**: Ensures requested quantities are available
- **Price Calculation**: Automatic calculation with discount application
- **Variant Support**: Handles product variants with different prices
- **Quantity Limits**: Enforces reasonable quantity limits (1-100)
- **Duplicate Handling**: Merges duplicate items by adding quantities

### ✅ Technical Features
- **Transaction Management**: Ensures data consistency
- **Caching**: Redis-based caching for performance
- **Admin Logging**: Tracks all cart modifications
- **Error Handling**: Comprehensive error handling with rollback
- **Authorization**: Requires user authentication
- **Validation**: Input validation with proper error messages

## API Endpoints

### GET `/api/cart`
- **Description**: Get the current user's cart
- **Authorization**: Required
- **Response**: CartDto with items and totals

### POST `/api/cart/add-item`
- **Description**: Add an item to the cart
- **Authorization**: Required
- **Body**: CreateCartItemDto
- **Response**: Updated CartDto

### PUT `/api/cart/update-item/{productId}`
- **Description**: Update item quantity
- **Authorization**: Required
- **Parameters**: productId, productVariantId (optional)
- **Body**: UpdateCartItemDto
- **Response**: Updated CartDto

### DELETE `/api/cart/remove-item`
- **Description**: Remove an item from cart
- **Authorization**: Required
- **Body**: RemoveCartItemDto
- **Response**: Updated CartDto

### DELETE `/api/cart/clear`
- **Description**: Clear all items from cart
- **Authorization**: Required
- **Response**: Success message

### GET `/api/cart/item-count`
- **Description**: Get total number of items in cart
- **Authorization**: Required
- **Response**: Item count

### GET `/api/cart/total-price`
- **Description**: Get total price of cart
- **Authorization**: Required
- **Response**: Total price

### GET `/api/cart/is-empty`
- **Description**: Check if cart is empty
- **Authorization**: Required
- **Response**: Boolean status

## Data Models

### Cart Model
```csharp
public class Cart : BaseEntity
{
    public string UserId { get; set; }
    public string CustomerId { get; set; }
    public Customer Customer { get; set; }
    public List<CartItem> Items { get; set; }
    
    // Calculated properties
    public bool IsEmpty => !Items.Any();
    public int TotalItems => Items.Sum(item => item.Quantity);
    public decimal TotalPrice => Items.Sum(item => CalculateItemTotal(item));
}
```

### CartItem Model
```csharp
public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public Cart Cart { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; }
}
```

## Configuration Required

### 1. Dependency Injection
Add the following services to your `Program.cs` or `Startup.cs`:

```csharp
// Register repositories
services.AddScoped<ICartRepository, CartRepository>();

// Register services
services.AddScoped<ICartServices, CartServices>();
```

### 2. Database Context
Ensure your `AppDbContext` includes:

```csharp
public DbSet<Cart> Carts { get; set; }
public DbSet<CartItem> CartItems { get; set; }
```

### 3. AutoMapper
The mapping profile is already configured in `MappingProfile.cs`.

## Usage Examples

### Adding an Item to Cart
```http
POST /api/cart/add-item
Authorization: Bearer {token}
Content-Type: application/json

{
  "productId": 1,
  "quantity": 2,
  "productVariantId": 5
}
```

### Updating Item Quantity
```http
PUT /api/cart/update-item/1?productVariantId=5
Authorization: Bearer {token}
Content-Type: application/json

{
  "quantity": 3
}
```

### Removing an Item
```http
DELETE /api/cart/remove-item
Authorization: Bearer {token}
Content-Type: application/json

{
  "productId": 1,
  "productVariantId": 5
}
```

## Error Handling

The system handles various error scenarios:

- **400 Bad Request**: Invalid input data or insufficient stock
- **401 Unauthorized**: User not authenticated
- **404 Not Found**: Product, variant, or cart not found
- **500 Internal Server Error**: Database or system errors

## Performance Features

- **Caching**: Cart data is cached in Redis for fast retrieval
- **Eager Loading**: Proper Entity Framework includes for related data
- **Transaction Management**: Ensures data consistency
- **Soft Delete**: Items are soft deleted for audit trails

## Security Features

- **Authorization**: All endpoints require user authentication
- **Input Validation**: Comprehensive validation of all inputs
- **Stock Validation**: Prevents overselling
- **Admin Logging**: Tracks all cart modifications for audit

## Testing Considerations

When testing the cart system, consider:

1. **Stock Validation**: Test with insufficient stock scenarios
2. **Variant Handling**: Test with and without product variants
3. **Duplicate Items**: Test adding the same item multiple times
4. **Price Calculation**: Verify discount application
5. **Transaction Rollback**: Test error scenarios
6. **Cache Invalidation**: Verify cache is cleared after modifications

## Future Enhancements

Potential improvements for the cart system:

1. **Cart Expiration**: Automatic cart cleanup after inactivity
2. **Save for Later**: Save items for future purchase
3. **Cart Sharing**: Share cart with other users
4. **Bulk Operations**: Add/remove multiple items at once
5. **Cart Templates**: Predefined cart configurations
6. **Real-time Updates**: WebSocket integration for live cart updates 
# Order Management System

## Overview

The Order Management System is a comprehensive solution for handling e-commerce orders, payments, and order lifecycle management. It integrates seamlessly with the existing Cart system and provides full order processing capabilities from creation to delivery.

## Architecture

### Core Components

1. **Models**
   - `Order`: Main order entity with status tracking and financial details
   - `OrderItem`: Individual items within an order
   - `Payment`: Payment information and processing details
   - `PaymentMethod`: Available payment methods (Credit Card, PayPal, etc.)
   - `PaymentProvider`: Payment service providers (Stripe, PayPal, etc.)

2. **DTOs**
   - `OrderDto`: Complete order information for API responses
   - `OrderItemDto`: Order item details
   - `PaymentDto`: Payment information
   - `CreateOrderDto`: Order creation request
   - `UpdateOrderStatusDto`: Status update requests
   - `CancelOrderDto`: Order cancellation with reason

3. **Repository Layer**
   - `IOrderRepository`: Data access interface
   - `OrderRepository`: Implementation with Entity Framework

4. **Service Layer**
   - `IOrderServices`: Business logic interface
   - `OrderServices`: Implementation with transaction management

5. **Controller Layer**
   - `OrderController`: RESTful API endpoints

## Features

### Order Management
- **Order Creation**: Convert cart items to orders with payment processing
- **Status Tracking**: Complete order lifecycle from Pending to Delivered
- **Order Cancellation**: Customer-initiated cancellations with reason tracking
- **Order History**: Customer order history with pagination
- **Order Search**: Find orders by ID, number, or status

### Payment Integration
- **Multiple Payment Methods**: Support for various payment options
- **Payment Provider Integration**: Extensible payment provider system
- **Payment Status Tracking**: Monitor payment processing status
- **Secure Payment Handling**: Proper payment information management

### Admin Features
- **Order Status Management**: Admin can update order statuses
- **Shipping Management**: Mark orders as shipped and delivered
- **Revenue Analytics**: Track revenue by customer and date ranges
- **Order Analytics**: Comprehensive order statistics and reporting

### Security & Validation
- **Authorization**: Role-based access control (Customer/Admin)
- **Data Validation**: Comprehensive input validation
- **Transaction Safety**: Database transaction management
- **Error Handling**: Robust error handling and logging

## Order Status Flow

```
Pending → Confirmed → Processing → Shipped → Delivered
    ↓
Cancelled
    ↓
Refunded/Returned
```

### Status Descriptions
- **Pending**: Order created, awaiting confirmation
- **Confirmed**: Order confirmed, payment processed
- **Processing**: Order being prepared for shipping
- **Shipped**: Order shipped to customer
- **Delivered**: Order successfully delivered
- **Cancelled**: Order cancelled (customer or admin)
- **Refunded**: Payment refunded to customer
- **Returned**: Items returned by customer

## API Endpoints

### Customer Endpoints

#### Get Order Information
```http
GET /api/order/{orderId}
GET /api/order/number/{orderNumber}
```

#### Customer Orders
```http
GET /api/order/customer?page=1&pageSize=10
GET /api/order/customer/count
GET /api/order/customer/revenue
```

#### Order Management
```http
POST /api/order/create-from-cart
POST /api/order/{orderId}/cancel
```

### Admin Endpoints

#### Order Management
```http
GET /api/order/admin?page=1&pageSize=10&status=Pending
GET /api/order/admin/count?status=Pending
PUT /api/order/{orderId}/status
POST /api/order/{orderId}/ship
POST /api/order/{orderId}/deliver
```

#### Analytics
```http
GET /api/order/revenue?startDate=2024-01-01&endDate=2024-12-31
GET /api/order/status/{status}?page=1&pageSize=10
```

## Data Models

### Order Model
```csharp
public class Order : BaseEntity
{
    public string CustomerId { get; set; }
    public string OrderNumber { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<OrderItem> Items { get; set; }
    public Payment Payment { get; set; }
}
```

### OrderItem Model
```csharp
public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderedAt { get; set; }
}
```

## Configuration

### Database Context
Ensure the following entities are configured in `AppDbContext`:
```csharp
public DbSet<Order> Orders { get; set; }
public DbSet<OrderItem> OrderItems { get; set; }
public DbSet<Payment> Payments { get; set; }
public DbSet<PaymentMethod> PaymentMethods { get; set; }
public DbSet<PaymentProvider> PaymentProviders { get; set; }
```

### Dependency Injection
Register services in `Program.cs`:
```csharp
services.AddScoped<IOrderRepository, OrderRepository>();
services.AddScoped<IOrderServices, OrderServices>();
```

## Usage Examples

### Creating an Order from Cart
```csharp
var orderDto = new CreateOrderDto
{
    PaymentMethodId = 1,
    PaymentProviderId = 1,
    TaxAmount = 10.00m,
    ShippingCost = 5.00m,
    DiscountAmount = 0.00m,
    Notes = "Please deliver before 5 PM"
};

var result = await _orderServices.CreateOrderFromCartAsync(userId, orderDto);
```

### Updating Order Status (Admin)
```csharp
var statusDto = new UpdateOrderStatusDto
{
    Status = OrderStatus.Shipped,
    Notes = "Order shipped via Express Delivery"
};

var result = await _orderServices.UpdateOrderStatusAsync(orderId, statusDto, "Admin");
```

### Cancelling an Order
```csharp
var cancelDto = new CancelOrderDto
{
    CancellationReason = "Changed my mind about the purchase"
};

var result = await _orderServices.CancelOrderAsync(orderId, cancelDto, userId);
```

## Error Handling

The system includes comprehensive error handling:

### Common Error Scenarios
- **Order Not Found**: 404 response with appropriate message
- **Access Denied**: 403 response for unauthorized access
- **Invalid Input**: 400 response with validation errors
- **Business Logic Errors**: Appropriate error messages for business rule violations
- **System Errors**: 500 response with logged error details

### Error Response Format
```json
{
    "success": false,
    "message": "Order not found",
    "statusCode": 404,
    "data": null
}
```

## Caching Strategy

### Cache Implementation
- **Order Details**: Cached by order ID and user ID
- **Cache Invalidation**: Automatic cache clearing on order updates
- **Cache Tags**: Uses "order" tag for bulk cache management

### Cache Keys
- `order_id_{orderId}_user_{userId}`: Individual order cache
- Cache cleared on: order creation, status updates, cancellation

## Security Considerations

### Authorization
- **Customer Access**: Users can only access their own orders
- **Admin Access**: Admins can access all orders and perform administrative actions
- **Role-Based Permissions**: Different endpoints require different roles

### Data Protection
- **Payment Information**: Sensitive payment data is handled securely
- **Input Validation**: All inputs are validated to prevent injection attacks
- **Transaction Safety**: Database transactions ensure data consistency

## Performance Optimizations

### Database Optimizations
- **Eager Loading**: Related entities loaded efficiently
- **Pagination**: Large result sets are paginated
- **Indexing**: Proper database indexing for common queries

### Caching Benefits
- **Reduced Database Load**: Frequently accessed orders are cached
- **Faster Response Times**: Cached data returns immediately
- **Scalability**: Reduces database connection usage

## Monitoring and Logging

### Logging Strategy
- **Information Logs**: Order creation, status changes, cancellations
- **Warning Logs**: Failed operations, validation errors
- **Error Logs**: System errors with stack traces

### Admin Notifications
- **Error Notifications**: Critical errors sent to admin via background jobs
- **Operation Logging**: All admin operations logged for audit trail

## Future Enhancements

### Planned Features
1. **Email Notifications**: Order status update emails
2. **SMS Notifications**: Shipping and delivery updates
3. **Order Tracking**: Real-time order tracking integration
4. **Return Management**: Automated return processing
5. **Inventory Integration**: Automatic inventory updates
6. **Analytics Dashboard**: Advanced reporting and analytics
7. **Multi-currency Support**: International order support
8. **Tax Calculation**: Automated tax calculation
9. **Shipping Integration**: Real-time shipping rates
10. **Order Templates**: Recurring order support

### Integration Possibilities
- **Payment Gateways**: Stripe, PayPal, Square integration
- **Shipping Providers**: FedEx, UPS, DHL API integration
- **Email Services**: SendGrid, Mailgun integration
- **SMS Services**: Twilio, AWS SNS integration
- **Analytics**: Google Analytics, Mixpanel integration

## Testing Strategy

### Unit Tests
- **Service Layer**: Business logic testing
- **Repository Layer**: Data access testing
- **Validation**: Input validation testing

### Integration Tests
- **API Endpoints**: End-to-end API testing
- **Database Operations**: Transaction testing
- **Cache Operations**: Caching behavior testing

### Performance Tests
- **Load Testing**: High-volume order processing
- **Stress Testing**: System behavior under load
- **Cache Performance**: Cache hit/miss ratio testing

## Deployment Considerations

### Database Migrations
- Ensure all required tables are created
- Run pending migrations before deployment
- Test migration rollback procedures

### Configuration
- Set up payment provider credentials
- Configure cache settings
- Set up logging and monitoring

### Monitoring
- Set up health checks for order endpoints
- Configure alerts for failed operations
- Monitor cache performance metrics

## Conclusion

The Order Management System provides a robust, scalable solution for e-commerce order processing. With comprehensive features, security measures, and performance optimizations, it's ready for production use and can be extended to meet future business requirements.

The system follows best practices for:
- **Architecture**: Clean separation of concerns
- **Security**: Proper authorization and validation
- **Performance**: Efficient caching and database operations
- **Maintainability**: Well-structured, documented code
- **Scalability**: Designed for growth and expansion 
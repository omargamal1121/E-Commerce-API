# 🏠 Customer Address System

## 📋 Overview

The Customer Address System provides comprehensive address management functionality for customers in the E-Commerce platform. It allows customers to manage multiple addresses, set default addresses, and organize addresses by type (Home, Work, Other).

## 🏗️ Architecture

### **Models**
- **CustomerAddress**: Enhanced model with comprehensive address fields
- **Customer**: Updated to include address collection

### **DTOs**
- **CustomerAddressDto**: Complete address information with calculated properties
- **CreateCustomerAddressDto**: For creating new addresses
- **UpdateCustomerAddressDto**: For updating existing addresses
- **SetDefaultAddressDto**: For setting default address

### **Repository Pattern**
- **ICustomerAddressRepository**: Interface defining address operations
- **CustomerAddressRepository**: Implementation with comprehensive CRUD operations

### **Service Layer**
- **ICustomerAddressServices**: Service interface
- **CustomerAddressServices**: Business logic implementation

### **Controller**
- **CustomerAddressController**: RESTful API endpoints with authorization

## 🚀 Features

### **Core Functionality**
- ✅ Create, Read, Update, Delete addresses
- ✅ Set default address
- ✅ Address type management (Home, Work, Other)
- ✅ Address search and filtering
- ✅ Address count tracking
- ✅ Default address management

### **Security & Validation**
- ✅ User authorization (customers can only access their own addresses)
- ✅ Admin-only endpoints for customer details
- ✅ Comprehensive input validation
- ✅ Data sanitization (trimming, formatting)

### **Performance & Caching**
- ✅ Redis-based caching for frequently accessed data
- ✅ Cache invalidation on data changes
- ✅ Optimized database queries with includes

### **Error Handling & Logging**
- ✅ Comprehensive error handling
- ✅ Admin operation logging
- ✅ Error notification system
- ✅ Detailed logging for debugging

### **Transaction Management**
- ✅ Database transactions for data integrity
- ✅ Rollback on errors
- ✅ Atomic operations

## 📊 Database Schema

### **CustomerAddress Table**
```sql
CREATE TABLE CustomerAddresses (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    CustomerId VARCHAR(255) NOT NULL,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    PhoneNumber VARCHAR(20) NOT NULL,
    Country VARCHAR(50) NOT NULL,
    State VARCHAR(50) NOT NULL,
    City VARCHAR(50) NOT NULL,
    StreetAddress VARCHAR(200) NOT NULL,
    ApartmentSuite VARCHAR(100) NULL,
    PostalCode VARCHAR(20) NOT NULL,
    AddressType VARCHAR(20) NOT NULL DEFAULT 'Home',
    IsDefault BOOLEAN NOT NULL DEFAULT FALSE,
    AdditionalNotes VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL,
    ModifiedAt DATETIME NULL,
    DeletedAt DATETIME NULL,
    FOREIGN KEY (CustomerId) REFERENCES AspNetUsers(Id)
);
```

## 🔧 API Endpoints

### **Customer Endpoints (Authenticated)**
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/CustomerAddress` | Get all customer addresses | Customer |
| GET | `/api/CustomerAddress/{id}` | Get specific address | Customer |
| GET | `/api/CustomerAddress/default` | Get default address | Customer |
| POST | `/api/CustomerAddress` | Create new address | Customer |
| PUT | `/api/CustomerAddress/{id}` | Update address | Customer |
| DELETE | `/api/CustomerAddress/{id}` | Delete address | Customer |
| POST | `/api/CustomerAddress/{id}/set-default` | Set default address | Customer |
| GET | `/api/CustomerAddress/type/{type}` | Get addresses by type | Customer |
| GET | `/api/CustomerAddress/search` | Search addresses | Customer |
| GET | `/api/CustomerAddress/count` | Get address count | Customer |

### **Admin Endpoints**
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/CustomerAddress/{id}/with-customer` | Get address with customer details | Admin |

## 📝 Request/Response Examples

### **Create Address**
```http
POST /api/CustomerAddress
Authorization: Bearer {token}
Content-Type: application/json

{
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "country": "United States",
  "state": "California",
  "city": "Los Angeles",
  "streetAddress": "123 Main Street",
  "apartmentSuite": "Apt 4B",
  "postalCode": "90210",
  "addressType": "Home",
  "isDefault": true,
  "additionalNotes": "Ring doorbell twice"
}
```

### **Response**
```json
{
  "success": true,
  "message": "Address created successfully",
  "data": {
    "id": 1,
    "customerId": "user123",
    "firstName": "John",
    "lastName": "Doe",
    "fullName": "John Doe",
    "phoneNumber": "+1234567890",
    "country": "United States",
    "state": "California",
    "city": "Los Angeles",
    "streetAddress": "123 Main Street",
    "apartmentSuite": "Apt 4B",
    "postalCode": "90210",
    "fullAddress": "123 Main Street Apt 4B, Los Angeles, California 90210, United States",
    "addressType": "Home",
    "isDefault": true,
    "additionalNotes": "Ring doorbell twice",
    "createdAt": "2024-01-15T10:30:00Z",
    "modifiedAt": null
  },
  "statusCode": 201
}
```

### **Get Customer Addresses**
```http
GET /api/CustomerAddress
Authorization: Bearer {token}
```

### **Set Default Address**
```http
POST /api/CustomerAddress/1/set-default
Authorization: Bearer {token}
```

## 🔒 Security Features

### **Authorization**
- All endpoints require authentication
- Customers can only access their own addresses
- Admin endpoints require admin role
- JWT token validation

### **Validation**
- Input sanitization (trimming, formatting)
- Comprehensive validation attributes
- Business rule validation
- Data integrity checks

### **Error Handling**
- Graceful error handling
- Meaningful error messages
- Admin notifications for critical errors
- Detailed logging for debugging

## 🚀 Performance Optimizations

### **Caching Strategy**
- Redis-based caching for frequently accessed data
- Cache keys: `address_{id}_{userId}`, `customer_addresses_{userId}`, `default_address_{userId}`
- Cache invalidation on data changes
- Cache tags for bulk invalidation

### **Database Optimization**
- Optimized queries with includes
- Indexed foreign keys
- Soft delete implementation
- Efficient pagination

### **Transaction Management**
- Database transactions for data integrity
- Atomic operations
- Rollback on errors
- Deadlock prevention

## 📈 Business Logic

### **Default Address Management**
- Only one default address per customer
- Automatic default assignment for first address
- Default transfer on address deletion
- Default removal from other addresses

### **Address Types**
- Home: Primary residence
- Work: Office/business address
- Other: Additional addresses
- Type-based filtering and organization

### **Data Validation**
- Phone number format validation
- Postal code format validation
- Address length and format validation
- Required field validation

## 🔧 Configuration

### **Dependency Injection**
```csharp
// Program.cs or Startup.cs
services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
services.AddScoped<ICustomerAddressServices, CustomerAddressServices>();
```

### **AutoMapper Configuration**
```csharp
// MappingProfile.cs
CreateMap<CustomerAddress, CustomerAddressDto>();
CreateMap<CreateCustomerAddressDto, CustomerAddress>();
```

## 🧪 Testing Considerations

### **Unit Tests**
- Repository layer testing
- Service layer testing
- Controller testing
- Validation testing

### **Integration Tests**
- Database integration
- Cache integration
- Authorization testing
- End-to-end testing

### **Performance Tests**
- Load testing
- Cache performance
- Database query optimization
- Memory usage testing

## 📚 Usage Examples

### **Creating Multiple Addresses**
```csharp
// Create home address
var homeAddress = new CreateCustomerAddressDto
{
    FirstName = "John",
    LastName = "Doe",
    PhoneNumber = "+1234567890",
    Country = "United States",
    State = "California",
    City = "Los Angeles",
    StreetAddress = "123 Home Street",
    PostalCode = "90210",
    AddressType = "Home",
    IsDefault = true
};

// Create work address
var workAddress = new CreateCustomerAddressDto
{
    FirstName = "John",
    LastName = "Doe",
    PhoneNumber = "+1234567890",
    Country = "United States",
    State = "California",
    City = "San Francisco",
    StreetAddress = "456 Work Avenue",
    PostalCode = "94102",
    AddressType = "Work",
    IsDefault = false
};
```

### **Address Management**
```csharp
// Get all addresses
var addresses = await _addressServices.GetCustomerAddressesAsync(userId);

// Get default address
var defaultAddress = await _addressServices.GetDefaultAddressAsync(userId);

// Search addresses
var searchResults = await _addressServices.SearchAddressesAsync("Los Angeles", userId);

// Set new default
await _addressServices.SetDefaultAddressAsync(addressId, userId);
```

## 🔄 Integration Points

### **Order System**
- Address selection during checkout
- Shipping address validation
- Billing address management

### **Customer Profile**
- Address management in profile
- Address history tracking
- Address preferences

### **Admin Dashboard**
- Customer address overview
- Address statistics
- Address management tools

## 📊 Monitoring & Analytics

### **Metrics to Track**
- Address creation rate
- Default address changes
- Address type distribution
- Address validation failures
- API response times

### **Logging**
- Address operations logging
- Error logging
- Performance logging
- Security event logging

## 🔮 Future Enhancements

### **Planned Features**
- Address verification service integration
- Geocoding for address validation
- Address autocomplete
- Address templates
- Bulk address import/export

### **Performance Improvements**
- Advanced caching strategies
- Database query optimization
- CDN integration for static data
- Microservice architecture

## 📞 Support

For questions or issues related to the Customer Address System:
- Check the logs for detailed error information
- Review the API documentation
- Contact the development team
- Submit issues through the project repository

---

**Last Updated**: January 2024
**Version**: 1.0.0
**Status**: Production Ready ✅ 
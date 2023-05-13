namespace ApiFunctionApp.Entities.AzureDB
{
    public class AzureSQL
    {
        public const string AddCustomer = @"
INSERT INTO [dbo].[Customers](
	[CustomerId]
    , [Name]
    , [RegionId])
VALUES (
	@CustomerId
    , @Name
    , @RegionId)";


        public const string GetCustomer = @"
SELECT  
    [CustomerId],
    [Name],
    [RegionId]
FROM [dbo].[Customers]
WHERE CustomerId = @customerId";


        public const string AddOrder = @"
INSERT INTO [dbo].[Orders](
	[CustomerId]
    , [OrderDate]
    , [ProductId])
VALUES (
	@CustomerId
    , @OrderDate
    , @ProductId)";
		

        public const string GetOrders = @"
SELECT  
    [OrderId],
    [CustomerId],
    [OrderDate],
	[ProductId]	
FROM [dbo].[Orders]
WHERE CustomerId = @customerId";
        
	}
}

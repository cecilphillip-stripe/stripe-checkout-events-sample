namespace StripeEventsCheckout.IdentityServer.Data.MongoData;

public interface ICustomerDataStore
{
    Task<Customer> GetCustomerByUsername(string username);
    Task<Customer> GetCustomerById(string id);

    Task CreateCustomer(Customer newCustomer); 
    
    Task<bool> DeleteCustomerByUsername(string username);
    Task<bool> DeleteCustomerById(string id);
}
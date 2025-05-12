using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;
public interface IDataAccess 
{
        
}
public class DataAcces : IDataAccess
{

    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    public DataAcces(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = configuration.GetConnectionString("Default");
    }

    public async Task<int?> AddProductToWarehouse(ProductWarehouse request)
    {
        using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        var transaction = connection.BeginTransaction();

        try
        {
            var checkProduct = new SqlCommand("SELECT 1 FROM Product WHERE IdProduct = @id", connection, transaction);
            checkProduct.Parameters.AddWithValue("@id", request.IdProduct);
            if ((await checkProduct.ExecuteScalarAsync()) == null)
                return null;

            var checkWarehouse = new SqlCommand("SELECT 1 FROM Warehouse WHERE IdWarehouse = @id", connection, transaction);
            checkWarehouse.Parameters.AddWithValue("@id", request.IdWarehouse);
            if ((await checkWarehouse.ExecuteScalarAsync()) == null)
                return null;

            var findOrder = new SqlCommand(@"
                SELECT TOP 1 IdOrder FROM [Order] 
                WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt", connection, transaction);
            findOrder.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            findOrder.Parameters.AddWithValue("@Amount", request.Amount);
            findOrder.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
            var idOrder = (int?)await findOrder.ExecuteScalarAsync();

            if (idOrder == null)
                return null;

            var checkRealized = new SqlCommand("SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder", connection, transaction);
            checkRealized.Parameters.AddWithValue("@IdOrder", idOrder);
            if ((await checkRealized.ExecuteScalarAsync()) != null)
                return null;

            var updateOrder = new SqlCommand("UPDATE [Order] SET FulfilledAt = @Now WHERE IdOrder = @IdOrder", connection, transaction);
            updateOrder.Parameters.AddWithValue("@Now", DateTime.Now);
            updateOrder.Parameters.AddWithValue("@IdOrder", idOrder);
            await updateOrder.ExecuteNonQueryAsync();

            var getPrice = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @IdProduct", connection, transaction);
            getPrice.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            var unitPrice = (decimal)(await getPrice.ExecuteScalarAsync());

            var insertCmd = new SqlCommand(@"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                OUTPUT INSERTED.IdProductWarehouse
                VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)", connection, transaction);
            insertCmd.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            insertCmd.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            insertCmd.Parameters.AddWithValue("@IdOrder", idOrder);
            insertCmd.Parameters.AddWithValue("@Amount", request.Amount);
            insertCmd.Parameters.AddWithValue("@Price", unitPrice * request.Amount);
            insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            var newId = (int?)await insertCmd.ExecuteScalarAsync();

            transaction.Commit();

            return newId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}


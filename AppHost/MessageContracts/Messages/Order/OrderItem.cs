namespace MessageContracts.Messages.Order;

public class OrderItem
{

    public string ProductId { get; init; } = string.Empty;


    public string Name { get; init; } = string.Empty;

 
    public int Quantity { get; init; }


    public decimal UnitPrice { get; init; }
}
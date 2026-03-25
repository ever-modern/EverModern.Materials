using CodegenSample.Supplier;

namespace CodegenSample.Consumer;

public class ConsumerModel : CodegenSample.Basic.Model
{
    public SupplierModel[] Suppliers { get; set; } = Array.Empty<SupplierModel>();
}
